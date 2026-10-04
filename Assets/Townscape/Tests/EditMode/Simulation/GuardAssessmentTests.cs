using System.Linq;
using NUnit.Framework;
using Townscape.Simulation.Security;

namespace Townscape.Tests.Simulation
{
    public sealed class GuardAssessmentTests
    {
        private static PerimeterOpening Door(bool open) => new PerimeterOpening("Shop door", window: false, open);

        [Test]
        public void OneSensor_WithEverythingShut_IsAFalseAlarm()
        {
            var assessment = GuardAssessment.Assess(new SiteEvidence
            {
                Zones = new[] { "Café sensor" },
                Openings = new[] { Door(false) },
            });

            Assert.That(assessment.Verdict, Is.EqualTo(GuardVerdict.FalseAlarm));
            Assert.That(assessment.Score, Is.EqualTo(GuardAssessment.OneZonePoints));
            Assert.That(assessment.Factors.Single().Reason, Does.Contain("Café sensor"));
        }

        [Test]
        public void TwoSensors_AreConfirmed_ButNotEnoughOnTheirOwn()
        {
            var assessment = GuardAssessment.Assess(new SiteEvidence { Zones = new[] { "Hall sensor", "Kitchen sensor" } });

            Assert.That(assessment.Score, Is.EqualTo(GuardAssessment.TwoZonesPoints));
            Assert.That(assessment.Verdict, Is.EqualTo(GuardVerdict.FalseAlarm), "the two-zone rule needs something else to go with it");
        }

        [Test]
        public void ADoorLeftOpen_AfterADoorAndTwoSensors_IsABreakIn()
        {
            var assessment = GuardAssessment.Assess(new SiteEvidence
            {
                Zones = new[] { "Café door", "Café sensor", "Storeroom sensor" },
                DoorThenMotion = true,
                Openings = new[] { Door(true), new PerimeterOpening("Roof window 1", window: true, open: false) },
            });

            Assert.That(assessment.Verdict, Is.EqualTo(GuardVerdict.BreakIn));
            Assert.That(assessment.Score, Is.EqualTo(GuardAssessment.DoorOpenPoints + GuardAssessment.ManyZonesPoints + GuardAssessment.DoorThenMotionPoints));
            Assert.That(assessment.Factors.Select(f => f.Reason), Has.Member("Shop door was open"));
        }

        [Test]
        public void SeeingSomeone_IsABreakIn_WhateverElse()
        {
            var assessment = GuardAssessment.Assess(new SiteEvidence { IntruderSeen = true, CodeEntered = true, Faults = AlarmFaults.Mains });

            Assert.That(assessment.Verdict, Is.EqualTo(GuardVerdict.BreakIn));
        }

        [Test]
        public void AFaultyPanel_OrTheRightCode_MakesAFalseAlarmLikelier()
        {
            var confirmed = new SiteEvidence { Zones = new[] { "Front door", "Hall sensor" }, DoorThenMotion = true, Tamper = false };
            var healthy = GuardAssessment.Assess(confirmed);

            confirmed.Faults = AlarmFaults.Mains | AlarmFaults.Battery;
            var faulty = GuardAssessment.Assess(confirmed);
            Assert.That(faulty.Score, Is.EqualTo(healthy.Score + GuardAssessment.FaultPoints));
            Assert.That(faulty.Factors.Last().Reason, Does.Contain("mains, battery"));

            confirmed.Faults = AlarmFaults.None;
            confirmed.Powered = false;
            Assert.That(GuardAssessment.Assess(confirmed).Score, Is.EqualTo(healthy.Score + GuardAssessment.NoPowerPoints));

            confirmed.Powered = true;
            confirmed.CodeEntered = true;
            Assert.That(GuardAssessment.Assess(confirmed).Score, Is.EqualTo(healthy.Score + GuardAssessment.CodeEnteredPoints));
        }

        [Test]
        public void TamperingAndCutWires_AddUp_ToABreakIn()
        {
            var tampered = GuardAssessment.Assess(new SiteEvidence { Tamper = true, LidOpen = true });
            Assert.That(tampered.Score, Is.EqualTo(GuardAssessment.TamperPoints + GuardAssessment.LidOpenPoints));
            Assert.That(tampered.Verdict, Is.EqualTo(GuardVerdict.FalseAlarm), "probably someone fiddling with it");

            var cut = GuardAssessment.Assess(new SiteEvidence { Tamper = true, LidOpen = true, CutZones = new[] { "Hall sensor", "Kitchen sensor", "Attic sensor" } });
            Assert.That(cut.Score, Is.EqualTo(tampered.Score + (2 * GuardAssessment.CutWirePoints)), "two cut wires count; more don't add more");
            Assert.That(cut.Verdict, Is.EqualTo(GuardVerdict.BreakIn));
        }

        [Test]
        public void TheScore_NeverGoesBelowNought()
        {
            var assessment = GuardAssessment.Assess(new SiteEvidence { CodeEntered = true, Powered = false });

            Assert.That(assessment.Score, Is.EqualTo(0));
            Assert.That(assessment.Summary, Is.EqualTo("suspicion 0: false alarm"));
        }
    }
}
