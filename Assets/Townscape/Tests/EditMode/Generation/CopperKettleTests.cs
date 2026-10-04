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
            var doors = GeneratedVillage.Town.Doors.Where(d => d.Noun == "door").ToList();
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
                // A door hangs on one edge; a roof window pivots about its middle.
                var leaf = door.ClosedLeaf();
                var from = door.Noun == "window" ? -door.Width : 0f;
                Assert.That(leaf.Positions, Is.Not.Empty);
                foreach (var p in leaf.Positions)
                {
                    var offset = p - door.Hinge;
                    Assert.That(Vector3.Dot(offset, door.Along), Is.InRange(from - 0.01f, door.Width + 0.01f), door.Name);
                    Assert.That(Vector3.Dot(offset, door.Axis), Is.InRange(-0.01f, door.Height + 0.01f), door.Name);
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
            Assert.That(Triangles(mesh, m => m == SurfaceMaterial.ClearGlass), Is.GreaterThan(40), "glass seen from both sides");
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
        public void Doorways_AndTheWaysIn_AreKeptClearOfFurniture()
        {
            var (mesh, _, _) = BuildAlone();
            var space = new UnitSpace(Kettle.Footprint);
            var architecture = new[] { SurfaceMaterial.Interior, SurfaceMaterial.InteriorFloor, SurfaceMaterial.TileLight, SurfaceMaterial.TileDark, SurfaceMaterial.Slate, Design.Wall };
            var walkways = CafeAndFlat.Walkways(Kettle.Footprint, Design).ToList();

            Assert.That(walkways, Has.Count.EqualTo(8));
            foreach (var submesh in mesh.Submeshes.Where(s => Array.IndexOf(architecture, s.Material) < 0))
            {
                for (var i = 0; i < submesh.Indices.Length; i += 3)
                {
                    var corners = new[] { submesh.Indices[i], submesh.Indices[i + 1], submesh.Indices[i + 2] }.Select(index => space.Local(mesh.Positions[index])).ToArray();
                    var low = Vector3.Min(Vector3.Min(corners[0], corners[1]), corners[2]);
                    var high = Vector3.Max(Vector3.Max(corners[0], corners[1]), corners[2]);
                    foreach (var way in walkways)
                    {
                        // Anything between ankle and head height that reaches into the walkway is in the way.
                        var blocks = high.X > way.X0 + 0.01f && low.X < way.X1 - 0.01f && high.Z > way.D0 + 0.01f && low.Z < way.D1 - 0.01f &&
                            high.Y > way.Floor + 0.05f && low.Y < way.Floor + WalkMotion.BodyHeight;
                        Assert.That(blocks, Is.False, $"{submesh.Material} blocks the way {way.Name}");
                    }
                }
            }
        }

        [Test]
        public void TheAttic_HasTwoRoofWindows_ThatPivotOpen()
        {
            var windows = GeneratedVillage.Town.Doors.Where(d => d.Noun == "window").ToList();
            var openings = CafeAndFlat.RoofWindows(Kettle.Footprint, Design);
            var attic = CafeAndFlat.Floors(Design)[3];

            Assert.That(windows.Select(w => w.Name), Is.EquivalentTo(new[] { "Roof window 1", "Roof window 2" }));
            Assert.That(openings, Has.Length.EqualTo(2));
            foreach (var opening in openings)
            {
                Assert.That(opening.B0, Is.GreaterThan(0.5f), "in the back slope");
                Assert.That(opening.B1, Is.LessThan(1f));
            }

            foreach (var window in windows)
            {
                Assert.That(Math.Abs(window.Axis.Y), Is.LessThan(1e-3f), "pivots about a level line");
                Assert.That(window.Along.Y, Is.GreaterThan(0.5f), "the top half tips in");
                Assert.That(window.Hinge.Y - attic, Is.InRange(1.2f, 1.9f), "within reach from the attic floor");

                var shut = window.LatchAt(0f);
                var open = window.LatchAt(window.OpenDegrees);
                Assert.That(open.Y, Is.LessThan(shut.Y - 0.2f), "its top edge comes down into the room");
                Assert.That(Kettle.Footprint.Contains(new Vector2(window.Hinge.X, window.Hinge.Z)), Is.True);
            }
        }

        [Test]
        public void HangingLights_AreFittings_ThatNobodyBumpsInto()
        {
            var fittings = GeneratedVillage.Town.Meshes.Where(m => m.Category == MeshCategory.Fittings).ToList();
            var kettle = Kettle.Footprint;

            Assert.That(MeshCategories.IsSolid(MeshCategory.Fittings), Is.False);
            Assert.That(fittings.SelectMany(m => m.Mesh.Submeshes).Select(s => s.Material), Has.Member(SurfaceMaterial.LampGlass));
            Assert.That(fittings.Any(m => m.Mesh.Positions.Any(p => kettle.Contains(new Vector2(p.X, p.Z)))), Is.True);

            // Built alone, without a fittings mesh, the lights fall back into the building's own.
            var (mesh, _, _) = BuildAlone();
            Assert.That(mesh.Submeshes.Select(s => s.Material), Has.Member(SurfaceMaterial.LampGlass));
        }

        [Test]
        public void TheBackWindows_LineUpInsideAndOut()
        {
            // The Copper Kettle is a little wedge-shaped, its back wider than its front: the glass
            // must still sit straight behind each hole in the inside of the back wall.
            var (mesh, _, _) = BuildAlone();
            var space = new UnitSpace(Kettle.Footprint);
            var floors = CafeAndFlat.Floors(Design);
            var glass = mesh.Submeshes.Where(s => s.Material == SurfaceMaterial.ClearGlass).SelectMany(s => s.Indices).Select(i => space.Local(mesh.Positions[i]))
                .Where(p => p.Z > space.Depth - 0.25f).ToList();

            for (var i = 0; i < floors.Length - 1; i++)
            {
                var hole = CafeAndFlat.BackWindow(space, floors[i], ground: i == 0);
                var pane = glass.Where(p => p.Y > hole.Bottom - 0.01f && p.Y < hole.Top + 0.01f).ToList();
                Assert.That(pane, Is.Not.Empty, $"floor {i}");
                Assert.That(pane.Min(p => p.X), Is.EqualTo(hole.From).Within(0.01f), $"floor {i}");
                Assert.That(pane.Max(p => p.X), Is.EqualTo(hole.To).Within(0.01f), $"floor {i}");
            }
        }

        [Test]
        public void TheAlarm_HasASensorInEveryRoom_HighUpAndLookingIn()
        {
            var (_, anchors, _) = BuildAlone();
            var space = new UnitSpace(Kettle.Footprint);
            var floors = CafeAndFlat.Floors(Design);
            var sensors = anchors.Where(a => a.Kind == AnchorKind.AlarmSensor).Select(a => (Local: space.Local(a.Position), a.Facing)).ToList();

            // The café, its storeroom and the hall; the living room and kitchen; the bedroom and bathroom; the attic.
            var perFloor = new[] { 3, 2, 2, 1 };
            for (var i = 0; i < floors.Length; i++)
            {
                var top = i + 1 < floors.Length ? floors[i + 1] : Design.Ridge;
                Assert.That(sensors.Count(s => s.Local.Y > floors[i] && s.Local.Y < top), Is.EqualTo(perFloor[i]), $"floor {i}");
            }

            foreach (var sensor in sensors)
            {
                var floor = floors.Last(f => f < sensor.Local.Y);
                Assert.That(sensor.Local.Y - floor, Is.GreaterThan(WalkMotion.BodyHeight + 0.3f), "out of reach, over your head");
                var world = space.At(sensor.Local.X, 0f, sensor.Local.Z);
                Assert.That(Kettle.Footprint.Contains(new Vector2(world.X, world.Z)), Is.True);
                Assert.That(Math.Abs(sensor.Facing.Y), Is.LessThan(1e-3f), "faces level; the alarm tilts it down");

                // Looking into the room, away from the walls it's on.
                var ahead = space.Local(space.At(sensor.Local.X, sensor.Local.Y, sensor.Local.Z) + sensor.Facing);
                Assert.That(ahead.X, Is.InRange(0.2f, space.Width - 0.2f));
                Assert.That(ahead.Z, Is.InRange(0.2f, space.Depth - 0.2f));
            }
        }

        [Test]
        public void TheAlarmHasAKeypad_InsideEachFrontDoor_AtHandHeight()
        {
            var (_, anchors, doors) = BuildAlone();
            var keypads = anchors.Where(a => a.Kind == AnchorKind.AlarmKeypad).ToList();
            var site = Kettle.Footprint;
            var outward = new Vector3(site.Outward.X, 0f, site.Outward.Y);

            Assert.That(keypads, Has.Count.EqualTo(2));
            foreach (var name in new[] { "Flat door", "Shop door" })
            {
                var door = doors.Single(d => d.Name == name);
                float Apart(TownAnchor k) => new Vector2(k.Position.X - door.Hinge.X, k.Position.Z - door.Hinge.Z).Length();
                var keypad = keypads.OrderBy(Apart).First();

                Assert.That(Apart(keypad), Is.LessThan(1.4f), $"by the {name}");
                Assert.That(Vector3.Dot(keypad.Position - site.FrontWall.Origin, outward), Is.LessThan(-0.15f), $"inside the {name}");
                Assert.That(keypad.Position.Y - door.Hinge.Y, Is.InRange(1.2f, 1.7f));
                Assert.That(keypad.Size, Is.InRange(0.1f, 0.2f), "small");

                // Clear of the door as it swings open.
                for (var a = 0f; a <= door.OpenDegrees; a += 5f)
                {
                    var edge = door.LatchAt(a);
                    var toKeypad = new Vector2(keypad.Position.X - door.Hinge.X, keypad.Position.Z - door.Hinge.Z);
                    var toEdge = new Vector2(edge.X - door.Hinge.X, edge.Z - door.Hinge.Z);
                    var along = Math.Clamp(Vector2.Dot(toKeypad, toEdge) / toEdge.LengthSquared(), 0f, 1f);
                    Assert.That(Vector2.Distance(toKeypad, toEdge * along), Is.GreaterThan(0.05f), $"{name} at {a}°");
                }
            }
        }

        [Test]
        public void TheBellBox_IsHighOnTheFront_BetweenTwoSecondFloorWindows()
        {
            var (mesh, anchors, _) = BuildAlone();
            var bell = anchors.Single(a => a.Kind == AnchorKind.AlarmBell);
            var front = Kettle.Footprint.FrontWall;
            var floors = CafeAndFlat.Floors(Design);
            var across = Vector3.Dot(bell.Position - front.Origin, front.Right);

            Assert.That(Vector3.Dot(bell.Facing, front.Out), Is.GreaterThan(0.99f), "on the street side");
            Assert.That(Vector3.Dot(bell.Position - front.Origin, front.Out), Is.InRange(0.05f, 0.15f), "on the face of the wall");
            Assert.That(bell.Position.Y, Is.InRange(floors[2] + 1.4f, floors[3]), "up on the second floor");
            Assert.That(bell.Size, Is.InRange(0.25f, 0.35f), "a real bell box's size");

            // Between the first two windows' columns, clear of both.
            var columnWidth = front.Width / 3f;
            Assert.That(across, Is.InRange(columnWidth * 0.5f + 0.5f + AlarmFittings.BellBoxWidth * 0.5f, columnWidth * 1.5f - 0.5f - AlarmFittings.BellBoxWidth * 0.5f));

            // White, with a blue strobe at its foot.
            var strobe = mesh.Submeshes.Single(s => s.Material == SurfaceMaterial.AlarmStrobe).Indices.Select(i => mesh.Positions[i]).ToList();
            Assert.That(strobe.Max(p => p.Y), Is.LessThan(bell.Position.Y + 0.05f));
            Assert.That(strobe.Min(p => p.Y), Is.GreaterThan(bell.Position.Y - 0.05f));
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
