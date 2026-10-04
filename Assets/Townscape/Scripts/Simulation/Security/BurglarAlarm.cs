namespace Townscape.Simulation.Security
{
    public enum AlarmState
    {
        /// <summary>Off: the sensors can see you and nothing happens.</summary>
        Unset,

        /// <summary>Just set: the exit time is counting down while you get out.</summary>
        Exiting,

        /// <summary>Set: the next thing a sensor sees starts the entry time.</summary>
        Set,

        /// <summary>Someone's come in: the entry time is counting down for the code to go in.</summary>
        Entry,

        /// <summary>Nobody put the code in: the bell rings and the strobe flashes.</summary>
        Sounding,
    }

    /// <summary>What the panel made of a code.</summary>
    public enum KeypadResult
    {
        Done,
        WrongCode,

        /// <summary>The right code, but the alarm was already set (or already unset).</summary>
        NothingToDo,
    }

    /// <summary>
    /// A house burglar alarm, as its control panel sees it. Putting the code in and pressing Set
    /// starts a 30 second exit time to get out; once it's set, anything a sensor sees starts a 30
    /// second entry time to reach the panel and put the code in again. If nobody does, the bell
    /// rings and the strobe flashes. The bell stops after 20 minutes, as the law asks of an alarm
    /// bell, but the strobe goes on flashing until the alarm is unset. The panel beeps once a second
    /// through the exit and entry times, and twice a second for the last ten.
    /// </summary>
    public sealed class BurglarAlarm
    {
        public const float EntryExitSeconds = 30f;
        public const float BellSeconds = 20f * 60f;
        public const float HurrySeconds = 10f;
        public const string DefaultCode = "1234";

        private readonly string _code;
        private float _nextBeep;

        public BurglarAlarm(string code = DefaultCode)
        {
            _code = code;
        }

        public AlarmState State { get; private set; } = AlarmState.Unset;

        /// <summary>Seconds left of the exit or entry time.</summary>
        public float Remaining { get; private set; }

        /// <summary>How long it has been sounding.</summary>
        public float SoundingFor { get; private set; }

        public bool Counting => State == AlarmState.Exiting || State == AlarmState.Entry;

        public bool BellRinging => State == AlarmState.Sounding && SoundingFor < BellSeconds;

        public bool StrobeFlashing => State == AlarmState.Sounding;

        /// <summary>Whether the alarm is on in any way: setting, set, or going off.</summary>
        public bool Armed => State != AlarmState.Unset;

        public int CodeLength => _code.Length;

        public KeypadResult Set(string code)
        {
            if (code != _code)
            {
                return KeypadResult.WrongCode;
            }

            if (State != AlarmState.Unset)
            {
                return KeypadResult.NothingToDo;
            }

            Count(AlarmState.Exiting);
            return KeypadResult.Done;
        }

        public KeypadResult Unset(string code)
        {
            if (code != _code)
            {
                return KeypadResult.WrongCode;
            }

            if (State == AlarmState.Unset)
            {
                return KeypadResult.NothingToDo;
            }

            State = AlarmState.Unset;
            Remaining = 0f;
            SoundingFor = 0f;
            return KeypadResult.Done;
        }

        /// <summary>A sensor has seen someone, or a door has opened. Only matters once the alarm is set.</summary>
        public void Detected()
        {
            if (State == AlarmState.Set)
            {
                Count(AlarmState.Entry);
            }
        }

        /// <summary>Moves the alarm on by <paramref name="seconds"/>, and returns how many times the panel beeped meanwhile.</summary>
        public int Tick(float seconds)
        {
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

        private void Count(AlarmState state)
        {
            State = state;
            Remaining = EntryExitSeconds;
            _nextBeep = EntryExitSeconds;
        }
    }
}
