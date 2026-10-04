using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Simulation.Walking;
using Townscape.Tests.Generation;

namespace Townscape.Tests.Simulation
{
    public sealed class WalkMotionTests
    {
        private static Vector2 Walk(Vector2 direction, bool running, float seconds, Vector2 start = default)
        {
            var velocity = start;
            for (var t = 0f; t < seconds; t += 0.02f)
            {
                velocity = WalkMotion.Steer(velocity, direction, running, grounded: true, 0.02f);
            }

            return velocity;
        }

        [Test]
        public void Walkers_ReachWalkingPace_Quickly_AndJogFaster()
        {
            Assert.That(Walk(Vector2.UnitX, false, 0.5f).Length(), Is.EqualTo(WalkMotion.WalkSpeed).Within(0.05f));
            Assert.That(Walk(Vector2.UnitX, true, 0.5f).Length(), Is.EqualTo(WalkMotion.RunSpeed).Within(0.1f));
        }

        [Test]
        public void Diagonals_AreNoFaster_AndLettingGoStops()
        {
            Assert.That(Walk(new Vector2(1f, 1f), false, 1f).Length(), Is.EqualTo(WalkMotion.WalkSpeed).Within(0.05f));
            Assert.That(Walk(Vector2.Zero, false, 0.5f, new Vector2(WalkMotion.RunSpeed, 0f)).Length(), Is.LessThan(0.05f));
        }

        [Test]
        public void InTheAir_SteeringBarelyChangesCourse()
        {
            var velocity = WalkMotion.Steer(new Vector2(WalkMotion.WalkSpeed, 0f), -Vector2.UnitX, false, grounded: false, 0.1f);

            Assert.That(velocity.X, Is.GreaterThan(WalkMotion.WalkSpeed * 0.6f));
        }

        [Test]
        public void AJump_ClearsAKerb_ButNotAWall()
        {
            Assert.That(WalkMotion.JumpHeight, Is.InRange(0.4f, 0.75f));

            var height = 0f;
            var speed = WalkMotion.Fall(0f, grounded: true, jump: true, 0.01f);
            var highest = 0f;
            for (var t = 0f; t < 2f; t += 0.01f)
            {
                height += speed * 0.01f;
                highest = System.Math.Max(highest, height);
                speed = WalkMotion.Fall(speed, grounded: false, jump: false, 0.01f);
            }

            Assert.That(highest, Is.EqualTo(WalkMotion.JumpHeight).Within(0.05f));
        }

        [Test]
        public void OnTheGround_WalkersAreHeldDown_SoTheyStepDownKerbs()
        {
            Assert.That(WalkMotion.Fall(0f, grounded: true, jump: false, 0.02f), Is.LessThan(0f));
            Assert.That(WalkMotion.Fall(-50f, grounded: false, jump: false, 1f), Is.GreaterThanOrEqualTo(-30f), "falling tops out");
        }

        [Test]
        public void Walkers_FitThroughDoors_AndSeeOverWalls()
        {
            Assert.That(WalkMotion.BodyRadius * 2f, Is.LessThan(0.8f), "a doorway is about 0.85 m wide");
            Assert.That(WalkMotion.BodyHeight, Is.LessThan(2.1f), "a door is about 2.1 m high");
            Assert.That(WalkMotion.EyeHeight, Is.InRange(1.5f, WalkMotion.BodyHeight));
            Assert.That(WalkMotion.StepHeight, Is.GreaterThan(0.2f), "up stairs without jumping");
        }
    }

    public sealed class WalkableTownTests
    {
        [Test]
        public void TheGroundBuildingsAndStreetFurniture_AreSolid_WaterAndPlantsAreNot()
        {
            foreach (var solid in new[] { MeshCategory.Ground, MeshCategory.Fells, MeshCategory.Structure, MeshCategory.Building, MeshCategory.Furniture })
            {
                Assert.That(MeshCategories.IsSolid(solid), Is.True, solid.ToString());
            }

            foreach (var soft in new[] { MeshCategory.Water, MeshCategory.Markings, MeshCategory.Vegetation, MeshCategory.Puddles, MeshCategory.Fittings })
            {
                Assert.That(MeshCategories.IsSolid(soft), Is.False, soft.ToString());
            }
        }

        [Test]
        public void EveryTree_HasATrunkToBumpInto()
        {
            var town = GeneratedVillage.Town;
            var trunks = town.Anchors.Where(a => a.Kind == AnchorKind.TreeTrunk).ToList();

            Assert.That(trunks.Count, Is.GreaterThan(100));
            Assert.That(trunks.Select(t => t.Size), Has.All.InRange(0.1f, 0.6f));
            foreach (var trunk in trunks.Where(t => System.Math.Abs(t.Position.X) < 90f && System.Math.Abs(t.Position.Z) < 90f))
            {
                var ground = town.Context.Ground.HeightAt(new Vector2(trunk.Position.X, trunk.Position.Z));
                Assert.That(trunk.Position.Y, Is.EqualTo(ground).Within(0.6f), "at the foot of the tree");
            }
        }
    }
}
