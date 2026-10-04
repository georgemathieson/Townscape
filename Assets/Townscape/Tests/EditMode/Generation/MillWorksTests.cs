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
        public void ItHasItsOwnAlarm_WithAKeypadInsideTheDoor_AndAYellowBellBox()
        {
            var alarm = GeneratedVillage.Town.Alarms.Single(a => a.Name == MillWorks.Name);
            var door = GeneratedVillage.Town.Doors.Single(d => d.Name == MillWorks.DoorName);

            Assert.That(alarm.Zones.Count(z => z.Kind == AlarmZoneKind.Motion), Is.EqualTo(5));
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
