using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Buildings;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Simulation.Walking;

namespace Townscape.Tests.Generation
{
    public sealed class CopperKettleTests
    {
        private static BuildingPlan Kettle => ShopLocator.Find(GeneratedVillage.Town.Context.Buildings, VillageShops.CopperKettle);

        private static UnitDesign Design => ((TerracedUnitStyle)Kettle.Style).Design;

        /// <summary>The Copper Kettle built on its own, so its geometry isn't mixed with its neighbours'.</summary>
        private static (MeshData Mesh, List<TownAnchor> Anchors, List<TownDoor> Doors) BuildAlone()
        {
            var builder = new MeshBuilder();
            var anchors = new List<TownAnchor>();
            var doors = new List<TownDoor>();
            Kettle.Style.Build(Kettle.Footprint, new BuildContext(builder, anchors, Kettle.Seed, doors));
            return (builder.Build("Copper Kettle"), anchors, doors);
        }

        private static int Triangles(MeshData mesh, Func<SurfaceMaterial, bool> which) =>
            mesh.Submeshes.Where(s => which(s.Material)).Sum(s => s.Indices.Length / 3);

        [Test]
        public void OnlyTheCopperKettle_CanBeEntered()
        {
            var enterable = GeneratedVillage.Town.Context.Buildings.Where(plan => plan.Style is TerracedUnitStyle unit && unit.Enterable).ToList();

            Assert.That(enterable, Has.Count.EqualTo(1));
            Assert.That(enterable[0], Is.SameAs(Kettle));
            Assert.That(CafeAndFlat.Fits(Kettle.Footprint), Is.True);
        }

        [Test]
        public void ItsTwoFrontDoors_Open_IntoTheBuilding()
        {
            var doors = GeneratedVillage.Town.Doors;
            var site = Kettle.Footprint;
            var outward = new Vector3(site.Outward.X, 0f, site.Outward.Y);

            Assert.That(doors.Select(d => d.Name), Is.EquivalentTo(new[] { "Shop door", "Flat door" }));
            foreach (var door in doors)
            {
                Assert.That(site.Contains(new Vector2(door.Hinge.X, door.Hinge.Z)), Is.True, $"{door.Name} hangs in the front wall");
                Assert.That(Vector3.Dot(door.Inward, outward), Is.LessThan(-0.99f), $"{door.Name} opens inwards");
                Assert.That(Math.Abs(Vector3.Dot(door.Along, door.Inward)), Is.LessThan(1e-3f));
                Assert.That(door.Width, Is.InRange(WalkMotion.BodyRadius * 2.5f, 1.1f), "wide enough to walk through");
                Assert.That(door.Height, Is.GreaterThan(WalkMotion.BodyHeight + 0.15f), "tall enough to walk through");

                var open = door.LatchAt(door.OpenDegrees);
                Assert.That(Vector3.Dot(open - door.Hinge, outward), Is.LessThan(-0.8f * door.Width), $"{door.Name} swings well into the room");
                Assert.That(Vector3.Distance(door.LatchAt(0f), door.Hinge + (door.Along * door.Width)), Is.LessThan(1e-4f));
            }

            // Looking at the shop from the street, the café door is on the right, the flat's on the left.
            var along = Vector2.Normalize(site.FrontRight - site.FrontLeft);
            float Across(TownDoor door) => Vector2.Dot(new Vector2(door.Hinge.X, door.Hinge.Z) - site.FrontLeft, along);
            Assert.That(Across(doors.Single(d => d.Name == "Shop door")), Is.GreaterThan(Across(doors.Single(d => d.Name == "Flat door"))));
        }

        [Test]
        public void DoorLeaves_FitTheirDoorways()
        {
            foreach (var door in GeneratedVillage.Town.Doors)
            {
                var leaf = door.ClosedLeaf();
                Assert.That(leaf.Positions, Is.Not.Empty);
                foreach (var p in leaf.Positions)
                {
                    var offset = p - door.Hinge;
                    var across = Vector3.Dot(offset, door.Along);
                    Assert.That(across, Is.InRange(-0.01f, door.Width + 0.01f), door.Name);
                    Assert.That(offset.Y, Is.InRange(-0.01f, door.Height + 0.01f), door.Name);
                }
            }
        }

        [Test]
        public void TheStairs_ClimbFloorToFloor_InEasySteps()
        {
            var floors = CafeAndFlat.Floors(Design);
            var flights = CafeAndFlat.Flights(Design);

            Assert.That(floors, Has.Length.EqualTo(4));
            Assert.That(floors[3], Is.EqualTo(Design.Eaves).Within(1e-3f), "the attic floor is at the eaves");
            Assert.That(flights, Has.Length.EqualTo(3));
            for (var i = 0; i < flights.Length; i++)
            {
                var flight = flights[i];
                Assert.That(flight.FromY, Is.EqualTo(floors[i]).Within(1e-4f));
                Assert.That(flight.ToY, Is.EqualTo(floors[i + 1]).Within(1e-4f));
                Assert.That(flight.Rise, Is.InRange(0.15f, Math.Min(0.21f, WalkMotion.StepHeight)), "a comfortable riser the walker takes in stride");
                Assert.That(flight.Going, Is.GreaterThanOrEqualTo(0.24f), "a tread a foot fits on");
                Assert.That(flight.X1 - flight.X0, Is.GreaterThan(WalkMotion.BodyRadius * 2f + 0.3f), "wide enough to climb");
                Assert.That(flight.HeightAt(flight.StartD - (0.1f * Math.Sign(flight.EndD - flight.StartD))), Is.EqualTo(flight.FromY));
                Assert.That(flight.HeightAt(flight.EndD), Is.EqualTo(flight.ToY).Within(1e-4f));
            }

            // Each flight lands where the next begins: a dog-leg up the left of the building.
            Assert.That(flights[0].EndD, Is.EqualTo(flights[1].StartD).Within(1e-4f));
            Assert.That(flights[1].EndD, Is.EqualTo(flights[2].StartD).Within(1e-4f));
        }

        [Test]
        public void ItsWindows_AreClearGlass_ToSeeInAndOut()
        {
            var (mesh, anchors, _) = BuildAlone();

            // Only the fanlight over the flat's door keeps glowing home-window glass.
            Assert.That(Triangles(mesh, m => NightLights_IsHomeWindow(m)), Is.LessThanOrEqualTo(2));
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.Window), Is.EqualTo(10), "six at the front, three at the back and the dormer");
            Assert.That(Triangles(mesh, m => m == SurfaceMaterial.ShopGlass), Is.GreaterThan(20));
        }

        [Test]
        public void ItsRooms_AreFurnished_AndLitAtNight()
        {
            var (mesh, anchors, _) = BuildAlone();
            var materials = new HashSet<SurfaceMaterial>(mesh.Submeshes.Select(s => s.Material));

            foreach (var thing in new[]
            {
                SurfaceMaterial.TileDark, SurfaceMaterial.Chalkboard, SurfaceMaterial.Chrome, SurfaceMaterial.Sponge, SurfaceMaterial.Icing,
                SurfaceMaterial.Cardboard, SurfaceMaterial.FabricSage, SurfaceMaterial.FabricNavy, SurfaceMaterial.Linen, SurfaceMaterial.Porcelain,
            })
            {
                Assert.That(materials, Has.Member(thing), thing.ToString());
            }

            Assert.That(anchors.Count(a => a.Kind == AnchorKind.RoomLight), Is.EqualTo(9), "hall, store, two rooms and a landing on each upper floor, the attic");
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.ShopWindow), Is.EqualTo(2), "the window and the café ceiling");
            foreach (var light in anchors.Where(a => a.Kind == AnchorKind.RoomLight))
            {
                Assert.That(Kettle.Footprint.Contains(new Vector2(light.Position.X, light.Position.Z)), Is.True);
                Assert.That(light.Position.Y, Is.LessThan(Design.Ridge));
            }
        }

        [Test]
        public void TheRestOfTheVillage_IsBuiltAsBefore()
        {
            // Other shops keep their window displays and glowing windows, and have no doors to open.
            var other = GeneratedVillage.Town.Context.Buildings.First(plan => plan.Style is TerracedUnitStyle unit && unit.Design.Shop != null && !unit.Enterable);
            var builder = new MeshBuilder();
            var doors = new List<TownDoor>();
            other.Style.Build(other.Footprint, new BuildContext(builder, new List<TownAnchor>(), other.Seed, doors));
            var mesh = builder.Build("Other");

            Assert.That(doors, Is.Empty);
            Assert.That(Triangles(mesh, NightLights_IsHomeWindow), Is.GreaterThan(4));
        }

        private static bool NightLights_IsHomeWindow(SurfaceMaterial material) => material >= SurfaceMaterial.Window0 && material <= SurfaceMaterial.Window7;
    }
}
