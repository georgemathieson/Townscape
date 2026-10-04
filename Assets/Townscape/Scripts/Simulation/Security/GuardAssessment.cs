using System;
using System.Collections.Generic;
using System.Linq;

namespace Townscape.Simulation.Security
{
    /// <summary>What a guard made of an alarm: a break-in for the police, or a false alarm.</summary>
    public enum GuardVerdict
    {
        FalseAlarm,
        BreakIn,
    }

    /// <summary>One thing the guard weighed up, and how much it counted for or against a break-in.</summary>
    public sealed class SuspicionFactor
    {
        public SuspicionFactor(string reason, int points)
        {
            Reason = reason;
            Points = points;
        }

        /// <summary>What it was: "The shop door was open".</summary>
        public string Reason { get; }

        /// <summary>Positive if it points to a break-in, negative if it points to a false alarm.</summary>
        public int Points { get; }

        public override string ToString() => $"{Reason} ({(Points >= 0 ? "+" : string.Empty)}{Points})";
    }

    /// <summary>A door or window on the outside of the building, and whether the guard found it open.</summary>
    public readonly struct PerimeterOpening
    {
        public PerimeterOpening(string name, bool window, bool open)
        {
            Name = name;
            Window = window;
            Open = open;
        }

        /// <summary>The door's name: "Shop door", "Roof window 1".</summary>
        public string Name { get; }

        public bool Window { get; }

        public bool Open { get; }
    }

    /// <summary>Everything a guard has to go on at a site: what the alarm reported, and what they found.</summary>
    public sealed class SiteEvidence
    {
        /// <summary>The different zones the alarm reported before anyone went in, in order.</summary>
        public IReadOnlyList<string> Zones { get; set; } = Array.Empty<string>();

        /// <summary>A door's contact went first, and a motion sensor after it: someone came in and walked about.</summary>
        public bool DoorThenMotion { get; set; }

        /// <summary>The alarm went off because the control box's lid came off.</summary>
        public bool Tamper { get; set; }

        /// <summary>The control box's lid is still off.</summary>
        public bool LidOpen { get; set; }

        /// <summary>The panel has power, from the mains or its battery.</summary>
        public bool Powered { get; set; } = true;

        /// <summary>What's wrong with the panel (the signal path aside).</summary>
        public AlarmFaults Faults { get; set; }

        /// <summary>Zones whose wire has been cut at the control box.</summary>
        public IReadOnlyList<string> CutZones { get; set; } = Array.Empty<string>();

        /// <summary>The centre stopped hearing from the alarm while it was in alarm.</summary>
        public bool SignalLost { get; set; }

        /// <summary>Someone put the right code in at the keypad after it went off.</summary>
        public bool CodeEntered { get; set; }

        /// <summary>The outside doors and windows the guard checked.</summary>
        public IReadOnlyList<PerimeterOpening> Openings { get; set; } = Array.Empty<PerimeterOpening>();

        /// <summary>The guard saw someone in the building.</summary>
        public bool IntruderSeen { get; set; }

        /// <summary>Someone (the centre, when nobody walks round) already knows there's been a break-in.</summary>
        public bool BreakInReported { get; set; }
    }

    /// <summary>
    /// A guard's judgement of an alarm, as a suspicion score: everything that points to a
    /// break-in adds to it and everything that points to a false alarm takes away. Two zones
    /// going off (the rule the police go by) counts for a lot, but on its own it isn't enough:
    /// it needs something more, such as a door left open, someone seen, the control box got at
    /// or a wire cut. A panel with a fault, or someone putting the right code in, makes a false
    /// alarm likelier. At <see cref="BreakInScore"/> or over, it's a break-in.
    /// </summary>
    public sealed class GuardAssessment
    {
        /// <summary>The score at which the guard calls it a break-in.</summary>
        public const int BreakInScore = 50;

        public const int IntruderPoints = 100;
        public const int BreakInReportedPoints = 60;
        public const int DoorOpenPoints = 40;
        public const int WindowOpenPoints = 25;
        public const int OneZonePoints = 10;
        public const int TwoZonesPoints = 35;
        public const int ManyZonesPoints = 45;
        public const int DoorThenMotionPoints = 10;
        public const int TamperPoints = 25;
        public const int LidOpenPoints = 15;
        public const int CutWirePoints = 20;
        public const int SignalLostPoints = 15;
        public const int FaultPoints = -15;
        public const int NoPowerPoints = -20;
        public const int CodeEnteredPoints = -30;

        private GuardAssessment(IReadOnlyList<SuspicionFactor> factors)
        {
            Factors = factors;
            Score = Math.Max(0, factors.Sum(f => f.Points));
            Verdict = Score >= BreakInScore ? GuardVerdict.BreakIn : GuardVerdict.FalseAlarm;
        }

        /// <summary>Everything that counted, in the order the guard thought of it.</summary>
        public IReadOnlyList<SuspicionFactor> Factors { get; }

        /// <summary>Never below nought; a break-in at <see cref="BreakInScore"/> or over.</summary>
        public int Score { get; }

        public GuardVerdict Verdict { get; }

        public bool BreakIn => Verdict == GuardVerdict.BreakIn;

        /// <summary>One line for the log: "suspicion 95: break-in".</summary>
        public string Summary => $"suspicion {Score}: {(BreakIn ? "break-in" : "false alarm")}";

        public static GuardAssessment Assess(SiteEvidence evidence)
        {
            var factors = new List<SuspicionFactor>();
            void Add(string reason, int points) => factors.Add(new SuspicionFactor(reason, points));

            if (evidence.IntruderSeen)
            {
                Add("Someone was in the building", IntruderPoints);
            }

            if (evidence.BreakInReported)
            {
                Add("Signs of a break-in", BreakInReportedPoints);
            }

            foreach (var opening in evidence.Openings.Where(o => o.Open))
            {
                Add($"{opening.Name} was open", opening.Window ? WindowOpenPoints : DoorOpenPoints);
            }

            // The two-zone rule: one zone is often a spider or a draught; two different ones is
            // what the police call a confirmed alarm.
            var zones = evidence.Zones.Count;
            if (zones == 1)
            {
                Add($"One zone went off ({evidence.Zones[0]})", OneZonePoints);
            }
            else if (zones == 2)
            {
                Add($"Two zones went off ({string.Join(", ", evidence.Zones)})", TwoZonesPoints);
            }
            else if (zones > 2)
            {
                Add($"{zones} zones went off ({string.Join(", ", evidence.Zones)})", ManyZonesPoints);
            }

            if (evidence.DoorThenMotion)
            {
                Add("A door opened, then something moved inside", DoorThenMotionPoints);
            }

            if (evidence.Tamper)
            {
                Add("The control box was tampered with", TamperPoints);
            }

            if (evidence.LidOpen)
            {
                Add("The control box's lid is off", LidOpenPoints);
            }

            foreach (var zone in evidence.CutZones.Take(2))
            {
                Add($"The wire to {zone} has been cut", CutWirePoints);
            }

            if (evidence.SignalLost)
            {
                Add("The alarm stopped reporting while it was going off", SignalLostPoints);
            }

            // Is the alarm itself working? A panel with no power or with a fault is the likeliest
            // cause of an alarm nobody can explain.
            if (!evidence.Powered)
            {
                Add("The panel has no power", NoPowerPoints);
            }
            else if (evidence.Faults != AlarmFaults.None)
            {
                Add($"The panel has a fault ({Describe(evidence.Faults)}): faulty alarms go off on their own", FaultPoints);
            }

            if (evidence.CodeEntered)
            {
                Add("Someone put the right code in after it went off", CodeEnteredPoints);
            }

            return new GuardAssessment(factors);
        }

        private static string Describe(AlarmFaults faults)
        {
            var names = new List<string>();
            if ((faults & AlarmFaults.Mains) != 0)
            {
                names.Add("mains");
            }

            if ((faults & AlarmFaults.Battery) != 0)
            {
                names.Add("battery");
            }

            if ((faults & AlarmFaults.Bell) != 0)
            {
                names.Add("bell box");
            }

            if ((faults & AlarmFaults.Comms) != 0)
            {
                names.Add("comms");
            }

            return string.Join(", ", names);
        }
    }
}
