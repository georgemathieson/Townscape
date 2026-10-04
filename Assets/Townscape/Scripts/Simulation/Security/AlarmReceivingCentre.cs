using System;
using System.Collections.Generic;
using System.Linq;

namespace Townscape.Simulation.Security
{
    /// <summary>How bad a line in the alarm receiving centre's log is.</summary>
    public enum ArcSeverity
    {
        /// <summary>Routine: set, unset, a fault cleared.</summary>
        Info,

        /// <summary>Something to look at: a fault, the signal path lost.</summary>
        Warning,

        /// <summary>An alarm or a tamper.</summary>
        Alarm,

        /// <summary>The guard's and the police's comings and goings.</summary>
        Response,
    }

    /// <summary>One line in the log.</summary>
    public sealed class ArcEvent
    {
        public ArcEvent(float hour, string site, string text, ArcSeverity severity)
        {
            Hour = hour;
            Site = site;
            Text = text;
            Severity = severity;
        }

        /// <summary>The town's clock when it happened, in hours.</summary>
        public float Hour { get; }

        public string Site { get; }

        public string Text { get; }

        public ArcSeverity Severity { get; }
    }

    /// <summary>A building whose alarm reports to the centre, and what the centre last heard from it.</summary>
    public sealed class ArcSite
    {
        internal ArcSite(string name, BurglarAlarm alarm, IReadOnlyList<string> zones)
        {
            Name = name;
            Alarm = alarm;
            Zones = zones;
        }

        public string Name { get; }

        public BurglarAlarm Alarm { get; }

        /// <summary>Each zone's name, as the control box labels it.</summary>
        public IReadOnlyList<string> Zones { get; }

        /// <summary>What the centre last heard the alarm was doing (it can't hear through a broken signal path).</summary>
        public AlarmState State { get; internal set; }

        /// <summary>The faults it last reported (not the signal path, which the centre sees for itself).</summary>
        public AlarmFaults Faults { get; internal set; }

        /// <summary>The zones it has reported since it was set.</summary>
        public int ReportedZones { get; internal set; }

        /// <summary>False once the centre has stopped hearing from it for <see cref="AlarmReceivingCentre.LostAfterSeconds"/>.</summary>
        public bool Online { get; internal set; } = true;

        internal float Silent { get; set; }

        internal bool Tampered { get; set; }

        /// <summary>The name of zone <paramref name="zone"/>, or "zone n".</summary>
        public string ZoneName(int zone) => zone >= 0 && zone < Zones.Count ? Zones[zone] : $"zone {zone + 1}";
    }

    public enum ResponderStage
    {
        /// <summary>Not sent.</summary>
        None,

        /// <summary>On the way.</summary>
        EnRoute,

        /// <summary>There, looking round.</summary>
        OnSite,

        /// <summary>Been and reported back.</summary>
        Done,
    }

    /// <summary>A key-holding guard, or the police, sent to an incident.</summary>
    public sealed class Responder
    {
        public ResponderStage Stage { get; internal set; }

        /// <summary>Seconds left of the journey, or of looking round.</summary>
        public float Remaining { get; internal set; }
    }

    /// <summary>An alarm at one of the sites, from the first signal until the operator closes it.</summary>
    public sealed class ArcIncident
    {
        internal ArcIncident(ArcSite site, float hour, bool tamper)
        {
            Site = site;
            OpenedHour = hour;
            Tamper = tamper;
        }

        public ArcSite Site { get; }

        public float OpenedHour { get; }

        /// <summary>Set off by the control box's lid rather than a zone.</summary>
        public bool Tamper { get; }

        /// <summary>The zones reported, in order.</summary>
        public List<int> Zones { get; } = new List<int>();

        /// <summary>
        /// Two different zones have reported: very likely someone really is inside. The police only
        /// come to a confirmed alarm (or one a guard has found broken into).
        /// </summary>
        public bool Confirmed { get; internal set; }

        /// <summary>The operator has seen it.</summary>
        public bool Acknowledged { get; internal set; }

        public Responder Guard { get; } = new Responder();

        public Responder Police { get; } = new Responder();

        /// <summary>The guard found signs of a break-in.</summary>
        public bool BreakInFound { get; internal set; }

        /// <summary>Someone put the code in at the keypad after the alarm.</summary>
        public bool CancelledAtKeypad { get; internal set; }

        public bool Closed { get; internal set; }

        /// <summary>What it says in the incident list.</summary>
        public string Summary
        {
            get
            {
                if (Tamper)
                {
                    return "TAMPER at the control box";
                }

                var zones = string.Join(", ", Zones.Select(Site.ZoneName));
                return (Confirmed ? "CONFIRMED ALARM: " : "ALARM: ") + (zones.Length > 0 ? zones : "no zone reported");
            }
        }
    }

    /// <summary>
    /// An alarm receiving centre: the office every alarm in the village reports to. A panel can
    /// only reach it over its building's broadband, so the centre knows what each alarm was doing
    /// when it last heard from it, and notices when a signal path goes quiet. Alarms open
    /// incidents for the operator, who acknowledges them, sends a key-holding guard to look, calls
    /// the police (who only come to a confirmed alarm, or once a guard has found a break-in) and
    /// closes them. A guard on site who finds nothing amiss puts the code in and unsets the alarm.
    /// </summary>
    /// <remarks>
    /// The guard and police are timed here for now: they take a while to get there and a while to
    /// look round. People walking the streets can take their place later through
    /// <see cref="TimedResponders"/>, <see cref="Arrived"/> and <see cref="Checked"/>.
    /// </remarks>
    public sealed class AlarmReceivingCentre
    {
        /// <summary>How long a site can go unheard before the centre calls its signal path lost.</summary>
        public const float LostAfterSeconds = 15f;

        public const float GuardTravelSeconds = 60f;

        public const float PoliceTravelSeconds = 90f;

        /// <summary>How long a guard or the police spend looking round.</summary>
        public const float CheckSeconds = 20f;

        public const int LogLength = 200;

        private readonly List<ArcSite> _sites = new List<ArcSite>();
        private readonly List<ArcIncident> _incidents = new List<ArcIncident>();
        private readonly List<ArcEvent> _log = new List<ArcEvent>();
        private readonly HashSet<string> _breakIns = new HashSet<string>();
        private float _hour;

        public IReadOnlyList<ArcSite> Sites => _sites;

        /// <summary>Every incident, oldest first, closed ones included.</summary>
        public IReadOnlyList<ArcIncident> Incidents => _incidents;

        /// <summary>The log, oldest first.</summary>
        public IReadOnlyList<ArcEvent> Log => _log;

        /// <summary>Incidents nobody has acknowledged yet.</summary>
        public int Unacknowledged => _incidents.Count(i => !i.Closed && !i.Acknowledged);

        /// <summary>Whether this class moves guards and police along on a timer, or something else does.</summary>
        public bool TimedResponders { get; set; } = true;

        /// <summary>Signs up a building's alarm, with each zone's name.</summary>
        public ArcSite Add(string name, BurglarAlarm alarm, IReadOnlyList<string> zones)
        {
            var site = new ArcSite(name, alarm, zones)
            {
                State = alarm.State,
                Faults = Reportable(alarm.Faults),
                ReportedZones = alarm.ActivatedZones.Count,
            };
            _sites.Add(site);
            return site;
        }

        public ArcSite Site(string name) => _sites.FirstOrDefault(s => s.Name == name);

        /// <summary>The open incident at <paramref name="site"/>, if any.</summary>
        public ArcIncident OpenIncident(ArcSite site) => _incidents.LastOrDefault(i => i.Site == site && !i.Closed);

        /// <summary>Someone has broken into <paramref name="site"/> (or stopped): what a guard looking round will find.</summary>
        public void SetBreakIn(string site, bool brokenInto)
        {
            if (brokenInto)
            {
                _breakIns.Add(site);
            }
            else
            {
                _breakIns.Remove(site);
            }
        }

        /// <summary>Listens to every site, and moves the guards and police along. <paramref name="hour"/> is the town's clock, for the log.</summary>
        public void Tick(float deltaTime, float hour)
        {
            _hour = hour;
            foreach (var site in _sites)
            {
                Listen(site, deltaTime);
            }

            if (!TimedResponders)
            {
                return;
            }

            foreach (var incident in _incidents)
            {
                if (incident.Closed)
                {
                    continue;
                }

                Move(incident, incident.Guard, deltaTime, guard: true);
                Move(incident, incident.Police, deltaTime, guard: false);
            }
        }

        public void Acknowledge(ArcIncident incident)
        {
            if (incident.Closed || incident.Acknowledged)
            {
                return;
            }

            incident.Acknowledged = true;
            Write(incident.Site, "Alarm acknowledged by the operator", ArcSeverity.Response);
        }

        /// <summary>Sends a key-holding guard. Returns false if one's already been sent.</summary>
        public bool DispatchGuard(ArcIncident incident)
        {
            if (incident.Closed || incident.Guard.Stage != ResponderStage.None)
            {
                return false;
            }

            incident.Acknowledged = true;
            incident.Guard.Stage = ResponderStage.EnRoute;
            incident.Guard.Remaining = GuardTravelSeconds;
            Write(incident.Site, "Guard dispatched", ArcSeverity.Response);
            return true;
        }

        /// <summary>
        /// Calls the police. They only come to a confirmed alarm, or once a guard has found a
        /// break-in: otherwise it's logged that they won't, and this returns false.
        /// </summary>
        public bool CallPolice(ArcIncident incident)
        {
            if (incident.Closed || incident.Police.Stage != ResponderStage.None)
            {
                return false;
            }

            incident.Acknowledged = true;
            if (!incident.Confirmed && !incident.BreakInFound)
            {
                Write(incident.Site, "Police won't attend an unconfirmed alarm: send a guard to check first", ArcSeverity.Warning);
                return false;
            }

            incident.Police.Stage = ResponderStage.EnRoute;
            incident.Police.Remaining = PoliceTravelSeconds;
            Write(incident.Site, "Police called: a car is on its way", ArcSeverity.Response);
            return true;
        }

        public void Close(ArcIncident incident)
        {
            if (incident.Closed)
            {
                return;
            }

            incident.Closed = true;
            incident.Acknowledged = true;
            Write(incident.Site, "Incident closed", ArcSeverity.Response);
        }

        /// <summary>A guard (or the police) has got there.</summary>
        public void Arrived(ArcIncident incident, bool guard)
        {
            var responder = guard ? incident.Guard : incident.Police;
            if (responder.Stage != ResponderStage.EnRoute)
            {
                return;
            }

            responder.Stage = ResponderStage.OnSite;
            responder.Remaining = CheckSeconds;
            Write(incident.Site, guard ? "Guard on site, checking the building" : "Police on site", ArcSeverity.Response);
        }

        /// <summary>
        /// A guard (or the police) has looked round. A guard who finds nothing amiss unsets the
        /// alarm with the code; one who finds a break-in says so, and the police can be called.
        /// </summary>
        public void Checked(ArcIncident incident, bool guard)
        {
            var responder = guard ? incident.Guard : incident.Police;
            if (responder.Stage != ResponderStage.OnSite)
            {
                return;
            }

            responder.Stage = ResponderStage.Done;
            var site = incident.Site;
            var brokenInto = _breakIns.Contains(site.Name);
            if (!guard)
            {
                Write(site, brokenInto ? "Police: intruder dealt with, building secured" : "Police: building searched, no one found", ArcSeverity.Response);
                return;
            }

            if (brokenInto)
            {
                incident.BreakInFound = true;
                Write(site, "Guard: signs of a break-in! Call the police", ArcSeverity.Alarm);
                return;
            }

            var unset = site.Alarm.Armed && site.Alarm.Unset(BurglarAlarm.DefaultCode) == KeypadResult.Done;
            Write(site, unset ? "Guard: building secure, alarm unset" : site.Alarm.Powered ? "Guard: building secure" : "Guard: building secure, but the alarm has no power", ArcSeverity.Response);
        }

        private void Move(ArcIncident incident, Responder responder, float deltaTime, bool guard)
        {
            if (responder.Stage != ResponderStage.EnRoute && responder.Stage != ResponderStage.OnSite)
            {
                return;
            }

            responder.Remaining -= deltaTime;
            if (responder.Remaining > 0f)
            {
                return;
            }

            if (responder.Stage == ResponderStage.EnRoute)
            {
                Arrived(incident, guard);
            }
            else
            {
                Checked(incident, guard);
            }
        }

        // What reaches the centre from one site: everything that's changed since it last heard,
        // as long as the panel is on and its broadband is up.
        private void Listen(ArcSite site, float deltaTime)
        {
            var alarm = site.Alarm;
            var reachable = alarm.Powered && alarm.EthernetConnected && alarm.InternetUp;
            if (!reachable)
            {
                site.Silent += deltaTime;
                if (site.Online && site.Silent >= LostAfterSeconds)
                {
                    site.Online = false;
                    Write(site, "Signal path lost: can't reach the alarm", ArcSeverity.Warning);
                }

                return;
            }

            site.Silent = 0f;
            if (!site.Online)
            {
                site.Online = true;
                Write(site, "Signal path restored", ArcSeverity.Info);
            }

            var faults = Reportable(alarm.Faults);
            Faults(site, site.Faults, faults);
            site.Faults = faults;

            var state = alarm.State == AlarmState.Entry ? AlarmState.Set : alarm.State;
            var was = site.State;
            var activated = alarm.ActivatedZones;
            if (state != was)
            {
                site.State = state;
                if (state == AlarmState.Set && was != AlarmState.Sounding)
                {
                    Write(site, "Set", ArcSeverity.Info);
                }
                else if (state == AlarmState.Unset)
                {
                    site.ReportedZones = 0;
                    site.Tampered = false;
                    var incident = OpenIncident(site);
                    if (was == AlarmState.Sounding && incident != null)
                    {
                        incident.CancelledAtKeypad = incident.Guard.Stage != ResponderStage.Done;
                    }

                    Write(site, was == AlarmState.Sounding ? "Unset after an alarm" : "Unset", ArcSeverity.Info);
                }
                else if (state == AlarmState.Sounding)
                {
                    Raise(site, alarm.Tampered);
                }
            }
            else if (state == AlarmState.Sounding && alarm.Tampered && !site.Tampered)
            {
                Raise(site, tamper: true);
            }

            // Zones reported while it's in alarm count towards confirming it.
            if (site.State == AlarmState.Sounding && activated.Count > site.ReportedZones)
            {
                var incident = OpenIncident(site);
                for (var i = site.ReportedZones; i < activated.Count; i++)
                {
                    if (incident != null && !incident.Zones.Contains(activated[i]))
                    {
                        incident.Zones.Add(activated[i]);
                        Write(site, $"Zone activated: {site.ZoneName(activated[i])}", ArcSeverity.Alarm);
                    }
                }

                site.ReportedZones = activated.Count;
                if (incident != null && !incident.Confirmed && incident.Zones.Count >= 2)
                {
                    incident.Confirmed = true;
                    Write(site, "Alarm CONFIRMED: two different zones", ArcSeverity.Alarm);
                }
            }
        }

        private void Raise(ArcSite site, bool tamper)
        {
            site.Tampered = tamper;
            var incident = OpenIncident(site);
            if (incident == null)
            {
                incident = new ArcIncident(site, _hour, tamper);
                _incidents.Add(incident);
            }

            var zones = site.Alarm.ActivatedZones;
            foreach (var zone in zones)
            {
                if (!incident.Zones.Contains(zone))
                {
                    incident.Zones.Add(zone);
                }
            }

            site.ReportedZones = zones.Count;
            var what = tamper ? "TAMPER: control box opened" : $"ALARM: {(zones.Count > 0 ? string.Join(", ", zones.Select(site.ZoneName)) : "unknown zone")}";
            Write(site, what, ArcSeverity.Alarm);
            if (!tamper && incident.Zones.Count >= 2)
            {
                incident.Confirmed = true;
                Write(site, "Alarm CONFIRMED: two different zones", ArcSeverity.Alarm);
            }
        }

        private void Faults(ArcSite site, AlarmFaults was, AlarmFaults now)
        {
            void One(AlarmFaults fault, string failed, string restored)
            {
                var before = (was & fault) != 0;
                var after = (now & fault) != 0;
                if (after && !before)
                {
                    Write(site, failed, ArcSeverity.Warning);
                }
                else if (before && !after)
                {
                    Write(site, restored, ArcSeverity.Info);
                }
            }

            One(AlarmFaults.Mains, "Mains failed: running on the battery", "Mains restored");
            One(AlarmFaults.Battery, "Battery fault", "Battery fault cleared");
            One(AlarmFaults.Bell, "Bell box fault", "Bell box fault cleared");
        }

        // The signal path is the centre's own business: it doesn't hear the panel complain about it.
        private static AlarmFaults Reportable(AlarmFaults faults) => faults & ~AlarmFaults.Comms;

        private void Write(ArcSite site, string text, ArcSeverity severity)
        {
            _log.Add(new ArcEvent(_hour, site.Name, text, severity));
            if (_log.Count > LogLength)
            {
                _log.RemoveAt(0);
            }
        }
    }
}
