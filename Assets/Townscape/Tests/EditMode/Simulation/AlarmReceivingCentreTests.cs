using System;
using System.Linq;
using NUnit.Framework;
using Townscape.Simulation.Security;

namespace Townscape.Tests.Simulation
{
    public sealed class AlarmReceivingCentreTests
    {
        private const string Code = BurglarAlarm.DefaultCode;
        private static readonly string[] Zones = { "Front door", "Hall sensor", "Kitchen sensor" };

        private static (AlarmReceivingCentre Arc, BurglarAlarm Alarm, ArcSite Site) Centre()
        {
            var arc = new AlarmReceivingCentre();
            var alarm = new BurglarAlarm(Zones.Length);
            var site = arc.Add("The flat", alarm, Zones);
            return (arc, alarm, site);
        }

        private static void Run(AlarmReceivingCentre arc, BurglarAlarm alarm, float seconds, float step = 0.5f)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += step)
            {
                var dt = Math.Min(step, seconds - t);
                alarm.Tick(dt);
                arc.Tick(dt, 21.5f);
            }
        }

        private static bool Logged(AlarmReceivingCentre arc, string text) => arc.Log.Any(e => e.Text.Contains(text));

        // Sets the alarm and lets the exit time run out.
        private static void SetAndLeave(AlarmReceivingCentre arc, BurglarAlarm alarm)
        {
            alarm.Set(Code);
            Run(arc, alarm, BurglarAlarm.EntryExitSeconds + 1f);
        }

        // Someone comes in through the zones given, and nobody puts the code in.
        private static void BreakIn(AlarmReceivingCentre arc, BurglarAlarm alarm, params int[] zones)
        {
            foreach (var zone in zones)
            {
                alarm.Detected(zone);
            }

            Run(arc, alarm, BurglarAlarm.EntryExitSeconds + 1f);
        }

        [Test]
        public void SettingAndUnsetting_AreLogged_ButNotTheEntryTime()
        {
            var (arc, alarm, site) = Centre();

            SetAndLeave(arc, alarm);
            Assert.That(site.State, Is.EqualTo(AlarmState.Set));
            Assert.That(arc.Log.Last().Text, Is.EqualTo("Set"));

            alarm.Detected(0);
            Run(arc, alarm, 5f);
            Assert.That(site.State, Is.EqualTo(AlarmState.Set), "the entry time isn't signalled");

            alarm.Unset(Code);
            Run(arc, alarm, 1f);
            Assert.That(site.State, Is.EqualTo(AlarmState.Unset));
            Assert.That(arc.Log.Last().Text, Is.EqualTo("Unset"));
            Assert.That(arc.Incidents, Is.Empty);
        }

        [Test]
        public void AnAlarm_OpensAnIncident_AndASecondZoneConfirmsIt()
        {
            var (arc, alarm, site) = Centre();
            SetAndLeave(arc, alarm);

            BreakIn(arc, alarm, 0);
            var incident = arc.OpenIncident(site);
            Assert.That(incident, Is.Not.Null);
            Assert.That(incident.Confirmed, Is.False);
            Assert.That(incident.Summary, Is.EqualTo("ALARM: Front door"));
            Assert.That(arc.Unacknowledged, Is.EqualTo(1));

            alarm.Detected(1);
            Run(arc, alarm, 1f);
            Assert.That(incident.Confirmed, Is.True);
            Assert.That(incident.Summary, Is.EqualTo("CONFIRMED ALARM: Front door, Hall sensor"));
            Assert.That(Logged(arc, "Zone activated: Hall sensor"), Is.True);
            Assert.That(arc.Incidents, Has.Count.EqualTo(1), "still the one incident");

            arc.Acknowledge(incident);
            Assert.That(arc.Unacknowledged, Is.EqualTo(0));
        }

        [Test]
        public void ThePolice_WontComeToAnUnconfirmedAlarm_ButAGuardChecksAndUnsetsIt()
        {
            var (arc, alarm, site) = Centre();
            SetAndLeave(arc, alarm);
            BreakIn(arc, alarm, 0);
            var incident = arc.OpenIncident(site);

            Assert.That(arc.CallPolice(incident), Is.False);
            Assert.That(incident.Police.Stage, Is.EqualTo(ResponderStage.None));
            Assert.That(Logged(arc, "won't attend an unconfirmed alarm"), Is.True);

            Assert.That(arc.DispatchGuard(incident), Is.True);
            Assert.That(arc.DispatchGuard(incident), Is.False, "only one guard");
            Run(arc, alarm, AlarmReceivingCentre.GuardTravelSeconds - 1f);
            Assert.That(incident.Guard.Stage, Is.EqualTo(ResponderStage.EnRoute));
            Run(arc, alarm, 2f);
            Assert.That(incident.Guard.Stage, Is.EqualTo(ResponderStage.OnSite));
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Sounding));

            Run(arc, alarm, AlarmReceivingCentre.CheckSeconds + 1f);
            Assert.That(incident.Guard.Stage, Is.EqualTo(ResponderStage.Done));
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Unset), "the guard put the code in");
            Assert.That(Logged(arc, "Guard: building secure, alarm unset"), Is.True);
            Assert.That(incident.CancelledAtKeypad, Is.False, "the guard did it, not a stranger");

            arc.Close(incident);
            Assert.That(arc.OpenIncident(site), Is.Null);
        }

        [Test]
        public void AConfirmedAlarm_GetsThePolice()
        {
            var (arc, alarm, site) = Centre();
            SetAndLeave(arc, alarm);
            BreakIn(arc, alarm, 0, 2);
            var incident = arc.OpenIncident(site);

            Assert.That(incident.Confirmed, Is.True);
            Assert.That(arc.CallPolice(incident), Is.True);
            Run(arc, alarm, AlarmReceivingCentre.PoliceTravelSeconds + 1f);
            Assert.That(incident.Police.Stage, Is.EqualTo(ResponderStage.OnSite));
            Run(arc, alarm, AlarmReceivingCentre.CheckSeconds + 1f);
            Assert.That(incident.Police.Stage, Is.EqualTo(ResponderStage.Done));
            Assert.That(Logged(arc, "Police: building searched"), Is.True);
        }

        [Test]
        public void AGuardWhoFindsABreakIn_LetsThePoliceBeCalled()
        {
            var (arc, alarm, site) = Centre();
            SetAndLeave(arc, alarm);
            BreakIn(arc, alarm, 2);
            arc.SetBreakIn("The flat", true);
            var incident = arc.OpenIncident(site);

            arc.DispatchGuard(incident);
            Run(arc, alarm, AlarmReceivingCentre.GuardTravelSeconds + AlarmReceivingCentre.CheckSeconds + 2f);

            Assert.That(incident.BreakInFound, Is.True);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Sounding), "the guard leaves it for the police");
            Assert.That(arc.CallPolice(incident), Is.True);
        }

        [Test]
        public void WithoutBroadband_TheSignalPathIsLost_AndAnAlarmOnlyArrivesOnceItsBack()
        {
            var (arc, alarm, site) = Centre();
            SetAndLeave(arc, alarm);

            alarm.ConnectInternet(false);
            Run(arc, alarm, AlarmReceivingCentre.LostAfterSeconds - 1f);
            Assert.That(site.Online, Is.True, "a moment's silence is nothing");
            Run(arc, alarm, 2f);
            Assert.That(site.Online, Is.False);
            Assert.That(arc.Log.Last().Text, Does.StartWith("Signal path lost"));

            BreakIn(arc, alarm, 0, 1);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Sounding));
            Assert.That(arc.Incidents, Is.Empty, "nobody heard it");

            alarm.ConnectInternet(true);
            Run(arc, alarm, 1f);
            Assert.That(site.Online, Is.True);
            Assert.That(Logged(arc, "Signal path restored"), Is.True);
            Assert.That(arc.OpenIncident(site)?.Confirmed, Is.True);
        }

        [Test]
        public void Faults_AreReported_AndATamperOpensAnIncident()
        {
            var (arc, alarm, site) = Centre();

            alarm.ConnectMains(false);
            Run(arc, alarm, 1f);
            Assert.That(arc.Log.Last().Text, Does.StartWith("Mains failed"));
            Assert.That(arc.Log.Last().Severity, Is.EqualTo(ArcSeverity.Warning));
            alarm.ConnectMains(true);
            Run(arc, alarm, 1f);
            Assert.That(arc.Log.Last().Text, Is.EqualTo("Mains restored"));

            alarm.OpenLid();
            Run(arc, alarm, 1f);
            var incident = arc.OpenIncident(site);
            Assert.That(incident.Tamper, Is.True);
            Assert.That(incident.Summary, Does.StartWith("TAMPER"));
        }

        [Test]
        public void UnsettingAtTheKeypad_AfterAnAlarm_IsNoted()
        {
            var (arc, alarm, site) = Centre();
            SetAndLeave(arc, alarm);
            BreakIn(arc, alarm, 1);

            alarm.Unset(Code);
            Run(arc, alarm, 1f);

            Assert.That(arc.Log.Last().Text, Is.EqualTo("Unset after an alarm"));
            Assert.That(arc.OpenIncident(site).CancelledAtKeypad, Is.True, "it stays open until the operator closes it");
        }
    }
}
