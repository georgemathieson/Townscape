using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Routes;
using Townscape.Simulation.Walking;

namespace Townscape.Tests.Generation
{
    public sealed class RouteNetworkTests
    {
        private static RouteNetwork Walking => GeneratedVillage.Town.Walking;

        private static RouteNetwork Driving => GeneratedVillage.Town.Driving;

        private static IReadOnlyList<TownSite> Sites => GeneratedVillage.Town.Sites;

        private static string GuardPost => GeneratedVillage.Town.AlarmCentres.Single().GuardPost;

        private static IEnumerable<string> Doors(RoutePath path) => path.Stops.Where(s => s.Door != null).Select(s => s.Door);

        [Test]
        public void EveryAlarmedBuilding_IsASite_WithItsPlacesOnTheMap()
        {
            var town = GeneratedVillage.Town;
            Assert.That(Sites.Select(s => s.Name), Is.EquivalentTo(town.Alarms.Select(a => a.Name)));
            foreach (var site in Sites)
            {
                foreach (var place in new[] { site.Outside, site.Keypad }.Concat(site.Rooms).Concat(site.Loot))
                {
                    Assert.That(Walking.Find(place), Is.GreaterThanOrEqualTo(0), place);
                }

                Assert.That(Walking.Indoors(Walking.Find(site.Outside)), Is.False, $"{site.Name}'s outside is outside");
                Assert.That(Walking.Indoors(Walking.Find(site.Keypad)), Is.True);

                // The keypad stop is within arm's reach of the keypad.
                var keypad = town.Alarms.Single(a => a.Name == site.Name).Keypads[0].Panel.Position;
                var stand = Walking.Point(Walking.Find(site.Keypad));
                Assert.That(Vector2.Distance(new Vector2(keypad.X, keypad.Z), new Vector2(stand.X, stand.Z)), Is.InRange(0.4f, 1.0f), site.Name);
            }

            Assert.That(Walking.Find(GuardPost), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TheGuard_CanWalkFromTheirPost_ToEverySite_AndRoundIt()
        {
            foreach (var site in Sites)
            {
                var there = Walking.Path(GuardPost, site.Outside);
                Assert.That(there, Is.Not.Null, $"to {site.Name}");
                Assert.That(Doors(there), Has.Member(MillWorks.CentreDoorName), "out through the ARC's door");
                Assert.That(Doors(there), Has.Member(MillWorks.DoorName), "and the front door");
                Assert.That(Walking.Path(site.Outside, GuardPost), Is.Not.Null, $"back from {site.Name}");

                var places = new[] { site.Keypad }.Concat(site.Rooms).Concat(site.Loot).ToList();
                foreach (var place in places)
                {
                    Assert.That(Walking.Path(site.Outside, place), Is.Not.Null, place);
                    Assert.That(Walking.Path(place, site.Outside), Is.Not.Null, place);
                }
            }

            // The way in to the café is through its door; the flat's through its own.
            Assert.That(Doors(Walking.Path($"{CafeAndFlat.ShopAlarm}: outside", $"{CafeAndFlat.ShopAlarm}: keypad")), Is.EqualTo(new[] { "Shop door" }));
            Assert.That(Doors(Walking.Path($"{CafeAndFlat.FlatAlarm}: outside", $"{CafeAndFlat.FlatAlarm}: bedroom")), Is.EqualTo(new[] { "Flat door" }));
        }

        [Test]
        public void TheWalk_FromMillWorksToTheCopperKettle_IsAFewMinutes_AtMost()
        {
            var path = Walking.Path(GuardPost, $"{CafeAndFlat.ShopAlarm}: outside");

            Assert.That(path.Length, Is.InRange(80f, 220f));
        }

        [Test]
        public void PeopleCanComeAndGo_FromTheEdgeOfTown()
        {
            Assert.That(Walking.Entrances, Has.Count.GreaterThanOrEqualTo(3));
            foreach (var site in Sites)
            {
                foreach (var entrance in Walking.Entrances)
                {
                    Assert.That(Walking.Path(entrance, Walking.Find(site.Outside)), Is.Not.Null, $"from {Walking.Point(entrance)} to {site.Name}");
                }
            }
        }

        [Test]
        public void ThePolice_CanDriveToEverySite_AndAwayAgain()
        {
            Assert.That(Driving.Entrances, Is.Not.Empty);
            Assert.That(Driving.Exits, Is.Not.Empty);
            foreach (var site in Sites)
            {
                var outside = Walking.Point(Walking.Find(site.Outside));
                var parking = Driving.Nearest(outside);
                Assert.That(Vector3.Distance(Driving.Point(parking), outside), Is.LessThan(14f), $"somewhere to stop near {site.Name}");
                Assert.That(Driving.Entrances.Any(e => Driving.Path(e, parking) != null), Is.True, $"a way in to {site.Name}");
                Assert.That(Driving.Exits.Any(e => Driving.Path(parking, e) != null), Is.True, $"a way out from {site.Name}");
            }
        }

        [Test]
        public void Cars_KeepLeft()
        {
            // Along the high street, eastbound traffic is on the south (right-hand, from above) side... of
            // a road running east: the left of the direction of travel is north, so it drives north of the centreline.
            var high = GeneratedVillage.Layout.Roads[0];
            var east = Driving.Path(Driving.Nearest(new Vector3(-60f, 0f, -1.5f)), Driving.Nearest(new Vector3(60f, 0f, 4f)));
            Assert.That(east, Is.Not.Null);
            foreach (var stop in east.Stops.Skip(2).Take(10))
            {
                var flat = new Vector2(stop.Position.X, stop.Position.Z);
                var hit = high.Centre.Closest(flat);
                var left = new Vector2(-hit.Tangent.Y, hit.Tangent.X);
                Assert.That(Vector2.Dot(flat - hit.Point, left), Is.GreaterThan(0.5f), $"{stop.Position} is on the left");
            }
        }

        [Test]
        public void NoWalk_GoesThroughAWall_OrTheFurniture()
        {
            // A body's worth of lines along every link, from the knees to the shoulders: nothing
            // solid (doors swing out of the way, so they're left out) may cross any of them.
            var solid = SolidGeometry.Village;
            var failures = new List<string>();
            for (var a = 0; a < Walking.Count; a++)
            {
                foreach (var link in Walking.Links(a))
                {
                    if (link.To < a && Walking.Linked(link.To, a))
                    {
                        continue;
                    }

                    var p = Walking.Point(a);
                    var q = Walking.Point(link.To);
                    var along = q - p;
                    var level = Vector3.Normalize(new Vector3(along.X, 0f, along.Z) + new Vector3(1e-6f, 0f, 0f));
                    var side = new Vector3(-level.Z, 0f, level.X) * (WalkMotion.BodyRadius - 0.08f);
                    foreach (var up in new[] { 0.45f, 1.0f, 1.55f })
                    {
                        foreach (var offset in new[] { Vector3.Zero, side, -side })
                        {
                            var lift = new Vector3(0f, up, 0f) + offset;
                            if (solid.Hits(p + lift, q + lift))
                            {
                                failures.Add($"{Describe(a)} to {Describe(link.To)} at {up} m{(offset == Vector3.Zero ? string.Empty : " (side)")}");
                            }
                        }
                    }
                }
            }

            Assert.That(failures.Distinct().Take(25), Is.Empty, $"{failures.Count} blocked");
        }

        [Test]
        public void NoDrive_GoesThroughAnything()
        {
            var solid = SolidGeometry.Village;
            var failures = new List<string>();
            for (var a = 0; a < Driving.Count; a++)
            {
                foreach (var link in Driving.Links(a))
                {
                    var p = Driving.Point(a);
                    var q = Driving.Point(link.To);
                    var along = q - p;
                    var level = Vector3.Normalize(new Vector3(along.X, 0f, along.Z) + new Vector3(1e-6f, 0f, 0f));
                    var side = new Vector3(-level.Z, 0f, level.X) * 0.85f;
                    foreach (var up in new[] { 0.5f, 1.3f })
                    {
                        foreach (var offset in new[] { Vector3.Zero, side, -side })
                        {
                            var lift = new Vector3(0f, up, 0f) + offset;
                            if (solid.Hits(p + lift, q + lift))
                            {
                                failures.Add($"{p} to {q} at {up} m");
                            }
                        }
                    }
                }
            }

            Assert.That(failures.Distinct().Take(25), Is.Empty, $"{failures.Count} blocked");
        }

        private static string Describe(int stop) => Walking.Name(stop) ?? Walking.Point(stop).ToString("F2", null);
    }
}
