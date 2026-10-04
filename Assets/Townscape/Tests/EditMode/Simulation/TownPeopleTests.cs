using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Simulation.People;
using Townscape.Simulation.Security;
using Townscape.Tests.Generation;

namespace Townscape.Tests.Simulation
{
    public sealed class TownPeopleTests
    {
        private const string Cafe = CafeAndFlat.ShopAlarm;
        private const float Step = 0.1f;

        [Test]
        public void TheGuard_WaitsAtTheirPost_InTheAlarmReceivingCentre()
        {
            var village = new Village();

            Assert.That(village.People.Guard, Is.Not.Null);
            Assert.That(Vector3.Distance(village.People.Guard.Position, village.Point(MillWorks.GuardPost)), Is.LessThan(0.01f));
            Assert.That(village.Centre.TimedResponders, Is.False, "people do the walking now");

            village.Run(30f);
            Assert.That(Vector3.Distance(village.People.Guard.Position, village.Point(MillWorks.GuardPost)), Is.LessThan(0.01f), "nobody's sent them anywhere");
        }

        [Test]
        public void ABurglar_ForcesTheDoor_SetsOffTheAlarm_AndRunsOff_LeavingItOpen()
        {
            var village = new Village();
            var forced = 0;
            village.People.DoorForced += _ => forced++;
            village.SetAndLeave(Cafe);

            Assert.That(village.People.SendBurglar(Cafe, nerve: 5f), Is.True);
            Assert.That(village.People.SendBurglar(Cafe), Is.False, "one at a time");
            Assert.That(village.People.Burglar.Position.Y, Is.LessThan(2f));
            var start = village.People.Burglar.Position;
            Assert.That(Vector3.Distance(start, village.Point($"{Cafe}: outside")), Is.InRange(20f, 60f), "seen a little way off");

            village.RunUntil(() => village.People.Burglar == null, 600f);

            Assert.That(forced, Is.EqualTo(1));
            Assert.That(village.World.IsOpen("Shop door"), Is.True, "left open behind them");
            var incident = village.Centre.OpenIncident(village.Centre.Site(Cafe));
            Assert.That(incident, Is.Not.Null);
            Assert.That(incident.Confirmed, Is.True, "the door, then the café sensor");
            Assert.That(village.Alarms[Cafe].State, Is.EqualTo(AlarmState.Sounding));
        }

        [Test]
        public void TheGuard_FindsTheDoorOpen_WaitsForThePolice_ThenSecuresTheBuilding()
        {
            var village = new Village();
            village.SetAndLeave(Cafe);
            village.People.SendBurglar(Cafe, nerve: 5f);
            village.RunUntil(() => village.People.Burglar == null, 600f);
            var incident = village.Centre.OpenIncident(village.Centre.Site(Cafe));

            village.Centre.DispatchGuard(incident);
            village.RunUntil(() => incident.Guard.Stage == ResponderStage.OnSite, 300f);
            Assert.That(village.Centre.Log.Any(e => e.Text == "Guard dispatched"), Is.True);
            Assert.That(Vector3.Distance(village.People.Guard.Position, village.Point($"{Cafe}: outside")), Is.LessThan(0.5f));

            village.RunUntil(() => incident.Guard.Stage == ResponderStage.Done, 60f);
            Assert.That(incident.Assessment.Verdict, Is.EqualTo(GuardVerdict.BreakIn));
            Assert.That(incident.Assessment.Factors.Select(f => f.Reason), Has.Member("Shop door was open"));
            Assert.That(incident.ResponderInside, Is.False, "they didn't go in");

            village.Run(30f);
            Assert.That(Vector3.Distance(village.People.Guard.Position, village.Point($"{Cafe}: outside")), Is.LessThan(0.5f), "waiting for the police");

            Assert.That(village.Centre.CallPolice(incident), Is.True);
            village.RunUntil(() => village.People.Car != null, 60f);
            Assert.That(village.People.Car.Siren, Is.True);
            Assert.That(village.People.Car.Lights, Is.True);
            village.RunUntil(() => incident.Police.Stage == ResponderStage.OnSite, 120f);
            Assert.That(village.People.Car.Siren, Is.False, "quiet once they're there");
            Assert.That(village.People.Officers, Has.Count.EqualTo(2));
            village.RunUntil(() => village.People.Car == null, 300f);

            Assert.That(incident.Police.Stage, Is.EqualTo(ResponderStage.Done));
            Assert.That(incident.Arrested, Is.False, "long gone");
            village.RunUntil(() => village.People.Guard.Doing == "at the alarm receiving centre", 300f);
            Assert.That(village.World.IsOpen("Shop door"), Is.False, "the guard shut it");
            Assert.That(village.Alarms[Cafe].State, Is.EqualTo(AlarmState.Set), "and set the alarm again");
            Assert.That(Vector3.Distance(village.People.Guard.Position, village.Point(MillWorks.GuardPost)), Is.LessThan(0.01f));
        }

        [Test]
        public void AFalseAlarm_IsCheckedInside_UnsetAndSetAgain()
        {
            var village = new Village();
            village.SetAndLeave(Cafe);
            village.Alarms[Cafe].Detected(1);
            village.Run(BurglarAlarm.EntryExitSeconds + 1f);
            var incident = village.Centre.OpenIncident(village.Centre.Site(Cafe));
            Assert.That(incident.Zones, Has.Count.EqualTo(1));

            village.Centre.DispatchGuard(incident);
            village.RunUntil(() => incident.Guard.Stage == ResponderStage.Done, 400f);

            Assert.That(incident.ResponderInside, Is.True);
            Assert.That(incident.Zones, Has.Count.EqualTo(1), "the guard walking about doesn't confirm it");
            Assert.That(incident.CancelledAtKeypad, Is.False);
            Assert.That(incident.Assessment.Verdict, Is.EqualTo(GuardVerdict.FalseAlarm));
            Assert.That(village.Centre.Log.Any(e => e.Text == "Unset by the guard"), Is.True);

            village.RunUntil(() => village.People.Guard.Doing == "at the alarm receiving centre", 300f);
            Assert.That(village.Alarms[Cafe].State, Is.EqualTo(AlarmState.Set), "set again on the way out");
            Assert.That(village.World.IsOpen("Shop door"), Is.False);
            Assert.That(village.World.IsOpen(MillWorks.DoorName), Is.False, "and every door shut behind them");
            Assert.That(village.World.IsOpen(MillWorks.CentreDoorName), Is.False);
        }

        [Test]
        public void ThePolice_CatchABurglarWhoStaysTooLong()
        {
            var village = new Village();
            village.SetAndLeave(Cafe);
            village.People.SendBurglar(Cafe, nerve: 600f);
            village.RunUntil(() => village.Alarms[Cafe].State == AlarmState.Sounding, 300f);
            var incident = village.Centre.OpenIncident(village.Centre.Site(Cafe));
            village.Run(2f);
            Assert.That(incident.Confirmed, Is.True);

            Assert.That(village.Centre.CallPolice(incident), Is.True);
            village.RunUntil(() => incident.Police.Stage == ResponderStage.Done, 300f);

            Assert.That(incident.Arrested, Is.True);
            Assert.That(village.People.Burglar.Arrested, Is.True);
            village.RunUntil(() => village.People.Car == null, 300f);
            Assert.That(village.People.Burglar, Is.Null, "taken away");
            Assert.That(village.Centre.Log.Any(e => e.Text.StartsWith("Police: intruder arrested")), Is.True);
        }

        [Test]
        public void ABurglar_AtAnUnsetAlarm_TakesTheirTime_AndNobodyKnows()
        {
            var village = new Village();
            village.People.SendBurglar(Cafe);

            village.RunUntil(() => village.People.Burglar == null, 600f);

            Assert.That(village.Centre.Incidents, Is.Empty);
            Assert.That(village.World.IsOpen("Shop door"), Is.True, "only the open door to show for it");
        }

        [Test]
        public void TheGuard_GoesStraightOnToASecondCall()
        {
            var village = new Village();
            const string flat = CafeAndFlat.FlatAlarm;
            village.SetAndLeave(Cafe);
            village.SetAndLeave(flat);
            village.Alarms[Cafe].Detected(1);
            village.Alarms[flat].Detected(1);
            village.Run(BurglarAlarm.EntryExitSeconds + 1f);
            var first = village.Centre.OpenIncident(village.Centre.Site(Cafe));
            var second = village.Centre.OpenIncident(village.Centre.Site(flat));

            village.Centre.DispatchGuard(first);
            village.Centre.DispatchGuard(second);
            village.RunUntil(() => second.Guard.Stage == ResponderStage.Done, 900f);

            Assert.That(first.Assessment.Verdict, Is.EqualTo(GuardVerdict.FalseAlarm));
            Assert.That(second.Assessment.Verdict, Is.EqualTo(GuardVerdict.FalseAlarm));
            Assert.That(village.People.Guard.Doing, Does.Not.Contain("at the alarm receiving centre"), "never went back in between");
            village.RunUntil(() => village.People.Guard.Doing == "at the alarm receiving centre", 400f);
            Assert.That(village.Alarms[flat].State, Is.EqualTo(AlarmState.Set));
            Assert.That(village.World.IsOpen("Flat door"), Is.False);
        }

        // The town around the people: doors that open and shut (tripping their contacts, as
        // the runtime's do), sensors that see them moving, and eyes that see through anything
        // on the same floor (walls are the runtime's business).
        private sealed class World : ITownWorld
        {
            private readonly HashSet<string> _open = new HashSet<string>();
            private readonly List<(BurglarAlarm Alarm, int Zone, AlarmZone Spec)> _zones = new List<(BurglarAlarm, int, AlarmZone)>();

            public World(GeneratedTown town, IReadOnlyDictionary<string, BurglarAlarm> alarms)
            {
                foreach (var spec in town.Alarms)
                {
                    for (var i = 0; i < spec.Zones.Count; i++)
                    {
                        _zones.Add((alarms[spec.Name], i, spec.Zones[i]));
                    }
                }
            }

            public bool IsOpen(string door) => _open.Contains(door);

            public void Open(string door)
            {
                if (_open.Add(door))
                {
                    Moved(door);
                }
            }

            public void Shut(string door)
            {
                if (_open.Remove(door))
                {
                    Moved(door);
                }
            }

            public bool CanSee(Vector3 eye, Vector3 target) => Math.Abs(eye.Y - target.Y) < 1.5f;

            public bool InTheWay(Vector3 position, Vector3 facing, float reach) => false;

            public void Sense(IEnumerable<Person> people)
            {
                foreach (var person in people.Where(p => p.Walker.Moving))
                {
                    foreach (var (alarm, zone, spec) in _zones.Where(z => z.Spec.Kind == AlarmZoneKind.Motion))
                    {
                        var above = spec.Position.Y - person.Position.Y;
                        if (above > 1f && above < 3.4f && MotionSensor.Covers(spec.Position, MotionSensor.Facing(spec.Facing), person.Chest))
                        {
                            alarm.Detected(zone);
                        }
                    }
                }
            }

            private void Moved(string door)
            {
                foreach (var (alarm, zone, _) in _zones.Where(z => z.Spec.Door == door))
                {
                    alarm.Detected(zone);
                }
            }
        }

        private sealed class Village
        {
            public Village()
            {
                var town = GeneratedVillage.Town;
                Centre = new AlarmReceivingCentre();
                foreach (var spec in town.Alarms)
                {
                    var alarm = new BurglarAlarm(spec.Zones.Count);
                    Alarms[spec.Name] = alarm;
                    var doors = Enumerable.Range(0, spec.Zones.Count).Where(i => spec.Zones[i].Kind == AlarmZoneKind.Door).ToList();
                    Centre.Add(spec.Name, alarm, spec.Zones.Select(z => z.Name).ToList(), doors);
                }

                World = new World(town, Alarms);
                People = new TownPeople(town, Centre, seed: 7);
            }

            public AlarmReceivingCentre Centre { get; }

            public Dictionary<string, BurglarAlarm> Alarms { get; } = new Dictionary<string, BurglarAlarm>();

            public World World { get; }

            public TownPeople People { get; }

            public Vector3 Point(string stop) => GeneratedVillage.Town.Walking.Point(GeneratedVillage.Town.Walking.Find(stop));

            public void SetAndLeave(string site)
            {
                Alarms[site].Set(BurglarAlarm.DefaultCode);
                Run(BurglarAlarm.EntryExitSeconds + 1f);
            }

            public void Run(float seconds) => RunUntil(() => false, seconds, mustHappen: false);

            public void RunUntil(Func<bool> done, float seconds, bool mustHappen = true)
            {
                for (var t = 0f; t < seconds; t += Step)
                {
                    if (done())
                    {
                        return;
                    }

                    foreach (var alarm in Alarms.Values)
                    {
                        alarm.Tick(Step);
                    }

                    Centre.Tick(Step, 22f);
                    World.Sense(People.People);
                    People.Tick(Step, World);
                }

                Assert.That(done() || !mustHappen, Is.True, $"still waiting after {seconds} s");
            }
        }
    }
}
