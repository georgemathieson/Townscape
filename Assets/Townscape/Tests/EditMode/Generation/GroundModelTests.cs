using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Ground;

namespace Townscape.Tests.Generation
{
    public sealed class GroundModelTests
    {
        private static GroundModel Ground => TownGenerator.CreateContext(GeneratedVillage.Layout, GenerationSettings.Default).Ground;

        [TestCase(-50f, -1f, RegionKind.Road)]
        [TestCase(-50f, 3.5f, RegionKind.Pavement)]
        [TestCase(0f, 20f, RegionKind.RiverBed)]
        [TestCase(0f, 0f, RegionKind.RiverBed)]
        [TestCase(10f, 0f, RegionKind.Hole)]
        [TestCase(-60f, -40f, RegionKind.OpenGround)]
        [TestCase(10f, 30f, RegionKind.Path)]
        [TestCase(8f, -100f, RegionKind.RiverBank)]
        public void Classify_KnownSpots(float x, float z, RegionKind expected)
        {
            Assert.That(Ground.Classify(new Vector2(x, z)).Kind, Is.EqualTo(expected));
        }

        [Test]
        public void LaneJunction_RoadBeatsPavement()
        {
            // Where Church Lane leaves the high street, the lane's carriageway cuts through the pavement.
            Assert.That(Ground.Classify(new Vector2(50.5f, 6f)).Kind, Is.EqualTo(RegionKind.Road));
        }

        [Test]
        public void Heights_RoadBelowPavementAboveRiver()
        {
            var road = Ground.HeightAt(new Vector2(-50f, -1f));
            var pavement = Ground.HeightAt(new Vector2(-50f, 3.5f));
            var riverBed = Ground.HeightAt(new Vector2(0f, 20f));

            Assert.That(road, Is.EqualTo(RoadFeature.RoadHeight).Within(1e-4f));
            Assert.That(pavement, Is.EqualTo(RoadFeature.PavementHeight).Within(1e-4f));
            Assert.That(riverBed, Is.LessThan(GeneratedVillage.Layout.WaterLevel - 0.4f));
        }

        [Test]
        public void CoreBorder_IsFlatGrassAwayFromTheRiver()
        {
            var ground = Ground;
            var settings = GenerationSettings.Default;
            for (var x = -90f; x <= 90f; x += 15f)
            {
                var p = new Vector2(x, -settings.CoreHalfExtent);
                if (ground.Classify(p).Kind != RegionKind.OpenGround)
                {
                    continue;
                }

                Assert.That(ground.HeightAt(p), Is.EqualTo(GeneratedVillage.Layout.GrassLevel).Within(1e-3f), $"at x={x}");
            }
        }
    }
}
