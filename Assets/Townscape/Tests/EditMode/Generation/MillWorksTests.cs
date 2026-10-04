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
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Geometry;
using Townscape.Simulation.Security;
using Townscape.Simulation.Walking;

namespace Townscape.Tests.Generation
{
    public sealed class MillWorksTests
    {
        private static BuildingPlan Mill => GeneratedVillage.Town.Context.Buildings.Single(b => b.Style is MillWorksStyle);

        private static Vector2 Flat(Vector3 v) => new Vector2(v.X, v.Z);

        /// <summary>Mill Works built on its own, so its geometry isn't mixed with anything else's.</summary>
        private static (MeshData Mesh, List<TownAnchor> Anchors, List<TownDoor> Doors) BuildAlone()
        {
            var builder = new MeshBuilder();
            var anchors = new List<TownAnchor>();
            var doors = new List<TownDoor>();
            Mill.Style.Build(Mill.Footprint, new BuildContext(builder, anchors, Mill.Seed, doors));
            return (builder.Build("Mill Works"), anchors, doors);
        }

        [Test]
        public void TheOldMill_IsNowMillWorks_WithADoorInAndADoorToTheAlarmReceivingCentre()
        {
            Assert.That(Mill.Group, Is.EqualTo(MillWorks.Name));
            Assert.That(MillWorks.Fits(Mill.Footprint), Is.True);
            var doors = GeneratedVillage.Town.Doors.Select(d => d.Name).ToList();
            Assert.That(doors, Has.Member(MillWorks.DoorName));
            Assert.That(doors, Has.Member(MillWorks.CentreDoorName));

            var front = GeneratedVillage.Town.Doors.Single(d => d.Name == MillWorks.DoorName);
            Assert.That(front.Width, Is.GreaterThan((2f * WalkMotion.BodyRadius) + 0.2f));
            Assert.That(front.Height, Is.GreaterThan(WalkMotion.BodyHeight + 0.2f));
            Assert.That(Vector2.Dot(Flat(front.Inward), -Mill.Footprint.Outward), Is.GreaterThan(0.99f), "it opens into the building");
        }

        [Test]
        public void TheStairs_ClimbFloorToFloor_InStepsYouCanWalkUp()
        {
            var space = new UnitSpace(Mill.Footprint);
            var f = MillWorks.Floors();
            var flights = MillWorks.Flights(space);

            Assert.That(flights[0].FromY, Is.EqualTo(f[0]));
            Assert.That(flights[0].ToY, Is.EqualTo(f[1]));
            Assert.That(flights[1].FromY, Is.EqualTo(f[1]));
            Assert.That(flights[1].ToY, Is.EqualTo(f[2]));
            foreach (var flight in flights)
            {
                Assert.That(flight.Rise, Is.LessThan(WalkMotion.StepHeight * 0.5f));
                Assert.That(flight.Going, Is.GreaterThan(0.24f));
                Assert.That(flight.X1 - flight.X0, Is.GreaterThan((2f * WalkMotion.BodyRadius) + 0.3f));
            }

            Assert.That(flights[0].FarD, Is.EqualTo(flights[1].FarD), "the second flight starts where the first one finishes");
        }

        [Test]
        public void Doorways_AndTheWaysThrough_AreKeptClearOfFurniture()
        {
            var (mesh, _, _) = BuildAlone();
            var space = new UnitSpace(Mill.Footprint);
            var architecture = new[] { SurfaceMaterial.Interior, SurfaceMaterial.InteriorFloor, SurfaceMaterial.Slate, SurfaceMaterial.Stone };
            var walkways = MillWorks.Walkways(Mill.Footprint).ToList();

            Assert.That(walkways, Has.Count.EqualTo(9));
            foreach (var submesh in mesh.Submeshes.Where(s => Array.IndexOf(architecture, s.Material) < 0))
            {
                for (var i = 0; i < submesh.Indices.Length; i += 3)
                {
                    var corners = new[] { submesh.Indices[i], submesh.Indices[i + 1], submesh.Indices[i + 2] }.Select(index => space.Local(mesh.Positions[index])).ToArray();
                    var low = Vector3.Min(Vector3.Min(corners[0], corners[1]), corners[2]);
                    var high = Vector3.Max(Vector3.Max(corners[0], corners[1]), corners[2]);
                    foreach (var way in walkways)
                    {
                        var blocks = high.X > way.X0 + 0.01f && low.X < way.X1 - 0.01f && high.Z > way.D0 + 0.01f && low.Z < way.D1 - 0.01f &&
                            high.Y > way.Floor + 0.05f && low.Y < way.Floor + WalkMotion.BodyHeight;
                        Assert.That(blocks, Is.False, $"{submesh.Material} blocks the way {way.Name}");
                    }
                }
            }
        }

        [Test]
        public void TheDoorways_AndEveryWindow_AreOpenRightThrough()
        {
            // Nothing of the walls (stone outside, plaster inside) may cross a door, a doorway or
            // a window: each is checked with lines straight through it.
            var (mesh, _, _) = BuildAlone();
            var space = new UnitSpace(Mill.Footprint);
            var walls = new List<(Vector3 A, Vector3 B, Vector3 C)>();
            foreach (var submesh in mesh.Submeshes.Where(s => s.Material == SurfaceMaterial.Interior || s.Material == SurfaceMaterial.Stone))
            {
                for (var i = 0; i < submesh.Indices.Length; i += 3)
                {
                    walls.Add((space.Local(mesh.Positions[submesh.Indices[i]]), space.Local(mesh.Positions[submesh.Indices[i + 1]]), space.Local(mesh.Positions[submesh.Indices[i + 2]])));
                }
            }

            void Open(string what, Vector3 from, Vector3 to) =>
                Assert.That(walls.Any(t => Crosses(from, to, t.A, t.B, t.C)), Is.False, $"a wall crosses {what} at {from}");

            var door = MillWorks.Door(space);
            foreach (var up in new[] { 0.4f, 1.2f, 2.0f })
            {
                var x = (door.From + door.To) * 0.5f;
                Open("the front door", new Vector3(x, door.Bottom + up, -0.5f), new Vector3(x, door.Bottom + up, 1.0f));
            }

            var core = MillWorks.CoreX(space);
            foreach (var doorway in MillWorks.CoreDoorways())
            {
                var d = (doorway.From + doorway.To) * 0.5f;
                foreach (var up in new[] { 0.4f, 1.2f, 1.9f })
                {
                    Open("a doorway into the stair core", new Vector3(core - 0.5f, doorway.Bottom + up, d), new Vector3(core + 0.6f, doorway.Bottom + up, d));
                }
            }

            Vector3 Middle(Hole h, float x, float d, bool along) =>
                along ? new Vector3(x, (h.Bottom + h.Top) * 0.5f, (h.From + h.To) * 0.5f) : new Vector3((h.From + h.To) * 0.5f, (h.Bottom + h.Top) * 0.5f, d);
            foreach (var window in MillWorks.FrontWindows(space))
            {
                var m = Middle(window, 0f, 0f, along: false);
                Open("a front window", m - new Vector3(0f, 0f, 0.5f), m + new Vector3(0f, 0f, 0.5f));
            }

            foreach (var window in MillWorks.BackWindows(space))
            {
                var m = Middle(window, 0f, space.Depth, along: false);
                Open("a back window", m - new Vector3(0f, 0f, 0.5f), m + new Vector3(0f, 0f, 0.5f));
            }

            foreach (var window in MillWorks.LeftWindows(space))
            {
                var m = Middle(window, 0f, 0f, along: true);
                Open("a left window", m - new Vector3(0.5f, 0f, 0f), m + new Vector3(0.5f, 0f, 0f));
            }

            foreach (var window in MillWorks.RightWindows(space))
            {
                var m = Middle(window, space.Width, 0f, along: true);
                Open("a right window", m - new Vector3(0.5f, 0f, 0f), m + new Vector3(0.5f, 0f, 0f));
            }
        }

        // Whether the segment from p to q passes through the triangle (a, b, c).
        private static bool Crosses(Vector3 p, Vector3 q, Vector3 a, Vector3 b, Vector3 c)
        {
            var direction = q - p;
            var e1 = b - a;
            var e2 = c - a;
            var h = Vector3.Cross(direction, e2);
            var det = Vector3.Dot(e1, h);
            if (Math.Abs(det) < 1e-9f)
            {
                return false;
            }

            var s = p - a;
            var u = Vector3.Dot(s, h) / det;
            var k = Vector3.Cross(s, e1);
            var v = Vector3.Dot(direction, k) / det;
            var t = Vector3.Dot(e2, k) / det;
            return u >= 0f && v >= 0f && u + v <= 1f && t >= 0f && t <= 1f;
        }

        [Test]
        public void ItHasItsOwnAlarm_WithAKeypadInsideTheDoor_AndAYellowBellBox()
        {
            var alarm = GeneratedVillage.Town.Alarms.Single(a => a.Name == MillWorks.Name);
            var door = GeneratedVillage.Town.Doors.Single(d => d.Name == MillWorks.DoorName);

            Assert.That(alarm.Zones.Where(z => z.Kind == AlarmZoneKind.Motion).Select(z => z.Name),
                Is.EqualTo(new[] { "Office sensor", "Stairs sensor", "ARC sensor", "Meeting room sensor" }), "one sensor in each room");

            // The office's one sensor sees right across the ground floor: the door, reception and the desks.
            var space = new UnitSpace(Mill.Footprint);
            var office = alarm.Zones.Single(z => z.Name == "Office sensor");
            var floor = MillWorks.Floors()[0];
            foreach (var (x, d) in new[] { (6f, 0.6f), (7.75f, 2.75f), (1.85f, 3.4f), (3.75f, 4.6f), (5f, 6.6f) })
            {
                var chest = space.At(x, floor + 1.2f, d);
                Assert.That(MotionSensor.Covers(office.Position, MotionSensor.Facing(office.Facing), chest), Is.True, $"sees ({x}, {d})");
            }

            Assert.That(alarm.Zones.Single(z => z.Kind == AlarmZoneKind.Door).Door, Is.EqualTo(MillWorks.DoorName));

            var keypad = alarm.Keypads.Single().Panel;
            Assert.That(Mill.Footprint.Contains(Flat(keypad.Position)), Is.True, "on the inside");
            Assert.That(Vector2.Distance(Flat(keypad.Position), Flat(door.Hinge)), Is.LessThan(1.2f), "right by the door");
            Assert.That(Vector2.Dot(Flat(keypad.Facing), -Mill.Footprint.Outward), Is.GreaterThan(0.99f), "facing into the room");
            Assert.That(Mill.Footprint.Contains(Flat(alarm.ControlBox.Position)), Is.True);
            Assert.That(Mill.Footprint.Contains(Flat(alarm.BellStrobe.Position)), Is.False, "the bell box is outside");
            Assert.That(alarm.BellStrobe.Position.Y, Is.GreaterThan(MillWorks.Floors()[2]));
        }

        [Test]
        public void ItsFibre_ComesIntoTheStairCore_ByTheAlarmsControlBox()
        {
            var broadband = GeneratedVillage.Town.Broadband.Single(b => b.Customer == MillWorks.Name);
            var alarm = GeneratedVillage.Town.Alarms.Single(a => a.Name == MillWorks.Name);

            Assert.That(Mill.Footprint.Contains(Flat(broadband.Ont.Position)), Is.True);
            Assert.That(Vector3.Distance(broadband.Router.Position, alarm.ControlBox.Position), Is.LessThan(1.2f));
            Assert.That(GeneratedVillage.Town.Broadband, Has.Count.EqualTo(3), "the café, the flat and Mill Works");
        }

        [Test]
        public void TheAlarmReceivingCentre_HasConsolesFacingAWallOfScreens_OnTheFirstFloor()
        {
            var centre = GeneratedVillage.Town.AlarmCentres.Single();
            var f = MillWorks.Floors();

            Assert.That(centre.Name, Is.EqualTo(MillWorks.CentreName));
            Assert.That(centre.Consoles.Count, Is.GreaterThanOrEqualTo(6));
            Assert.That(centre.VideoWall.Count, Is.GreaterThanOrEqualTo(GeneratedVillage.Town.Alarms.Count), "a screen for every site");
            foreach (var console in centre.Consoles)
            {
                Assert.That(Mill.Footprint.Contains(Flat(console.Position)), Is.True);
                Assert.That(console.Position.Y - f[1], Is.InRange(0.9f, 1.4f), "on a desk, at sitting eye height");
            }

            // The operators face the screens: each console looks away from the wall the screens are on.
            var wall = centre.VideoWall[0];
            foreach (var console in centre.Consoles)
            {
                Assert.That(Vector3.Dot(console.Facing, wall.Facing), Is.GreaterThan(0.99f));
                Assert.That(Vector3.Dot(console.Position - wall.Position, wall.Facing), Is.GreaterThan(2f));
            }

            var (_, anchors, _) = BuildAlone();
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.OfficeLight), Is.GreaterThanOrEqualTo(4), "lit all night");
        }
    }
}
