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
            alarm.Detected();
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

            alarm.Detected();
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Entry));
            Run(alarm, 20f);
            alarm.Detected();
            Assert.That(alarm.Remaining, Is.EqualTo(10f).Within(0.01f), "seeing you again doesn't restart the clock");

            Assert.That(alarm.Unset("9999"), Is.EqualTo(KeypadResult.WrongCode));
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
            alarm.Detected();
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
        public void TheBell_RingsLoudly_AndLoopsWithoutAJoin()
        {
            var bell = AlarmSounds.Bell(7);
            var rms = MathF.Sqrt(bell.Average(s => s * s));
            var steps = Enumerable.Range(1, bell.Length - 1).Select(i => MathF.Abs(bell[i] - bell[i - 1])).ToList();
            var join = MathF.Abs(bell[0] - bell[bell.Length - 1]);

            Assert.That(bell.Length, Is.EqualTo(2 * ProceduralSounds.SampleRate));
            Assert.That(bell.Max(MathF.Abs), Is.LessThanOrEqualTo(0.86f));
            Assert.That(rms, Is.GreaterThan(0.12f), "a bell, not a tinkle");
            Assert.That(join, Is.LessThan(steps.Max()), "the loop joins like any other step");

            // Twenty strikes a second: the level rises and falls with the hammer.
            var blocks = Enumerable.Range(0, 40).Select(b => bell.Skip(b * 1600).Take(1600).Max(MathF.Abs)).ToList();
            Assert.That(blocks.Min(), Is.GreaterThan(0.3f), "it rings on between strikes");
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
