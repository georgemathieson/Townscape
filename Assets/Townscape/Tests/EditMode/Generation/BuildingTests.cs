using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Buildings;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;

namespace Townscape.Tests.Generation
{
    public sealed class BuildingTests
    {
        private static IReadOnlyList<BuildingPlan> Plans => GeneratedVillage.Town.Context.Buildings;

        [Test]
        public void FellsideCoffee_IsOnTheHighStreet_WithRoomInFrontToStand()
        {
            var shop = ShopLocator.Find(Plans, VillageShops.FellsideCoffee);

            Assert.That(shop, Is.Not.Null);
            var front = (shop.Footprint.FrontLeft + shop.Footprint.FrontRight) * 0.5f;
            foreach (var distance in new[] { 2f, 5f, 8.5f })
            {
                var spot = front + (shop.Footprint.Outward * distance);
                Assert.That(Plans.Any(plan => plan.Footprint.Contains(spot)), Is.False, $"{distance} m out from the shop is inside a building");
            }

            Assert.That(ShopLocator.Find(Plans, new ShopDefinition("NOWHERE", Townscape.Generation.Geometry.SurfaceMaterial.PaintTeal, null)), Is.Null);
        }

        [Test]
        public void TheViewOfFellsideCoffee_StandsInTheStreet_LookingAtTheShop()
        {
            var shop = ShopLocator.Find(Plans, VillageShops.FellsideCoffee).Footprint;
            var ground = GeneratedVillage.Town.Context.Ground;

            var view = ShopLocator.ViewOf(shop, ground.HeightAt);

            var eye = new Vector2(view.Eye.X, view.Eye.Z);
            Assert.That(Plans.Any(plan => plan.Footprint.Contains(eye)), Is.False, "the camera stands outside every building");
            Assert.That(view.Eye.Y - ground.HeightAt(eye), Is.EqualTo(ShopLocator.EyeHeight).Within(1e-4f));
            var toShop = new Vector2(view.LookAt.X, view.LookAt.Z) - eye;
            Assert.That(Vector2.Dot(Vector2.Normalize(toShop), shop.Outward), Is.LessThan(-0.9f), "looking back at the shopfront");
        }

        [Test]
        public void Buildings_KeepOffRoadsRiverAndPaths()
        {
            // Classify against the ground as it would be without any building plots.
            var layout = GeneratedVillage.Layout;
            var withoutPlots = new GroundModel(
                GeneratedVillage.Town.Context.Terrain,
                TownGenerator.CreateFeatures(layout, Array.Empty<BuildingPlan>()));

            foreach (var plan in Plans)
            {
                foreach (var point in InteriorSamples(plan.Footprint))
                {
                    var kind = withoutPlots.Classify(point).Kind;
                    Assert.That(kind, Is.EqualTo(RegionKind.OpenGround), $"{plan.Group} stands on {kind} at {point}");
                }
            }
        }

        [Test]
        public void Buildings_DoNotOverlapOtherBuildings()
        {
            foreach (var a in Plans)
            {
                foreach (var b in Plans)
                {
                    if (ReferenceEquals(a, b) || a.Group == b.Group)
                    {
                        continue;
                    }

                    foreach (var point in InteriorSamples(a.Footprint))
                    {
                        Assert.That(Inside(b.Footprint, point), Is.False, $"{a.Group} overlaps {b.Group} at {point}");
                    }
                }
            }
        }

        [Test]
        public void TerraceNeighbours_ShareTheirPartyWalls()
        {
            // Units vary in depth, so neighbours share a front corner and the line of the party
            // wall, but not necessarily the back corner.
            foreach (var group in Plans.GroupBy(p => p.Group).Where(g => g.Count() > 1))
            {
                var units = group.ToList();
                for (var i = 0; i < units.Count - 1; i++)
                {
                    var a = units[i].Footprint;
                    var b = units[i + 1].Footprint;
                    var (wallA, wallB) = Vector2.Distance(a.FrontRight, b.FrontLeft) < 1e-3f
                        ? ((a.FrontRight, a.BackRight), (b.FrontLeft, b.BackLeft))
                        : ((a.FrontLeft, a.BackLeft), (b.FrontRight, b.BackRight));

                    Assert.That(Vector2.Distance(wallA.Item1, wallB.Item1), Is.LessThan(1e-3f), $"{group.Key} units {i} and {i + 1} share a front corner");
                    var directionA = Vector2.Normalize(wallA.Item2 - wallA.Item1);
                    var directionB = Vector2.Normalize(wallB.Item2 - wallB.Item1);
                    Assert.That(Vector2.Dot(directionA, directionB), Is.GreaterThan(0.9999f), $"{group.Key} units {i} and {i + 1} party wall line");
                }
            }
        }

        [Test]
        public void Buildings_StandOnFlatYards()
        {
            var ground = GeneratedVillage.Town.Context.Ground;
            foreach (var plan in Plans)
            {
                var centre = plan.Footprint.Centre;
                Assert.That(ground.Classify(centre).Kind, Is.EqualTo(RegionKind.Yard), plan.Group);
                Assert.That(ground.HeightAt(centre), Is.EqualTo(RoadFeature.PavementHeight).Within(1e-4f), plan.Group);
            }
        }

        [Test]
        public void Buildings_StayInsideTheDetailedCore()
        {
            var limit = GenerationSettings.Default.CoreHalfExtent - 1f;
            foreach (var plan in Plans)
            {
                foreach (var corner in plan.Footprint.Corners)
                {
                    Assert.That(Math.Abs(corner.X), Is.LessThan(limit), plan.Group);
                    Assert.That(Math.Abs(corner.Y), Is.LessThan(limit), plan.Group);
                }
            }
        }

        [Test]
        public void Village_HasTheMustHaveShops()
        {
            var shops = AllShops().ToList();

            Assert.That(shops.Count(s => s.Display is BookshopDisplay), Is.GreaterThanOrEqualTo(2), "bookshops");
            Assert.That(shops.Count(s => s.Display is CoffeeShopDisplay), Is.GreaterThanOrEqualTo(2), "coffee shops");
            Assert.That(shops.Count(s => s.Display is ComputerShopDisplay), Is.GreaterThanOrEqualTo(1), "computer shop");
            Assert.That(shops.Count(s => s.Display is NewsagentDisplay), Is.GreaterThanOrEqualTo(1), "newsagent");
        }

        [Test]
        public void ShopSigns_UseOnlySupportedCharacters()
        {
            foreach (var shop in AllShops())
            {
                foreach (var character in shop.Name)
                {
                    Assert.That(PixelFont.Supports(character), Is.True, $"'{character}' in {shop.Name}");
                }
            }
        }

        [Test]
        public void Buildings_LeaveAnchorsForLightsAndSmoke()
        {
            var anchors = GeneratedVillage.Town.Anchors;

            Assert.That(anchors.Count(a => a.Kind == AnchorKind.Chimney), Is.GreaterThan(50));
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.Window), Is.GreaterThan(100));
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.ShopWindow), Is.GreaterThanOrEqualTo(18));
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.DoorLamp), Is.GreaterThan(0));
        }

        [Test]
        public void TerraceRoofs_StayAboveTheirEaves()
        {
            foreach (var plan in Plans)
            {
                if (plan.Style is TerracedUnitStyle unit)
                {
                    Assert.That(unit.Design.Ridge, Is.GreaterThan(unit.Design.Eaves + 2f), plan.Group);
                    Assert.That(unit.Design.Eaves, Is.GreaterThan(BuildingLevels.Floor + unit.Design.GroundFloorHeight), plan.Group);
                }
            }
        }

        [Test]
        public void PixelFont_WritesOneQuadPerRunOfPixels()
        {
            var builder = new Townscape.Generation.Geometry.MeshBuilder();
            var wall = WallFrame.FromBase(Vector2.Zero, new Vector2(10f, 0f));

            PixelFont.Write(builder, wall, "I", 5f, 1f, 0.1f, 10f, 0.01f, Townscape.Generation.Geometry.SurfaceMaterial.PaintGold);

            // "I" is a bar on top, five single-pixel rows and a bar at the bottom: 7 runs, 2 triangles each.
            Assert.That(builder.Build("text").TriangleCount, Is.EqualTo(14));
        }

        private static IEnumerable<ShopDefinition> AllShops()
        {
            var layout = GeneratedVillage.Layout;
            return layout.Terraces.SelectMany(t => t.Units).Select(u => u.Shop).Where(s => s != null)
                .Concat(new[] { VillageShops.Inkwell });
        }

        private static IEnumerable<Vector2> InteriorSamples(Footprint footprint)
        {
            for (var a = 0.06f; a < 0.95f; a += 0.22f)
            {
                for (var b = 0.06f; b < 0.95f; b += 0.22f)
                {
                    yield return footprint.At(a, b);
                }
            }
        }

        private static bool Inside(Footprint footprint, Vector2 p)
        {
            var corners = footprint.Corners;
            var sign = 0;
            for (var i = 0; i < corners.Length; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) % corners.Length];
                var cross = ((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X));
                var s = Math.Sign(cross);
                if (s == 0)
                {
                    continue;
                }

                if (sign == 0)
                {
                    sign = s;
                }
                else if (s != sign)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
