using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Buildings;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;

namespace Townscape.Tests.Generation
{
    public sealed class PetrolStationTests
    {
        private static readonly SurfaceMaterial[] BulbColours =
        {
            SurfaceMaterial.BulbRed, SurfaceMaterial.BulbGreen, SurfaceMaterial.BulbOrange, SurfaceMaterial.BulbYellow, SurfaceMaterial.BulbBlue,
        };

        [Test]
        public void Village_HasOnePetrolStation_OffTheHighStreet_OnAConcreteForecourt()
        {
            var stations = GeneratedVillage.Town.Context.Buildings.Where(plan => plan.Style is PetrolStationStyle).ToList();

            Assert.That(stations, Has.Count.EqualTo(1));
            var site = stations[0].Footprint;
            var high = GeneratedVillage.Layout.Roads.Single(road => road.Name == "High Street");
            var front = (site.FrontLeft + site.FrontRight) * 0.5f;
            var road = high.Centre.Closest(front);
            Assert.That(road.Distance, Is.LessThan(high.HalfWidth + high.PavementWidth + 1f), "the forecourt opens off the High Street pavement");
            Assert.That(Vector2.Dot(site.Outward, Vector2.Normalize(road.Point - front)), Is.GreaterThan(0.9f), "facing the road");

            var forecourt = site.At(0.6f, 0.25f);
            var region = GeneratedVillage.Town.Context.Ground.Classify(forecourt);
            Assert.That(region.Kind, Is.EqualTo(RegionKind.Yard));
            Assert.That(region.TopAt(forecourt), Is.EqualTo(SurfaceMaterial.Concrete));
        }

        [Test]
        public void TwoPumps_StandSideBySide_ParallelToTheGarage_UnderTheCanopy()
        {
            var station = Station.Build();
            var pumps = PetrolStationStyle.PumpPositions;

            Assert.That(pumps, Has.Count.EqualTo(2));
            Assert.That(pumps[0].Y, Is.EqualTo(pumps[1].Y), "on one island, the same distance back from the road as each other");
            Assert.That(Math.Abs(pumps[1].X - pumps[0].X), Is.GreaterThan(1f));

            // The canopy's ceiling lights spread over both pumps, well above a van.
            var ceiling = station.Vertices(SurfaceMaterial.CanopyLight).ToList();
            Assert.That(ceiling, Is.Not.Empty);
            var plan = ceiling.Select(station.ToSite).ToList();
            foreach (var pump in pumps)
            {
                Assert.That(pump.X, Is.InRange(plan.Min(p => p.X), plan.Max(p => p.X)));
                Assert.That(pump.Y, Is.InRange(plan.Min(p => p.Y), plan.Max(p => p.Y)));
            }

            Assert.That(ceiling.Min(v => v.Y), Is.GreaterThan(4f));
        }

        [Test]
        public void ShopWindows_HaveStringLights_InRedGreenOrangeYellowAndBlue()
        {
            var station = Station.Build();

            var counts = BulbColours.Select(colour => station.Vertices(colour).Count()).ToList();
            Assert.That(counts, Has.All.GreaterThan(0));
            Assert.That(counts.Max() - counts.Min(), Is.LessThanOrEqualTo(counts.Max() / 2), "the colours take turns along the strings");

            // Just behind the glass, between sill and head height.
            var glass = station.Vertices(SurfaceMaterial.ShopGlass).Max(v => station.ToSite(v).Y);
            foreach (var bulb in BulbColours.SelectMany(station.Vertices))
            {
                Assert.That(station.ToSite(bulb).Y, Is.InRange(glass, glass + 0.3f));
                Assert.That(bulb.Y, Is.InRange(BuildingLevels.Floor + 0.9f, BuildingLevels.Floor + 2.5f));
            }
        }

        [Test]
        public void Bunting_IsStrungAboutTheForecourt_AboveHeadHeight()
        {
            // A colour used nowhere else on the station, so every triangle of it is a flag.
            var station = Station.Build(new PetrolStationDesign { Bunting = new[] { SurfaceMaterial.PaintPurple } });

            var flags = station.Vertices(SurfaceMaterial.PaintPurple).ToList();
            Assert.That(flags.Count / 6, Is.GreaterThan(40), "flags, each a double-sided triangle");
            Assert.That(flags.Min(v => v.Y), Is.GreaterThan(RoadFeature.PavementHeight + 2.2f));
        }

        [Test]
        public void Station_LeavesAnchorsForItsLights()
        {
            var anchors = Station.Build().Anchors;

            Assert.That(anchors.Count(a => a.Kind == AnchorKind.CanopyLight), Is.EqualTo(2));
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.LitSign), Is.EqualTo(1), "the price sign");
            Assert.That(anchors.Count(a => a.Kind == AnchorKind.ShopWindow), Is.EqualTo(1));
            Assert.That(anchors.Where(a => a.Kind == AnchorKind.CanopyLight).Select(a => a.Position.Y), Has.All.GreaterThan(4f));
        }

        [Test]
        public void Station_StaysInsideItsPlot()
        {
            var station = Station.Build();

            var outside = station.Mesh.Positions.Select(station.ToSite)
                .Where(p => p.X < -0.01f || p.X > station.Site.FrontWidth + 0.01f || p.Y < -0.01f || p.Y > station.Site.Depth + 0.01f)
                .ToList();
            Assert.That(outside, Is.Empty);
        }

        [Test]
        public void Station_NeedsABigEnoughPlot()
        {
            var small = Footprint.FromFront(Vector2.Zero, -Vector2.UnitY, 15f, 12f);
            var context = new BuildContext(new MeshBuilder(), new List<TownAnchor>(), 1);

            Assert.Throws<ArgumentException>(() => new PetrolStationStyle().Build(small, context));
        }

        /// <summary>A station built on its own, on the smallest plot it allows.</summary>
        private sealed class Station
        {
            private Station(MeshData mesh, List<TownAnchor> anchors, Footprint site)
            {
                Mesh = mesh;
                Anchors = anchors;
                Site = site;
            }

            public MeshData Mesh { get; }

            public List<TownAnchor> Anchors { get; }

            public Footprint Site { get; }

            public static Station Build(PetrolStationDesign design = null)
            {
                var site = Footprint.FromFront(Vector2.Zero, -Vector2.UnitY, PetrolStationStyle.MinimumWidth, PetrolStationStyle.MinimumDepth);
                var builder = new MeshBuilder();
                var anchors = new List<TownAnchor>();
                new PetrolStationStyle(design).Build(site, new BuildContext(builder, anchors, 7));
                return new Station(builder.Build("Petrol station"), anchors, site);
            }

            public IEnumerable<Vector3> Vertices(SurfaceMaterial material) =>
                Mesh.Submeshes.Where(s => s.Material == material).SelectMany(s => s.Indices).Select(i => Mesh.Positions[i]);

            /// <summary>Metres from the plot's front-left corner: x along the road, y back from it.</summary>
            public Vector2 ToSite(Vector3 p)
            {
                var offset = new Vector2(p.X, p.Z) - Site.FrontLeft;
                var right = Vector2.Normalize(Site.FrontRight - Site.FrontLeft);
                var back = Vector2.Normalize(Site.BackLeft - Site.FrontLeft);
                return new Vector2(Vector2.Dot(offset, right), Vector2.Dot(offset, back));
            }
        }
    }
}
