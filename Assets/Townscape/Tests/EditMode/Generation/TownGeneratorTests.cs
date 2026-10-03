using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Structures;

namespace Townscape.Tests.Generation
{
    public sealed class TownGeneratorTests
    {
        [Test]
        public void ProducesEveryCategory()
        {
            var categories = GeneratedVillage.Town.Meshes.Select(m => m.Category).Distinct().ToList();

            Assert.That(categories, Is.EquivalentTo(new[]
            {
                MeshCategory.Ground,
                MeshCategory.Fells,
                MeshCategory.Water,
                MeshCategory.Markings,
                MeshCategory.Structure,
                MeshCategory.Building,
            }));
        }

        [Test]
        public void MeshesFitSixteenBitIndices()
        {
            foreach (var generated in GeneratedVillage.Town.Meshes)
            {
                Assert.That(generated.Mesh.VertexCount, Is.LessThanOrEqualTo(65535), generated.Mesh.Name);
            }
        }

        [Test]
        public void NormalsAreUnitLength()
        {
            foreach (var generated in GeneratedVillage.Town.Meshes)
            {
                foreach (var normal in generated.Mesh.Normals)
                {
                    Assert.That(normal.Length(), Is.EqualTo(1f).Within(1e-3f), generated.Mesh.Name);
                }
            }
        }

        [Test]
        public void GroundSnapping_RarelyFlipsTriangles()
        {
            var stats = GeneratedVillage.Town.GroundStats;

            Assert.That(stats.SnappedVertices, Is.GreaterThan(1000), "edges should be snapped");
            Assert.That(stats.FlippedTriangles, Is.LessThanOrEqualTo(stats.TopTriangles / 2000));
            Assert.That(stats.StepFaces, Is.GreaterThan(0), "kerbs and walls should exist");
        }

        [Test]
        public void Generation_IsDeterministic()
        {
            var again = new TownGenerator().Generate(GeneratedVillage.Layout);
            var first = GeneratedVillage.Town.Meshes;

            Assert.That(again.Meshes.Count, Is.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(again.Meshes[i].Mesh.VertexCount, Is.EqualTo(first[i].Mesh.VertexCount));
                Assert.That(again.Meshes[i].Mesh.Positions, Is.EqualTo(first[i].Mesh.Positions));
            }
        }

        [Test]
        public void Markings_StayOnTheCarriageway()
        {
            var ground = GeneratedVillage.Town.Context.Ground;
            var markings = GeneratedVillage.Town.Meshes.Single(m => m.Category == MeshCategory.Markings).Mesh;
            var positions = markings.Positions;

            for (var i = 0; i < positions.Length; i += 3)
            {
                var centroid = (positions[i] + positions[i + 1] + positions[i + 2]) / 3f;
                var flat = new Vector2(centroid.X, centroid.Z);
                Assert.That(ground.Classify(flat).Kind, Is.EqualTo(RegionKind.Road).Or.EqualTo(RegionKind.Pavement), $"marking at {flat}");
            }
        }

        [Test]
        public void Bridge_MeetsTheRoadAndClearsTheWater()
        {
            var spec = GeneratedVillage.Layout.Bridges[0];
            var bridge = new HumpbackBridgeBuilder(spec);

            Assert.That(bridge.DeckHeight(-spec.HalfLength), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(bridge.DeckHeight(spec.HalfLength), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(bridge.DeckHeight(0f), Is.EqualTo(spec.HumpHeight).Within(1e-4f));
            Assert.That(bridge.ArchHeight(spec.ArchHalfSpan), Is.EqualTo(spec.SpringingHeight).Within(1e-3f));
            Assert.That(bridge.ArchHeight(0f), Is.EqualTo(spec.SpringingHeight + spec.ArchRise).Within(1e-3f));
            Assert.That(spec.SpringingHeight, Is.GreaterThan(GeneratedVillage.Layout.WaterLevel));

            for (var u = -spec.ArchHalfSpan; u <= spec.ArchHalfSpan; u += 0.25f)
            {
                Assert.That(bridge.DeckHeight(u) - bridge.ArchHeight(u), Is.GreaterThan(0.6f), $"deck too thin at u={u}");
            }
        }

        [Test]
        public void Bridge_ArchSpringsFromTheEmbankments()
        {
            var layout = GeneratedVillage.Layout;

            Assert.That(layout.Bridges[0].ArchHalfSpan, Is.EqualTo(layout.River.WallHalfWidth));
        }

        [Test]
        public void SurfacePalette_CoversEveryMaterial()
        {
            foreach (SurfaceMaterial material in System.Enum.GetValues(typeof(SurfaceMaterial)))
            {
                var appearance = SurfacePalette.Get(material);
                Assert.That(appearance.R == 1f && appearance.G == 0f && appearance.B == 1f, Is.False, material.ToString());
            }
        }

        [Test]
        public void CustomStructureStrategies_AreUsed()
        {
            var generator = new TownGenerator(new IStructureGenerator[0]);

            var town = generator.Generate(new LakeDistrictVillageLayout().Create());

            Assert.That(town.Meshes.Any(m => m.Category == MeshCategory.Structure), Is.False);
        }
    }
}
