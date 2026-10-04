using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Simulation.Audio;
using Townscape.Simulation.Security;

namespace Townscape.Tests.Simulation
{
    public sealed class BurglarAlarmTests
    {
        private const string Code = BurglarAlarm.DefaultCode;
        private const string Engineer = BurglarAlarm.DefaultEngineerCode;

        // Runs the alarm on in small steps, as frames would, and counts the beeps.
        private static int Run(BurglarAlarm alarm, float seconds, float step = 0.05f)
        {
            var beeps = 0;
            for (var t = 0f; t < seconds - 1e-4f; t += step)
            {
                beeps += alarm.Tick(Math.Min(step, seconds - t));
            }

            return beeps;
        }

        [Test]
        public void SettingIt_TakesTheCode_AndGivesThirtySecondsToGetOut()
        {
            var alarm = new BurglarAlarm();

            Assert.That(alarm.Set("0000"), Is.EqualTo(KeypadResult.WrongCode));
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Unset));
            Assert.That(alarm.Set(Code), Is.EqualTo(KeypadResult.Done));
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Exiting));
            Assert.That(alarm.Remaining, Is.EqualTo(BurglarAlarm.EntryExitSeconds));

            // Walking about on the way out sets nothing off.
            alarm.Detected(0);
            Run(alarm, 29.5f);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Exiting));
            Run(alarm, 1f);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Set));
            Assert.That(alarm.Set(Code), Is.EqualTo(KeypadResult.NothingToDo));
        }

        [Test]
        public void ThePanel_BeepsEverySecond_AndFasterForTheLastTen()
        {
            var alarm = new BurglarAlarm();
            alarm.Set(Code);

            Assert.That(Run(alarm, 20f), Is.EqualTo(20), "once a second from 30 down to 11");
            Assert.That(Run(alarm, 10f), Is.EqualTo(20), "twice a second from 10 down");
            Assert.That(Run(alarm, 5f), Is.Zero, "quiet once it's set");
        }

        [Test]
        public void ComingIn_GivesThirtySeconds_ToPutTheCodeIn()
        {
            var alarm = new BurglarAlarm();
            alarm.Set(Code);
            Run(alarm, 31f);

            alarm.Detected(0);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Entry));
            Run(alarm, 20f);
            alarm.Detected(0);
            Assert.That(alarm.Remaining, Is.EqualTo(10f).Within(0.01f), "seeing you again doesn't restart the clock");

            Assert.That(alarm.Unset("5555"), Is.EqualTo(KeypadResult.WrongCode));
            Assert.That(alarm.Unset(Code), Is.EqualTo(KeypadResult.Done));
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Unset));
            Assert.That(alarm.Armed, Is.False);
        }

        [Test]
        public void TooLate_TheBellRings_ForTwentyMinutes_AndTheStrobeFlashesOn()
        {
            var alarm = new BurglarAlarm();
            alarm.Set(Code);
            Run(alarm, 31f);
            alarm.Detected(0);
            Run(alarm, 30.5f);

            Assert.That(alarm.State, Is.EqualTo(AlarmState.Sounding));
            Assert.That(alarm.BellRinging, Is.True);
            Assert.That(alarm.StrobeFlashing, Is.True);

            Run(alarm, BurglarAlarm.BellSeconds, step: 1f);
            Assert.That(alarm.BellRinging, Is.False, "the bell cuts out");
            Assert.That(alarm.StrobeFlashing, Is.True, "the strobe goes on until it's unset");

            Assert.That(alarm.Unset(Code), Is.EqualTo(KeypadResult.Done));
            Assert.That(alarm.StrobeFlashing, Is.False);
            Assert.That(alarm.Unset(Code), Is.EqualTo(KeypadResult.NothingToDo));
        }

        // Set, with nobody about.
        private static BurglarAlarm SetAlarm(int zones = 3)
        {
            var alarm = new BurglarAlarm(zones);
            alarm.Set(Code);
            Run(alarm, 31f);
            return alarm;
        }

        [Test]
        public void ACutZone_SeesNothing()
        {
            var alarm = SetAlarm();
            alarm.ConnectZone(1, false);

            alarm.Detected(1);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Set), "the cut sensor");
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.None), "and nothing on the keypad gives it away");

            alarm.Detected(2);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Entry), "the others still work");
        }

        [Test]
        public void OpeningTheControlBox_WithoutTheEngineerCode_SetsOffTheTamper()
        {
            var alarm = new BurglarAlarm();
            alarm.OpenLid();

            Assert.That(alarm.State, Is.EqualTo(AlarmState.Sounding), "even when it's unset");
            Assert.That(alarm.Tampered, Is.True);
            Assert.That(alarm.BellRinging, Is.True);
            Assert.That(alarm.Unset(Code), Is.EqualTo(KeypadResult.Done));
            Assert.That(alarm.BellRinging, Is.False);
        }

        [Test]
        public void TheEngineerCode_LetsTheLidOff_AndWontLeaveUntilItsBackOn()
        {
            var alarm = new BurglarAlarm();

            Assert.That(alarm.Engineer(Code), Is.EqualTo(KeypadResult.WrongCode), "the user's code won't do");
            Assert.That(alarm.Engineer(Engineer), Is.EqualTo(KeypadResult.Done));
            Assert.That(alarm.EngineerMode, Is.True);
            Assert.That(alarm.Set(Code), Is.EqualTo(KeypadResult.EngineerMode), "no setting it with an engineer at work");

            alarm.OpenLid();
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Unset), "no tamper");
            Assert.That(alarm.Engineer(Engineer), Is.EqualTo(KeypadResult.CloseTheLid));
            alarm.CloseLid();
            Assert.That(alarm.Engineer(Engineer), Is.EqualTo(KeypadResult.Done));
            Assert.That(alarm.EngineerMode, Is.False);

            // Only when it's unset.
            alarm.Set(Code);
            Assert.That(alarm.Engineer(Engineer), Is.EqualTo(KeypadResult.NothingToDo));
        }

        [Test]
        public void WithoutMains_ItRunsTenMinutesOnItsBattery_ThenTheBellBoxSoundsForTwo()
        {
            var alarm = SetAlarm();
            alarm.ConnectMains(false);

            Assert.That(alarm.Powered, Is.True);
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.Mains));
            Run(alarm, BurglarAlarm.BatterySeconds - 1f, step: 0.5f);
            Assert.That(alarm.Powered, Is.True, "still going on the battery");
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Set));

            Run(alarm, 2f);
            Assert.That(alarm.Powered, Is.False, "the battery's flat: the panel dies");
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.None), "and the keypad goes blank");
            Assert.That(alarm.BellRinging, Is.True, "the bell box sounds on its own battery");
            Assert.That(alarm.StrobeFlashing, Is.True);
            Assert.That(alarm.Set(Code), Is.EqualTo(KeypadResult.NoPower));

            Run(alarm, BurglarAlarm.SelfActivateSeconds, step: 0.5f);
            Assert.That(alarm.BellRinging, Is.False, "for two minutes");
            Assert.That(alarm.StrobeFlashing, Is.False);

            // The mains back: it starts up again, unset, and charges its battery.
            alarm.ConnectMains(true);
            Assert.That(alarm.Powered, Is.True);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Unset));
            Run(alarm, 60f, step: 1f);
            Assert.That(alarm.BatteryLeft, Is.EqualTo(60f).Within(0.1f));
        }

        [Test]
        public void WithoutMainsOrBattery_ThePanelDiesAtOnce()
        {
            var alarm = SetAlarm();
            alarm.ConnectBattery(false);
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.Battery), "the fault light for the battery");

            alarm.ConnectMains(false);
            Assert.That(alarm.Powered, Is.False);
            Assert.That(alarm.BellRinging, Is.True, "the bell box sounds on its own");

            // Power back while it's sounding: the panel takes over and quietens it.
            alarm.ConnectBattery(true);
            Assert.That(alarm.Powered, Is.True);
            Assert.That(alarm.BellRinging, Is.False);
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.Mains));
        }

        [Test]
        public void UnpluggingTheNetwork_IsACommsFault()
        {
            var alarm = new BurglarAlarm();
            alarm.ConnectEthernet(false);

            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.Comms));
            alarm.ConnectEthernet(true);
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.None));
        }

        [Test]
        public void TheBellBoxWiredBackwards_BlowsItsFuse_AndItNeverSoundsUntilPutRight()
        {
            var alarm = new BurglarAlarm(3);
            alarm.ReverseBell(true);

            Assert.That(alarm.FuseBlown, Is.True, "the fuse goes at once");
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.Bell));

            // Broken in: it sees you, the entry time runs out, and nothing happens outside.
            alarm.Set(Code);
            Run(alarm, 31f);
            alarm.Detected(0);
            Run(alarm, 31f);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Sounding));
            Assert.That(alarm.BellRinging, Is.False);
            Assert.That(alarm.StrobeFlashing, Is.False);

            // Nor when the panel dies.
            alarm.ConnectMains(false);
            alarm.ConnectBattery(false);
            Assert.That(alarm.BellRinging, Is.False);
            alarm.ConnectMains(true);
            alarm.ConnectBattery(true);

            // A new fuse alone blows again; wired right and with a new fuse, it's mended.
            alarm.FitNewFuse();
            Assert.That(alarm.FuseBlown, Is.True);
            alarm.ReverseBell(false);
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.Bell), "still a dead fuse");
            alarm.FitNewFuse();
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.None));
            alarm.OpenLid();
            Assert.That(alarm.BellRinging, Is.True, "and it sounds again");
        }

        [Test]
        public void ASensorInTheCorner_SeesTheWholeRoom_ButNotBehindIt()
        {
            // High in the back-left corner of a 4 by 4 metre room, looking at the far corner.
            var sensor = new Vector3(0.1f, 2.4f, 0.1f);
            var facing = MotionSensor.Facing(new Vector3(1f, 0f, 1f));

            Assert.That(facing.Y, Is.LessThan(0f), "tilted down into the room");
            for (var x = 0.4f; x < 4f; x += 0.4f)
            {
                for (var z = 0.4f; z < 4f; z += 0.4f)
                {
                    Assert.That(MotionSensor.Covers(sensor, facing, new Vector3(x, 1.2f, z)), Is.True, $"({x}, {z})");
                }
            }

            Assert.That(MotionSensor.Covers(sensor, facing, new Vector3(-2f, 1.2f, -2f)), Is.False, "behind the wall it's on");
            Assert.That(MotionSensor.Covers(sensor, facing, new Vector3(12f, 1.2f, 12f)), Is.False, "out of range");
        }

        [Test]
        public void TheSounder_WailsHighAndPiercing_AndLoopsWithoutAJoin()
        {
            var rate = ProceduralSounds.SampleRate;
            var sounder = AlarmSounds.Sounder();
            var rms = MathF.Sqrt(sounder.Average(s => s * s));

            Assert.That(sounder.Length, Is.EqualTo(2 * rate));
            Assert.That(sounder.Max(MathF.Abs), Is.LessThanOrEqualTo(0.9f));
            Assert.That(rms, Is.GreaterThan(0.35f), "loud: a sounder, not a chirp");

            // The join is as smooth as anywhere else: the loop ends where it starts.
            var steps = Enumerable.Range(1, sounder.Length - 1).Select(i => MathF.Abs(sounder[i] - sounder[i - 1])).Max();
            Assert.That(MathF.Abs(sounder[0] - sounder[sounder.Length - 1]), Is.LessThanOrEqualTo(steps));

            // Its pitch, from rising zero crossings in 20 ms windows, sweeps between the low and high notes.
            var window = rate / 50;
            var pitches = Enumerable.Range(0, sounder.Length / window).Select(w =>
                Enumerable.Range((w * window) + 1, window - 1).Count(i => sounder[i - 1] < 0f && sounder[i] >= 0f) * 50f).ToList();
            Assert.That(pitches.Min(), Is.InRange(AlarmSounds.SweepLow - 150f, AlarmSounds.SweepLow + 400f));
            Assert.That(pitches.Max(), Is.InRange(AlarmSounds.SweepHigh - 400f, AlarmSounds.SweepHigh + 150f));

            // Up and down five times a second: the pitch turns round twenty times in two seconds.
            var turns = Enumerable.Range(2, pitches.Count - 2).Count(i => Math.Sign(pitches[i] - pitches[i - 1]) * Math.Sign(pitches[i - 1] - pitches[i - 2]) < 0);
            Assert.That(turns, Is.InRange(16, 26));
        }

        [Test]
        public void TheBeep_IsShort_AndStartsAndEndsQuietly()
        {
            var beep = AlarmSounds.Beep();

            Assert.That(beep.Length / (float)ProceduralSounds.SampleRate, Is.InRange(0.05f, 0.2f));
            Assert.That(MathF.Abs(beep[0]), Is.LessThan(0.01f));
            Assert.That(MathF.Abs(beep[beep.Length - 1]), Is.LessThan(0.01f));
            Assert.That(beep.Max(MathF.Abs), Is.InRange(0.3f, 0.7f));
        }
    }
}
