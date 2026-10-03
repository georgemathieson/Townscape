using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Tests.Generation
{
    public sealed class StormGenerationTests
    {
        private static readonly GroundModel Ground = TownGenerator.CreateContext(GeneratedVillage.Layout, GenerationSettings.Default).Ground;

        private static MeshData[] Meshes(MeshCategory category) =>
            GeneratedVillage.Town.Meshes.Where(m => m.Category == category).Select(m => m.Mesh).ToArray();

        [Test]
        public void Puddles_LieOnHardSurfaces_FacingUp()
        {
            var puddles = Meshes(MeshCategory.Puddles);
            var triangles = puddles.Sum(m => m.TriangleCount);
            Assert.That(triangles / 12, Is.InRange(30, 60), "puddles of twelve triangles each");

            var allowed = new[] { RegionKind.Road, RegionKind.Path, RegionKind.Yard, RegionKind.Pavement };
            foreach (var mesh in puddles)
            {
                Assert.That(mesh.Uvs, Is.Not.Null);
                Assert.That(mesh.Submeshes.Select(s => s.Material), Is.EqualTo(new[] { SurfaceMaterial.Puddle }));
                for (var i = 0; i < mesh.VertexCount; i++)
                {
                    var flat = GeoMath.Flat(mesh.Positions[i]);
                    Assert.That(mesh.Normals[i].Y, Is.GreaterThan(0.9f));
                    Assert.That(allowed, Does.Contain(Ground.Classify(flat).Kind));
                    Assert.That(GeneratedVillage.Town.Context.Buildings.Any(b => b.Footprint.Contains(flat)), Is.False, "never indoors");
                    Assert.That(mesh.Positions[i].Y - Ground.HeightAt(flat), Is.EqualTo(0.02f).Within(1e-3f));
                }
            }
        }

        [Test]
        public void Plants_KnowHowHighEachVertexIsAboveTheirBase()
        {
            var vegetation = Meshes(MeshCategory.Vegetation);
            Assert.That(vegetation.All(m => m.SwayHeights != null && m.SwayHeights.Length == m.VertexCount));

            var tallest = vegetation.Max(m => m.SwayHeights.Max());
            Assert.That(tallest, Is.InRange(6f, 25f), "tree tops");
            Assert.That(vegetation.All(m => m.SwayHeights.All(h => h >= 0f)));
        }

        [Test]
        public void Furniture_AndBuildings_NeverSway()
        {
            var rigid = GeneratedVillage.Town.Meshes.Where(m => m.Category == MeshCategory.Furniture || m.Category == MeshCategory.Building);
            Assert.That(rigid.All(m => m.Mesh.SwayHeights == null));
        }

        [Test]
        public void RiverSurface_FlowsDownstream_FromTheFellsToTheLake()
        {
            var river = Meshes(MeshCategory.Water).Single(m => m.Submeshes.Any(s => s.Material == SurfaceMaterial.RiverWater));
            var layout = GeneratedVillage.Layout;

            Assert.That(river.Uvs, Is.Not.Null);
            Assert.That(river.Positions.All(p => MathF.Abs(p.Y - layout.WaterLevel) < 1e-4f));
            Assert.That(river.Normals.All(n => n.Y > 0.99f), "faces up");

            // v runs along the river, so it grows from the northern end to the southern end.
            var north = Enumerable.Range(0, river.VertexCount).OrderByDescending(i => river.Positions[i].Z).First();
            var south = Enumerable.Range(0, river.VertexCount).OrderBy(i => river.Positions[i].Z).First();
            Assert.That(river.Uvs[south].Y - river.Uvs[north].Y, Is.EqualTo(layout.River.Centre.Length).Within(5f));
        }

        [Test]
        public void WaterPlane_SitsJustBelowTheRiver()
        {
            var plane = Meshes(MeshCategory.Water).Single(m => m.Submeshes.Any(s => s.Material == SurfaceMaterial.Water));

            Assert.That(plane.Uvs, Is.Not.Null);
            Assert.That(plane.Positions[0].Y, Is.EqualTo(GeneratedVillage.Layout.WaterLevel - 0.03f).Within(1e-4f));
        }
    }
}
