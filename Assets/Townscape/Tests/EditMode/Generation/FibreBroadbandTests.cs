using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Dressing.Props;
using Townscape.Generation.Layout;
using Townscape.Simulation.Walking;

namespace Townscape.Tests.Generation
{
    public sealed class FibreBroadbandTests
    {
        private static readonly Vector2 PhoneBoxSpot = new Vector2(-12.5f, 9f);

        private static Vector2 Flat(Vector3 v) => new Vector2(v.X, v.Z);

        private static TownDoor Door(string name) => GeneratedVillage.Town.Doors.Single(d => d.Name == name);

        private static TownCabinet Cabinet => GeneratedVillage.Town.Cabinets.Single();

        [Test]
        public void TheCabinet_StandsBesideThePhoneBox_FacingTheSameWay()
        {
            var cabinet = Cabinet;
            var phone = Door(PhoneBox.DoorName);
            var gap = Vector2.Distance(Flat(cabinet.Rack.Position), PhoneBoxSpot);

            Assert.That(cabinet.Name, Is.EqualTo(LakeDistrictVillageLayout.FibreCabinetName));
            Assert.That(gap, Is.InRange(PhoneBox.Half + FibreCabinet.HalfWidth + 0.1f, 2.5f), "next to it, not in it");
            Assert.That(Vector3.Dot(cabinet.Facing, phone.Inward), Is.GreaterThan(0.99f));
        }

        [Test]
        public void ThePhoneBox_HasADoorWideAndTallEnoughToStepThrough_ThatSwingsOut()
        {
            var door = Door(PhoneBox.DoorName);
            var centre = door.Hinge + (door.Along * (door.Width * 0.5f)) - (door.Inward * PhoneBox.Half);

            Assert.That(door.Width, Is.GreaterThan((2f * WalkMotion.BodyRadius) + 0.15f));
            Assert.That(door.Height, Is.GreaterThan(WalkMotion.BodyHeight + 0.2f));
            Assert.That(Vector2.Distance(Flat(centre), PhoneBoxSpot), Is.LessThan(0.01f));
            Assert.That(Vector2.Distance(Flat(door.LatchAt(door.OpenDegrees)), PhoneBoxSpot), Is.GreaterThan(PhoneBox.Half + 0.2f), "it opens outwards");
        }

        [Test]
        public void InsideThePhoneBox_ThereIsRoomToStand()
        {
            // Nothing of the box (the payphone, the shelf, the glazing) comes within a body's
            // width of the middle, between the floor and head height.
            var ground = Door(PhoneBox.DoorName).Hinge.Y - 0.1f;
            var furniture = GeneratedVillage.Town.Meshes.Where(m => m.Category == MeshCategory.Furniture).Select(m => m.Mesh);
            foreach (var mesh in furniture)
            {
                foreach (var p in mesh.Positions)
                {
                    if (p.Y > ground + 0.15f && p.Y < ground + WalkMotion.BodyHeight)
                    {
                        Assert.That(Vector2.Distance(Flat(p), PhoneBoxSpot), Is.GreaterThan(WalkMotion.BodyRadius + 0.02f), $"{p} is in the way");
                    }
                }
            }
        }

        [Test]
        public void TheCabinet_OpensOnItsRack_WithALightForEachPort()
        {
            var cabinet = Cabinet;
            var left = Door("Cabinet left door");
            var right = Door("Cabinet right door");

            Assert.That(left.Width + right.Width, Is.InRange(0.8f, 2f * FibreCabinet.HalfWidth));
            Assert.That(Vector3.Dot(left.Inward, cabinet.Facing), Is.GreaterThan(0.99f), "the doors swing out");
            Assert.That(Vector3.Dot(left.Along, right.Along), Is.LessThan(-0.99f), "and meet in the middle");

            var ports = cabinet.Leds.Where(l => l.Kind == CabinetLedKind.Port).Select(l => l.Port).ToList();
            Assert.That(ports, Has.Member(1));
            Assert.That(ports, Has.Member(2));
            Assert.That(cabinet.Leds.Count(l => l.Kind == CabinetLedKind.Uplink), Is.EqualTo(1));
            Assert.That(cabinet.Leds.Count(l => l.Kind == CabinetLedKind.Wan), Is.EqualTo(1));
            Assert.That(cabinet.Leds.Count(l => l.Kind == CabinetLedKind.Ups), Is.EqualTo(1));

            // Everything is inside the cabinet, behind its doors.
            var front = Vector3.Dot(left.Hinge - cabinet.Rack.Position, cabinet.Facing);
            Assert.That(front, Is.GreaterThan(0.05f));
            foreach (var led in cabinet.Leds)
            {
                Assert.That(Vector2.Distance(Flat(led.Position), Flat(cabinet.Rack.Position)), Is.LessThan(FibreCabinet.HalfWidth));
                Assert.That(Vector3.Dot(left.Hinge - led.Position, cabinet.Facing), Is.GreaterThan(0.05f));
            }

            Assert.That(Vector2.Distance(Flat(cabinet.Screen.Position), Flat(cabinet.Rack.Position)), Is.LessThan(FibreCabinet.HalfWidth));
        }

        [Test]
        public void EachAlarmedBuilding_HasItsFibreBoxAndRouterOnAWallInside()
        {
            var town = GeneratedVillage.Town;
            var kettle = ShopLocator.Find(town.Context.Buildings, VillageShops.CopperKettle);
            Assert.That(town.Broadband.Select(b => b.Customer), Is.EquivalentTo(town.Alarms.Select(a => a.Name)));

            foreach (var broadband in town.Broadband)
            {
                Assert.That(broadband.OntLeds, Has.Count.EqualTo(4));
                Assert.That(broadband.RouterLeds, Has.Count.EqualTo(3));
                foreach (var mount in new[] { broadband.Ont, broadband.Router })
                {
                    Assert.That(kettle.Footprint.Contains(Flat(mount.Position)), Is.True, $"{broadband.Customer}'s kit is indoors");
                }

                // Side by side, not overlapping, and within reach of each other's cable.
                var apart = Vector3.Distance(broadband.Ont.Position, broadband.Router.Position);
                Assert.That(apart, Is.GreaterThan((BroadbandFittings.OntWidth + BroadbandFittings.RouterWidth) * 0.5f));
                Assert.That(apart, Is.LessThan(0.6f));
                Assert.That(Vector3.Dot(broadband.Ont.Facing, broadband.Router.Facing), Is.GreaterThan(0.99f));

                // Each light is on the front of its box.
                foreach (var led in broadband.OntLeds)
                {
                    Assert.That(Vector3.Dot(led - broadband.Ont.Position, broadband.Ont.Facing), Is.InRange(0f, 0.01f));
                }

                foreach (var led in broadband.RouterLeds)
                {
                    Assert.That(Vector3.Dot(led - broadband.Router.Position, broadband.Router.Facing), Is.InRange(0f, 0.01f));
                }
            }

            // The café's router sits by its alarm's control box, which reports through it.
            var shop = town.Broadband.Single(b => b.Customer == CafeAndFlat.ShopAlarm);
            var box = town.Alarms.Single(a => a.Name == CafeAndFlat.ShopAlarm).ControlBox;
            Assert.That(Vector3.Distance(shop.Router.Position, box.Position), Is.LessThan(1f));
        }
    }
}
