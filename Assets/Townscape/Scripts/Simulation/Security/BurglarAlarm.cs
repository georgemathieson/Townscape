using System;
using System.Collections.Generic;

namespace Townscape.Simulation.Security
{
    public enum AlarmState
    {
        /// <summary>Off: the sensors can see you and nothing happens.</summary>
        Unset,

        /// <summary>Just set: the exit time is counting down while you get out.</summary>
        Exiting,

        /// <summary>Set: the next thing a zone sees starts the entry time.</summary>
        Set,

        /// <summary>Someone's come in: the entry time is counting down for the code to go in.</summary>
        Entry,

        /// <summary>Nobody put the code in, or the lid was opened: the bell box sounds and its strobe flashes.</summary>
        Sounding,
    }

    /// <summary>What the panel made of a code.</summary>
    public enum KeypadResult
    {
        Done,
        WrongCode,

        /// <summary>The right code, but there was nothing to do (already set, already unset).</summary>
        NothingToDo,

        /// <summary>The panel has no power: the keypad is dead.</summary>
        NoPower,

        /// <summary>The engineer can't leave engineer mode with the control box's lid open.</summary>
        CloseTheLid,

        /// <summary>It can't be set while an engineer is working on it.</summary>
        EngineerMode,
    }

    /// <summary>What's wrong, as the keypad's fault light and screen show it.</summary>
    [Flags]
    public enum AlarmFaults
    {
        None = 0,

        /// <summary>No mains: the panel is running on its battery.</summary>
        Mains = 1,

        /// <summary>The standby battery is disconnected or flat.</summary>
        Battery = 2,

        /// <summary>The panel can't reach the internet to report.</summary>
        Comms = 4,

        /// <summary>The bell box's fuse has blown or its wiring is wrong.</summary>
        Bell = 8,
    }

    /// <summary>
    /// A burglar alarm's control panel. Putting the code in and pressing Set starts a 30 second exit
    /// time; once it's set, anything one of its zones sees (a motion sensor, or a door's contact)
    /// starts a 30 second entry time to put the code in again. If nobody does, the bell box sounds
    /// (a piezo sounder these days, though it's still called the bell) and its strobe flashes. The
    /// sound stops after 20 minutes, as the law asks of an alarm, but the strobe goes on flashing
    /// until the alarm is unset. The panel beeps once a second through the exit and entry times,
    /// and twice a second for the last ten.
    /// </summary>
    /// <remarks>
    /// Inside the control box: a wire to each zone, which can be cut (the zone then sees nothing);
    /// the mains, without which the panel runs on its standby battery for ten minutes; the battery;
    /// the network cable it reports through; and the bell box's power, through a fuse. Wire the bell
    /// box's power the wrong way round and the fuse blows: the bell box is dead until it's wired
    /// right and given a new fuse. When the panel loses all its power the bell box, which has a
    /// battery of its own, sounds for two minutes. The lid has a tamper switch: open it without
    /// the engineer code entered at a keypad and the alarm goes off, set or not.
    /// </remarks>
    public sealed class BurglarAlarm
    {
        public const float EntryExitSeconds = 30f;
        public const float BellSeconds = 20f * 60f;
        public const float HurrySeconds = 10f;

        /// <summary>How long the standby battery keeps the panel going without mains.</summary>
        public const float BatterySeconds = 10f * 60f;

        /// <summary>How long the bell box sounds on its own battery when the panel dies.</summary>
        public const float SelfActivateSeconds = 2f * 60f;

        public const string DefaultCode = "1234";
        public const string DefaultEngineerCode = "9999";

        private readonly string _code;
        private readonly string _engineerCode;
        private readonly bool[] _zones;
        private readonly List<int> _activated = new List<int>();
        private float _nextBeep;
        private bool _wasPowered = true;

        public BurglarAlarm(int zones = 1, string code = DefaultCode, string engineerCode = DefaultEngineerCode)
        {
            _code = code;
            _engineerCode = engineerCode;
            _zones = new bool[Math.Max(0, zones)];
            for (var i = 0; i < _zones.Length; i++)
            {
                _zones[i] = true;
            }
        }

        public AlarmState State { get; private set; } = AlarmState.Unset;

        /// <summary>Seconds left of the exit or entry time.</summary>
        public float Remaining { get; private set; }

        /// <summary>How long it has been sounding.</summary>
        public float SoundingFor { get; private set; }

        /// <summary>Whether what's sounding was set off by the control box's lid.</summary>
        public bool Tampered { get; private set; }

        /// <summary>The engineer code has been put in: the lid can come off without setting it off.</summary>
        public bool EngineerMode { get; private set; }

        public bool LidOpen { get; private set; }

        public int ZoneCount => _zones.Length;

        /// <summary>
        /// The zones that have seen someone since it was set, in the order they did (each once):
        /// the first starts the entry time, and a second, different one is what an alarm
        /// receiving centre counts as a confirmed alarm.
        /// </summary>
        public IReadOnlyList<int> ActivatedZones => _activated;

        public bool MainsConnected { get; private set; } = true;

        public bool BatteryConnected { get; private set; } = true;

        public bool EthernetConnected { get; private set; } = true;

        /// <summary>Whether the building's broadband (the router the network cable goes to) is working.</summary>
        public bool InternetUp { get; private set; } = true;

        /// <summary>The bell box's power is wired the wrong way round.</summary>
        public bool BellReversed { get; private set; }

        public bool FuseBlown { get; private set; }

        /// <summary>Seconds of standby left in the battery.</summary>
        public float BatteryLeft { get; private set; } = BatterySeconds;

        /// <summary>Seconds left of the bell box sounding on its own battery.</summary>
        public float SelfActivating { get; private set; }

        /// <summary>The panel is on, from the mains or its battery.</summary>
        public bool Powered => MainsConnected || (BatteryConnected && BatteryLeft > 0f);

        public bool Counting => Powered && (State == AlarmState.Exiting || State == AlarmState.Entry);

        public bool BellWorks => !FuseBlown;

        public bool BellRinging => BellWorks && (SelfActivating > 0f || (Powered && State == AlarmState.Sounding && SoundingFor < BellSeconds));

        public bool StrobeFlashing => BellWorks && (SelfActivating > 0f || (Powered && State == AlarmState.Sounding));

        /// <summary>Whether the alarm is on in any way: setting, set, or going off.</summary>
        public bool Armed => State != AlarmState.Unset;

        public int CodeLength => _code.Length;

        /// <summary>What the keypad reports; nothing when the panel is off.</summary>
        public AlarmFaults Faults
        {
            get
            {
                if (!Powered)
                {
                    return AlarmFaults.None;
                }

                var faults = AlarmFaults.None;
                faults |= MainsConnected ? AlarmFaults.None : AlarmFaults.Mains;
                faults |= BatteryConnected && BatteryLeft > 0f ? AlarmFaults.None : AlarmFaults.Battery;
                faults |= EthernetConnected && InternetUp ? AlarmFaults.None : AlarmFaults.Comms;
                faults |= FuseBlown || BellReversed ? AlarmFaults.Bell : AlarmFaults.None;
                return faults;
            }
        }

        public bool ZoneConnected(int zone) => zone >= 0 && zone < _zones.Length && _zones[zone];

        public KeypadResult Set(string code)
        {
            if (!Powered)
            {
                return KeypadResult.NoPower;
            }

            if (code != _code)
            {
                return KeypadResult.WrongCode;
            }

            if (EngineerMode)
            {
                return KeypadResult.EngineerMode;
            }

            if (State != AlarmState.Unset)
            {
                return KeypadResult.NothingToDo;
            }

            Count(AlarmState.Exiting);
            return KeypadResult.Done;
        }

        /// <summary>Unsets it, or silences it: the user's code or the engineer's.</summary>
        public KeypadResult Unset(string code)
        {
            if (!Powered)
            {
                return KeypadResult.NoPower;
            }

            if (code != _code && code != _engineerCode)
            {
                return KeypadResult.WrongCode;
            }

            if (State == AlarmState.Unset)
            {
                return KeypadResult.NothingToDo;
            }

            Reset();
            return KeypadResult.Done;
        }

        /// <summary>The engineer code goes into engineer mode when the alarm's unset, and out again once the lid is back on.</summary>
        public KeypadResult Engineer(string code)
        {
            if (!Powered)
            {
                return KeypadResult.NoPower;
            }

            if (code != _engineerCode)
            {
                return KeypadResult.WrongCode;
            }

            if (EngineerMode)
            {
                if (LidOpen)
                {
                    return KeypadResult.CloseTheLid;
                }

                EngineerMode = false;
                return KeypadResult.Done;
            }

            if (State != AlarmState.Unset)
            {
                return KeypadResult.NothingToDo;
            }

            EngineerMode = true;
            return KeypadResult.Done;
        }

        /// <summary>Takes the control box's lid off: the tamper switch sets the alarm off unless an engineer's working on it.</summary>
        public void OpenLid()
        {
            LidOpen = true;
            if (Powered && !EngineerMode)
            {
                State = AlarmState.Sounding;
                Tampered = true;
                Remaining = 0f;
                SoundingFor = 0f;
            }
        }

        public void CloseLid() => LidOpen = false;

        /// <summary>A zone has seen someone (a sensor, or a door opening). Only matters once it's set, and only through a connected zone.</summary>
        public void Detected(int zone)
        {
            if (!Powered || EngineerMode || !ZoneConnected(zone) || State == AlarmState.Unset || State == AlarmState.Exiting)
            {
                return;
            }

            if (!_activated.Contains(zone))
            {
                _activated.Add(zone);
            }

            if (State == AlarmState.Set)
            {
                Count(AlarmState.Entry);
            }
        }

        public void ConnectZone(int zone, bool connected)
        {
            if (zone >= 0 && zone < _zones.Length)
            {
                _zones[zone] = connected;
            }
        }

        public void ConnectMains(bool connected) => Repower(() => MainsConnected = connected);

        public void ConnectBattery(bool connected) => Repower(() => BatteryConnected = connected);

        public void ConnectEthernet(bool connected) => EthernetConnected = connected;

        /// <summary>The building's broadband has come up or gone down: without it the panel can't report.</summary>
        public void ConnectInternet(bool up) => InternetUp = up;

        /// <summary>Wires the bell box's power the wrong way round (or back): with power on, the fuse blows at once.</summary>
        public void ReverseBell(bool reversed)
        {
            BellReversed = reversed;
            BlowIfReversed();
        }

        /// <summary>Puts a new fuse in for the bell box; it blows straight away if the wiring's still wrong.</summary>
        public void FitNewFuse()
        {
            FuseBlown = false;
            BlowIfReversed();
        }

        /// <summary>Moves the alarm on by <paramref name="seconds"/>, and returns how many times the panel beeped meanwhile.</summary>
        public int Tick(float seconds)
        {
            // The battery runs the panel without mains, and charges back up from it.
            if (BatteryConnected)
            {
                BatteryLeft = MainsConnected ? Math.Min(BatterySeconds, BatteryLeft + seconds) : Math.Max(0f, BatteryLeft - seconds);
            }

            SelfActivating = Math.Max(0f, SelfActivating - seconds);
            Notice();
            if (!Powered)
            {
                return 0;
            }

            if (State == AlarmState.Sounding)
            {
                SoundingFor += seconds;
                return 0;
            }

            if (!Counting)
            {
                return 0;
            }

            var remaining = Remaining - seconds;
            var beeps = 0;
            while (_nextBeep > 0f && _nextBeep >= remaining)
            {
                beeps++;
                _nextBeep -= _nextBeep > HurrySeconds + 1e-3f ? 1f : 0.5f;
            }

            if (remaining > 0f)
            {
                Remaining = remaining;
                return beeps;
            }

            Remaining = 0f;
            if (State == AlarmState.Exiting)
            {
                State = AlarmState.Set;
            }
            else
            {
                State = AlarmState.Sounding;
                SoundingFor = -remaining;
            }

            return beeps;
        }

        private void Repower(Action change)
        {
            change();
            Notice();
        }

        // Power coming and going: the panel dies (and the bell box, losing the panel's hold-off,
        // sounds on its own battery) or starts up again, unset.
        private void Notice()
        {
            var powered = Powered;
            if (_wasPowered && !powered)
            {
                Reset();
                EngineerMode = false;
                SelfActivating = BellWorks ? SelfActivateSeconds : 0f;
            }
            else if (!_wasPowered && powered)
            {
                Reset();
                SelfActivating = 0f;
            }

            _wasPowered = powered;
            BlowIfReversed();
        }

        private void BlowIfReversed()
        {
            if (BellReversed && Powered)
            {
                FuseBlown = true;
                SelfActivating = 0f;
            }
        }

        private void Reset()
        {
            _activated.Clear();
            State = AlarmState.Unset;
            Tampered = false;
            Remaining = 0f;
            SoundingFor = 0f;
        }

        private void Count(AlarmState state)
        {
            State = state;
            Remaining = EntryExitSeconds;
            _nextBeep = EntryExitSeconds;
        }
    }
}
