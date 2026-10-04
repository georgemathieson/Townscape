using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation;
using Townscape.Generation.Routes;
using Townscape.Simulation.Security;

namespace Townscape.Simulation.People
{
    /// <summary>
    /// The people who come when an alarm goes off, walking (and driving) the town's routes in
    /// place of the alarm receiving centre's timers. The key-holding guard waits at their post in
    /// the centre; sent to an alarm, they walk there, check the outside, let themselves in, put
    /// the code in and look round every room, then weigh it all up (<see cref="GuardAssessment"/>).
    /// A false alarm they set again and leave; a break-in they back out of and wait outside for
    /// the police. The police drive in with their blue lights and siren, search the building
    /// (catching anyone still in it) and drive off. A burglar, sent from the menu, walks in from
    /// the edge of the town, forces the door, goes for the till and the telly, and runs off once
    /// the alarm has been going a while, leaving the door open behind them.
    /// </summary>
    /// <remarks>
    /// Each person's errand is a coroutine, stepped once a tick, so it reads in order. What the
    /// alarms' sensors see of them, and the doors they open, are left to the town around them
    /// (<see cref="ITownWorld"/>), as they are for the player.
    /// </remarks>
    public sealed class TownPeople
    {
        public const float GuardSpeed = 1.7f;
        public const float OfficerSpeed = 1.6f;
        public const float BurglarSpeed = 1.3f;
        public const float RunSpeed = 3.2f;

        /// <summary>How far anyone can make out someone else.</summary>
        public const float SightRange = 10f;

        /// <summary>How long the police take to reach the edge of the town once they're called.</summary>
        public const float PoliceDelaySeconds = 20f;

        /// <summary>How far from the building a burglar is first seen, walking towards it.</summary>
        public const float BurglarApproach = 45f;

        public const float ForceSeconds = 4f;
        public const float LookSeconds = 3f;
        public const float RoomSeconds = 1.5f;

        /// <summary>How long a burglar stays once the alarm's going: at least this, and up to a couple of minutes.</summary>
        public const float LeastNerveSeconds = 15f;

        /// <summary>How long someone will wait for the player to get out of their way before going round.</summary>
        private const float PatienceSeconds = 3f;

        private readonly RouteNetwork _walking;
        private readonly RouteNetwork _driving;
        private readonly AlarmReceivingCentre _centre;
        private readonly Dictionary<string, TownSite> _sites = new Dictionary<string, TownSite>();
        private readonly Dictionary<string, TownAlarm> _alarms = new Dictionary<string, TownAlarm>();
        private readonly HashSet<string> _windows = new HashSet<string>();
        private readonly HashSet<ArcIncident> _guardCalls = new HashSet<ArcIncident>();
        private readonly HashSet<ArcIncident> _policeCalls = new HashSet<ArcIncident>();
        private readonly List<ArcIncident> _guardQueue = new List<ArcIncident>();
        private readonly List<ArcIncident> _policeQueue = new List<ArcIncident>();
        private readonly List<Person> _officers = new List<Person>();
        private readonly Dictionary<Person, float> _held = new Dictionary<Person, float>();
        private readonly Random _random;
        private readonly string _post;
        private readonly Vector3 _postFacing;

        private IEnumerator<bool> _guardJob;
        private IEnumerator<bool> _policeJob;
        private IEnumerator<bool> _burglarJob;
        private ITownWorld _world;
        private float _dt;
        private float _alarmFor;

        public TownPeople(GeneratedTown town, AlarmReceivingCentre centre, int seed = 1)
        {
            _walking = town.Walking;
            _driving = town.Driving;
            _centre = centre;
            _random = new Random(seed);
            foreach (var site in town.Sites)
            {
                _sites[site.Name] = site;
            }

            foreach (var alarm in town.Alarms)
            {
                _alarms[alarm.Name] = alarm;
            }

            foreach (var door in town.Doors.Where(d => d.Noun == "window"))
            {
                _windows.Add(door.Name);
            }

            if (_centre != null)
            {
                _centre.TimedResponders = false;
            }

            // The guard starts at their post, facing into the room.
            var spec = town.AlarmCentres.FirstOrDefault();
            _post = spec?.GuardPost;
            var post = _walking.Find(_post);
            if (post >= 0)
            {
                Guard = new Person(PersonRole.Guard, "The guard", _walking.Point(post), GuardSpeed) { Doing = "at the alarm receiving centre" };
                Guard.Walker.ShutsDoors = true;
                _postFacing = spec.Room - _walking.Point(post);
                Guard.Walker.Face(_postFacing);
            }
        }

        /// <summary>Someone has forced a door: where, for its sound.</summary>
        public event Action<Vector3> DoorForced;

        /// <summary>The alarm receiving centre's guard (null if there's no centre with a post).</summary>
        public Person Guard { get; }

        /// <summary>The burglar, while there is one.</summary>
        public Person Burglar { get; private set; }

        /// <summary>Which building the burglar's after.</summary>
        public string BurglarTarget { get; private set; }

        /// <summary>The police car, while it's in town.</summary>
        public PoliceCar Car { get; private set; }

        /// <summary>The police officers out of their car.</summary>
        public IReadOnlyList<Person> Officers => _officers;

        /// <summary>Everyone out and about.</summary>
        public IEnumerable<Person> People
        {
            get
            {
                if (Guard != null && Guard.Visible)
                {
                    yield return Guard;
                }

                if (Burglar != null && Burglar.Visible)
                {
                    yield return Burglar;
                }

                foreach (var officer in _officers.Where(o => o.Visible))
                {
                    yield return officer;
                }
            }
        }

        /// <summary>The buildings a burglar can be sent to.</summary>
        public IEnumerable<string> Sites => _sites.Keys;

        /// <summary>
        /// Sends a burglar to <paramref name="site"/>. False if one's already about, or there's no
        /// such site. <paramref name="nerve"/> is how long they'll stay once the alarm's going
        /// (left out, it's up to chance).
        /// </summary>
        public bool SendBurglar(string site, float? nerve = null)
        {
            if (Burglar != null || !_sites.TryGetValue(site, out var target))
            {
                return false;
            }

            // In from the edge of the town, seen first a little way off.
            var outside = _walking.Find(target.Outside);
            var ways = _walking.Entrances.Select(e => _walking.Path(e, outside)).Where(p => p != null).ToList();
            if (ways.Count == 0)
            {
                return false;
            }

            var way = ways[_random.Next(ways.Count)];
            var start = way.Stops.Count - 1;
            var left = 0f;
            while (start > 0 && left < BurglarApproach)
            {
                left += Vector3.Distance(way.Stops[start - 1].Position, way.Stops[start].Position);
                start--;
            }

            Burglar = new Person(PersonRole.Burglar, "The burglar", way.Stops[start].Position, BurglarSpeed);
            BurglarTarget = site;
            _alarmFor = 0f;
            var stay = nerve ?? LeastNerveSeconds + ((float)_random.NextDouble() * 100f);
            _burglarJob = BurglarErrand(target, new RoutePath(way.Stops.Skip(start).ToList()), stay).GetEnumerator();
            return true;
        }

        /// <summary>Moves everyone on by <paramref name="deltaTime"/>: picks up the centre's calls, and steps each errand.</summary>
        public void Tick(float deltaTime, ITownWorld world)
        {
            _dt = deltaTime;
            _world = world;
            if (_centre != null)
            {
                foreach (var incident in _centre.Incidents)
                {
                    if (incident.Closed)
                    {
                        continue;
                    }

                    if (incident.Guard.Stage == ResponderStage.EnRoute && _guardCalls.Add(incident))
                    {
                        _guardQueue.Add(incident);
                    }

                    if (incident.Police.Stage == ResponderStage.EnRoute && _policeCalls.Add(incident))
                    {
                        _policeQueue.Add(incident);
                    }
                }
            }

            if (_guardJob == null && _guardQueue.Count > 0 && Guard != null)
            {
                _guardJob = GuardErrand(Take(_guardQueue)).GetEnumerator();
            }

            if (_policeJob == null && _policeQueue.Count > 0)
            {
                _policeJob = PoliceErrand(Take(_policeQueue)).GetEnumerator();
            }

            if (Burglar != null && BurglarTarget != null && Alarm(BurglarTarget)?.State == AlarmState.Sounding)
            {
                _alarmFor += deltaTime;
            }

            Step(ref _guardJob);
            Step(ref _policeJob);
            Step(ref _burglarJob);

            foreach (var person in People.ToList())
            {
                Move(person, deltaTime);
            }

            if (Car != null)
            {
                Car.Driver.Held = world != null && !Car.Driver.Arrived && world.InTheWay(Car.Position, Car.Facing, 6f);
                Car.Driver.Tick(deltaTime, null);
            }
        }

        // ---- The guard -------------------------------------------------------------------------

        private IEnumerable<bool> GuardErrand(ArcIncident incident)
        {
            var guard = Guard;
            if (_sites.TryGetValue(incident.Site.Name, out var site))
            {
                guard.Doing = $"on the way to {site.Name}";
                foreach (var step in Walk(guard, site.Outside, () => _centre.Eta(incident, true, guard.Walker.Remaining / GuardSpeed)))
                {
                    if (incident.Closed)
                    {
                        break;
                    }

                    yield return step;
                }

                if (!incident.Closed)
                {
                    _centre.Arrived(incident, guard: true);
                    foreach (var step in CheckSite(incident, site))
                    {
                        yield return step;
                    }
                }
            }

            // Back to the centre, unless there's another call to go straight on to.
            if (_guardQueue.Count > 0)
            {
                yield break;
            }

            guard.Doing = "on the way back to the centre";
            foreach (var step in Walk(guard, _post))
            {
                yield return step;
            }

            guard.Walker.Face(_postFacing);
            guard.Doing = "at the alarm receiving centre";
        }

        private IEnumerable<bool> CheckSite(ArcIncident incident, TownSite site)
        {
            var guard = Guard;
            var alarm = incident.Site.Alarm;
            var seen = false;
            void Look() => seen |= Sees(guard);
            var keypad = _walking.Point(_walking.Find(site.Keypad));
            var setBefore = alarm.Armed && !incident.Tamper;

            // Outside first: is the door open, is anyone moving about inside?
            guard.Doing = $"checking the outside of {site.Name}";
            guard.Walker.Face(keypad - guard.Position);
            foreach (var step in Wait(LookSeconds, Look))
            {
                yield return step;
            }

            var inside = false;
            if (!seen && !Openings(site).Any(o => o.Open && !o.Window))
            {
                // In with the key, the code in at the keypad, and round every room.
                _centre.GoingIn(incident, guard: true);
                inside = true;
                guard.Doing = $"letting themselves in to {site.Name}";
                foreach (var step in Walk(guard, site.Keypad, Look))
                {
                    yield return step;
                }

                foreach (var step in Wait(RoomSeconds, Look))
                {
                    yield return step;
                }

                if (alarm.Armed)
                {
                    alarm.Unset(BurglarAlarm.DefaultCode);
                }

                guard.Doing = $"looking round {site.Name}";
                for (var i = 0; i < site.Rooms.Count && !seen; i++)
                {
                    var roomsLeft = site.Rooms.Count - i;
                    foreach (var step in Walk(guard, site.Rooms[i], () => { Look(); _centre.Eta(incident, true, roomsLeft * 8f); }))
                    {
                        yield return step;
                    }

                    foreach (var step in Wait(RoomSeconds, Look))
                    {
                        yield return step;
                    }
                }
            }

            var evidence = _centre.Evidence(incident);
            evidence.Openings = Openings(site).ToList();
            evidence.IntruderSeen = seen;
            var assessment = GuardAssessment.Assess(evidence);
            _centre.GuardReported(incident, assessment);

            if (assessment.BreakIn)
            {
                // Out of harm's way, to wait for the police to come and go (or for the operator to
                // call it off).
                guard.Doing = $"waiting outside {site.Name} for the police";
                if (inside)
                {
                    foreach (var step in Walk(guard, site.Outside))
                    {
                        yield return step;
                    }
                }

                while (!incident.Closed && (incident.Police.Stage != ResponderStage.Done || _officers.Count > 0))
                {
                    guard.Walker.Face(keypad - guard.Position);
                    yield return false;
                }
            }

            // Secure it: the code in if it's still going, every door and window shut, and set
            // again (as it was) on the way out.
            guard.Doing = $"securing {site.Name}";
            foreach (var step in Walk(guard, site.Keypad))
            {
                yield return step;
            }

            if (alarm.Armed)
            {
                alarm.Unset(BurglarAlarm.DefaultCode);
            }

            foreach (var opening in Openings(site).Where(o => o.Open))
            {
                _world?.Shut(opening.Name);
            }

            foreach (var step in Wait(RoomSeconds))
            {
                yield return step;
            }

            if (setBefore)
            {
                alarm.Set(BurglarAlarm.DefaultCode);
            }

            foreach (var step in Walk(guard, site.Outside))
            {
                yield return step;
            }
        }

        // ---- The police ------------------------------------------------------------------------

        private IEnumerable<bool> PoliceErrand(ArcIncident incident)
        {
            if (!_sites.TryGetValue(incident.Site.Name, out var site))
            {
                yield break;
            }

            var outside = _walking.Point(_walking.Find(site.Outside));
            var parking = _driving.Nearest(outside);
            var route = _driving.Entrances.Select(e => _driving.Path(e, parking)).Where(p => p != null).OrderBy(p => p.Length).FirstOrDefault();
            if (route == null)
            {
                yield break;
            }

            // A car's on its way: a little while before it gets to the edge of the town.
            for (var left = PoliceDelaySeconds; left > 0f; left -= _dt)
            {
                _centre.Eta(incident, false, left + (route.Length / PoliceCar.Speed));
                yield return false;
            }

            Car = new PoliceCar(route.Stops[0].Position) { Lights = true, Siren = true };
            Car.Driver.Follow(route);
            while (!Car.Driver.Arrived)
            {
                _centre.Eta(incident, false, Car.Driver.Remaining / PoliceCar.Speed);
                yield return false;
            }

            // Two officers get out on the kerb side and go to the door.
            Car.Siren = false;
            var kerb = Car.Position + (Left(Car.Facing) * 1.3f);
            _officers.Clear();
            _officers.Add(new Person(PersonRole.Police, "PC Hartley", kerb + (Car.Facing * 0.5f), OfficerSpeed));
            _officers.Add(new Person(PersonRole.Police, "PC Nicholson", kerb - (Car.Facing * 0.6f), OfficerSpeed * 0.92f));
            var pavement = _walking.Nearest(kerb, outdoorsOnly: true);
            foreach (var officer in _officers)
            {
                officer.Doing = $"going to {site.Name}";
                officer.Walker.Follow(From(officer.Position, _walking.Path(pavement, _walking.Find(site.Outside))));
            }

            while (_officers.Any(o => !o.Walker.Arrived))
            {
                yield return false;
            }

            _centre.Arrived(incident, guard: false);

            // In, and round every room, looking for anyone still there.
            _centre.GoingIn(incident, guard: false);
            var caught = false;
            void Look()
            {
                if (!caught && Burglar != null && !Burglar.Arrested && _officers.Any(Sees))
                {
                    caught = true;
                    Arrest();
                }
            }

            // One searches room by room; the other keeps the door.
            var lead = _officers[0];
            var cover = _officers[1];
            cover.Doing = $"keeping the door of {site.Name}";
            cover.Walker.Follow(PathTo(cover, _walking.Find(site.Keypad)));
            for (var i = 0; i < site.Rooms.Count && !caught; i++)
            {
                var roomsLeft = site.Rooms.Count - i;
                lead.Doing = $"searching {site.Name}";
                foreach (var step in Walk(lead, site.Rooms[i], () => { Look(); _centre.Eta(incident, false, roomsLeft * 8f); }))
                {
                    yield return step;
                }

                foreach (var step in Wait(RoomSeconds, Look))
                {
                    yield return step;
                }
            }

            _centre.PoliceReported(incident, caught);

            // Back to the car (with whoever they've caught), and away.
            var door = Car.Position + (Left(Car.Facing) * 1.1f);
            var going = _officers.ToList();
            if (caught && Burglar != null)
            {
                going.Add(Burglar);
            }

            foreach (var person in going)
            {
                person.Doing = person.Role == PersonRole.Burglar ? "being taken away" : "going back to the car";
                person.Walker.Speed = OfficerSpeed;
                var way = PathTo(person, pavement);
                person.Walker.Follow(way == null ? null : new RoutePath(way.Stops.Append(new RouteStop(door)).ToList()));
            }

            while (going.Any(p => !p.Walker.Arrived))
            {
                yield return false;
            }

            foreach (var person in going)
            {
                person.Visible = false;
            }

            _officers.Clear();
            if (caught)
            {
                Burglar = null;
                BurglarTarget = null;
                _burglarJob = null;
            }

            foreach (var step in Wait(2f))
            {
                yield return step;
            }

            Car.Lights = false;
            var exit = _driving.Exits.Select(e => _driving.Path(parking, e)).Where(p => p != null).OrderBy(p => p.Length).FirstOrDefault();
            Car.Driver.Follow(exit);
            while (!Car.Driver.Arrived)
            {
                yield return false;
            }

            Car = null;
        }

        private void Arrest()
        {
            var burglar = Burglar;
            burglar.Arrested = true;
            burglar.Carrying = false;
            burglar.Doing = "under arrest";
            burglar.Walker.Stop();
            _burglarJob = null;
        }

        // ---- The burglar -----------------------------------------------------------------------

        private IEnumerable<bool> BurglarErrand(TownSite site, RoutePath approach, float nerve)
        {
            var burglar = Burglar;
            var alarm = Alarm(site.Name);
            bool Spooked() => alarm != null && _alarmFor > nerve;

            burglar.Doing = $"heading for {site.Name}";
            foreach (var step in Walk(burglar, approach))
            {
                yield return step;
            }

            // Force the door, and in.
            burglar.Doing = "forcing the door";
            var keypad = _walking.Point(_walking.Find(site.Keypad));
            burglar.Walker.Face(keypad - burglar.Position);
            foreach (var step in Wait(ForceSeconds))
            {
                yield return step;
            }

            DoorForced?.Invoke(burglar.Position);

            // The things worth taking first, then a look round the rest; and round again while
            // the alarm's going, until their nerve goes. With no alarm, once round is enough.
            var spots = site.Loot.Concat(site.Rooms.Where(r => !site.Loot.Contains(r)).OrderBy(_ => _random.Next()).Take(2)).ToList();
            while (!Spooked())
            {
                foreach (var spot in spots)
                {
                    if (Spooked())
                    {
                        break;
                    }

                    burglar.Doing = $"inside {site.Name}";
                    foreach (var step in Walk(burglar, spot))
                    {
                        if (Spooked())
                        {
                            break;
                        }

                        yield return step;
                    }

                    burglar.Doing = "rummaging";
                    foreach (var step in Wait(6f + ((float)_random.NextDouble() * 6f)))
                    {
                        if (Spooked())
                        {
                            break;
                        }

                        yield return step;
                    }

                    burglar.Carrying = true;
                }

                if (alarm == null || alarm.State != AlarmState.Sounding)
                {
                    break;
                }
            }

            // Off, leaving the door open: running if the alarm's going.
            var running = alarm != null && alarm.State == AlarmState.Sounding;
            burglar.Doing = running ? "running off" : "slipping away";
            burglar.Walker.Speed = running ? RunSpeed : BurglarSpeed;
            foreach (var step in Walk(burglar, site.Outside))
            {
                yield return step;
            }

            var exits = _walking.Exits.Where(e => Vector3.Distance(_walking.Point(e), burglar.Position) > BurglarApproach).ToList();
            if (exits.Count == 0)
            {
                exits = _walking.Exits.ToList();
            }

            if (exits.Count > 0)
            {
                foreach (var step in Walk(burglar, PathTo(burglar, exits[_random.Next(exits.Count)])))
                {
                    yield return step;
                }
            }

            burglar.Visible = false;
            Burglar = null;
            BurglarTarget = null;
        }

        // ---- Getting about ---------------------------------------------------------------------

        private IEnumerable<bool> Walk(Person person, string place, Action each = null) =>
            Walk(person, PathTo(person, _walking.Find(place)), each);

        private IEnumerable<bool> Walk(Person person, RoutePath path, Action each = null)
        {
            if (path == null)
            {
                yield break;
            }

            person.Walker.Follow(path);
            while (!person.Walker.Arrived)
            {
                each?.Invoke();
                yield return false;
            }
        }

        private IEnumerable<bool> Wait(float seconds, Action each = null)
        {
            for (var left = seconds; left > 0f; left -= _dt)
            {
                each?.Invoke();
                yield return false;
            }
        }

        // The way from wherever they are (via the stop nearest them) to a stop.
        private RoutePath PathTo(Person person, int stop) => From(person.Position, _walking.Path(_walking.Nearest(person.Position), stop));

        private static RoutePath From(Vector3 position, RoutePath path)
        {
            if (path == null || Vector3.DistanceSquared(path.Stops[0].Position, position) < 1e-4f)
            {
                return path;
            }

            return new RoutePath(new[] { new RouteStop(position) }.Concat(path.Stops).ToList());
        }

        // Steps their walk on, waiting a moment for the player to get out of the way.
        private void Move(Person person, float deltaTime)
        {
            var walker = person.Walker;
            _held.TryGetValue(person, out var held);
            var blocked = _world != null && !walker.Arrived && _world.InTheWay(person.Position, walker.Facing, 0.9f);
            held = blocked ? held + deltaTime : 0f;
            _held[person] = held;
            walker.Held = blocked && held < PatienceSeconds;
            walker.Tick(deltaTime, _world);
        }

        private bool Sees(Person watcher)
        {
            var burglar = Burglar;
            return burglar != null && burglar.Visible && _world != null &&
                Vector3.Distance(watcher.Eye, burglar.Chest) < SightRange && _world.CanSee(watcher.Eye, burglar.Chest);
        }

        // The doors and windows on the outside of the building: those with a contact on them.
        private IEnumerable<PerimeterOpening> Openings(TownSite site)
        {
            if (!_alarms.TryGetValue(site.Name, out var alarm))
            {
                yield break;
            }

            foreach (var zone in alarm.Zones.Where(z => z.Kind == AlarmZoneKind.Door && z.Door != null))
            {
                yield return new PerimeterOpening(zone.Door, _windows.Contains(zone.Door), _world != null && _world.IsOpen(zone.Door));
            }
        }

        private BurglarAlarm Alarm(string site) => _centre?.Site(site)?.Alarm;

        private static void Step(ref IEnumerator<bool> job)
        {
            if (job != null && !job.MoveNext())
            {
                job = null;
            }
        }

        private static ArcIncident Take(List<ArcIncident> queue)
        {
            var next = queue[0];
            queue.RemoveAt(0);
            return next;
        }

        private static Vector3 Left(Vector3 facing) => new Vector3(-facing.Z, 0f, facing.X);
    }
}
