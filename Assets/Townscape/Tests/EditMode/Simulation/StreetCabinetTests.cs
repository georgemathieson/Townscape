using System;
using NUnit.Framework;
using Townscape.Simulation.Network;
using Townscape.Simulation.Security;

namespace Townscape.Tests.Simulation
{
    public sealed class StreetCabinetTests
    {
        private static StreetCabinet Cabinet(int seed = 3) =>
            new StreetCabinet(new[] { ("The Copper Kettle", 500f, 500f), ("The flat", 900f, 900f) }, seed);

        private static void Run(StreetCabinet cabinet, float seconds, float step = 0.1f)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += step)
            {
                cabinet.Tick(Math.Min(step, seconds - t));
            }
        }

        [Test]
        public void EveryCustomer_StartsOnline_OnTheirOwnPort()
        {
            var cabinet = Cabinet();

            Assert.That(cabinet.Lines.Count, Is.EqualTo(2));
            Assert.That(cabinet.IndexOf("The flat"), Is.EqualTo(1));
            Assert.That(cabinet.Lines[1].Port, Is.EqualTo(2));
            for (var i = 0; i < cabinet.Lines.Count; i++)
            {
                Assert.That(cabinet.InternetUp(i), Is.True);
                Assert.That(cabinet.Ont(i).Fibre, Is.EqualTo(Led.Green));
                Assert.That(cabinet.Ont(i).Los, Is.EqualTo(Led.Off));
                Assert.That(cabinet.Router(i).Internet, Is.EqualTo(Led.Green));
            }
        }

        [Test]
        public void Unpatching_BlinksTheFaultLights_AndPatchingBack_SyncsAfterAFewSeconds()
        {
            var cabinet = Cabinet();

            cabinet.Patch(0, false);
            Assert.That(cabinet.InternetUp(0), Is.False);
            Assert.That(cabinet.InternetUp(1), Is.True, "only that customer's line goes");
            Assert.That(cabinet.Ont(0).Los, Is.EqualTo(Led.RedBlink));
            Assert.That(cabinet.Ont(0).Fibre, Is.EqualTo(Led.Off));
            Assert.That(cabinet.Router(0).Internet, Is.EqualTo(Led.RedBlink));
            Assert.That(cabinet.RunTest(0).Connected, Is.False);
            Assert.That(cabinet.RunTest(0).RxDbm, Is.EqualTo(StreetCabinet.NoLightDbm));

            cabinet.Patch(0, true);
            Assert.That(cabinet.InternetUp(0), Is.False, "it has to get back in sync first");
            Assert.That(cabinet.Ont(0).Los, Is.EqualTo(Led.Off));
            Assert.That(cabinet.Ont(0).Fibre, Is.EqualTo(Led.GreenBlink));
            Assert.That(cabinet.Router(0).Internet, Is.EqualTo(Led.AmberBlink));

            Run(cabinet, StreetCabinet.RangingSeconds - 0.5f);
            Assert.That(cabinet.InternetUp(0), Is.False);
            Run(cabinet, 1f);
            Assert.That(cabinet.InternetUp(0), Is.True);
            Assert.That(cabinet.Ont(0).Fibre, Is.EqualTo(Led.Green));
            Assert.That(cabinet.Router(0).Internet, Is.EqualTo(Led.Green));
        }

        [Test]
        public void AHealthyLine_TestsNearItsPlan()
        {
            var cabinet = Cabinet();

            var result = cabinet.RunTest(1);

            Assert.That(result.Connected, Is.True);
            Assert.That(result.DownMbps, Is.InRange(800f, 900f));
            Assert.That(result.UpMbps, Is.InRange(800f, 900f));
            Assert.That(result.PingMs, Is.LessThan(6f));
            Assert.That(result.LossPercent, Is.EqualTo(0f));
            Assert.That(result.RxDbm, Is.InRange(-10f, -9f));
            Assert.That(result.PortErrors, Is.EqualTo(0));
        }

        [Test]
        public void EachFault_HasItsOwnSignature()
        {
            var cabinet = Cabinet();
            var healthy = cabinet.RunTest(0);

            cabinet.Inject(LineFault.DirtyConnector, 0);
            Run(cabinet, 10f);
            var dirty = cabinet.RunTest(0);
            Assert.That(dirty.RxDbm, Is.LessThan(healthy.RxDbm - 5f), "a dirty connector lets less light through");
            Assert.That(dirty.RxDbm, Is.GreaterThan(StreetCabinet.SensitivityDbm + 5f), "but plenty to stay in sync");
            Assert.That(dirty.DownMbps, Is.LessThan(healthy.DownMbps * 0.7f));
            Assert.That(dirty.LossPercent, Is.InRange(1f, 3f));
            Assert.That(cabinet.RunTest(1).DownMbps, Is.GreaterThan(800f), "the other line is fine");

            cabinet.Inject(LineFault.FailingPort, 0);
            var before = cabinet.Lines[0].PortErrors;
            Run(cabinet, 10f);
            var port = cabinet.RunTest(0);
            Assert.That(port.RxDbm, Is.GreaterThan(-10f), "the light's fine");
            Assert.That(port.PortErrors - before, Is.GreaterThan(200), "but the port counts errors");
            Assert.That(port.LossPercent, Is.InRange(4f, 8f));

            cabinet.Inject(LineFault.CongestedUplink);
            var errors = cabinet.Lines[1].PortErrors;
            Run(cabinet, 10f);
            for (var i = 0; i < 2; i++)
            {
                var congested = cabinet.RunTest(i);
                Assert.That(congested.PingMs, Is.GreaterThan(80f), "every line's pings are long");
                Assert.That(congested.DownMbps, Is.LessThan(congested.UpMbps), "downloads suffer most");
                Assert.That(congested.RxDbm, Is.GreaterThan(-10f));
            }

            Assert.That(cabinet.Lines[1].PortErrors, Is.EqualTo(errors), "no errors from congestion");
        }

        [Test]
        public void ABentFibre_KeepsDropping_WithHeavyLoss()
        {
            var cabinet = Cabinet();
            cabinet.Inject(LineFault.BentFibre, 1);

            var test = cabinet.RunTest(1);
            Assert.That(test.RxDbm, Is.LessThan(-20f));
            Assert.That(test.RxDbm, Is.GreaterThan(StreetCabinet.SensitivityDbm));
            Assert.That(test.LossPercent, Is.GreaterThanOrEqualTo(12f));
            Assert.That(test.JitterMs, Is.GreaterThan(10f));

            // Within half a minute it loses the light, blinks its fault lights, then comes back.
            var dropped = false;
            var back = false;
            for (var t = 0f; t < 40f; t += 0.1f)
            {
                cabinet.Tick(0.1f);
                if (!cabinet.Lines[1].HasLight)
                {
                    dropped = true;
                    Assert.That(cabinet.Ont(1).Los, Is.EqualTo(Led.RedBlink));
                    Assert.That(cabinet.InternetUp(1), Is.False);
                }
                else if (dropped && cabinet.InternetUp(1))
                {
                    back = true;
                }
            }

            Assert.That(dropped, Is.True);
            Assert.That(back, Is.True);
        }

        [Test]
        public void NamingTheFault_FixesIt_AndAWrongGuessDoesNot()
        {
            var cabinet = Cabinet();
            cabinet.Inject(LineFault.FailingPort, 1);

            Assert.That(cabinet.Diagnose(1, LineFault.DirtyConnector), Is.False);
            Assert.That(cabinet.Diagnose(0, LineFault.FailingPort), Is.False, "the other line is healthy");
            Assert.That(cabinet.Misdiagnosed, Is.EqualTo(2));
            Assert.That(cabinet.Fault, Is.EqualTo(LineFault.FailingPort));

            Assert.That(cabinet.Diagnose(0, LineFault.None), Is.True, "nothing wrong with that one");
            Assert.That(cabinet.Diagnose(1, LineFault.FailingPort), Is.True);
            Assert.That(cabinet.Fault, Is.EqualTo(LineFault.None));
            Assert.That(cabinet.Diagnosed, Is.EqualTo(1));
            Assert.That(cabinet.RunTest(1).LossPercent, Is.EqualTo(0f));

            // A congested uplink can be named from any line.
            cabinet.Inject(LineFault.CongestedUplink);
            Assert.That(cabinet.FaultOn(0), Is.EqualTo(LineFault.CongestedUplink));
            Assert.That(cabinet.Diagnose(0, LineFault.CongestedUplink), Is.True);
            Assert.That(cabinet.Diagnosed, Is.EqualTo(2));
        }

        [Test]
        public void FaultsTurnUpOnTheirOwn_OneAtATime_AfterAQuietSpell()
        {
            var cabinet = Cabinet(seed: 11);

            Run(cabinet, StreetCabinet.QuietSeconds - 1f, 1f);
            Assert.That(cabinet.Fault, Is.EqualTo(LineFault.None), "nothing breaks straight away");

            Run(cabinet, 3600f, 1f);
            Assert.That(cabinet.Fault, Is.Not.EqualTo(LineFault.None), "an hour is plenty for something to go wrong");
            var first = (cabinet.Fault, cabinet.FaultLine);

            Run(cabinet, 600f, 1f);
            Assert.That((cabinet.Fault, cabinet.FaultLine), Is.EqualTo(first), "and nothing else breaks until it's found");
        }

        [Test]
        public void TheAlarm_ShowsACommsFault_WhileItsBroadbandIsDown()
        {
            var alarm = new BurglarAlarm();

            alarm.ConnectInternet(false);
            Assert.That(alarm.Faults.HasFlag(AlarmFaults.Comms), Is.True);

            alarm.ConnectInternet(true);
            Assert.That(alarm.Faults, Is.EqualTo(AlarmFaults.None));
        }
    }
}
