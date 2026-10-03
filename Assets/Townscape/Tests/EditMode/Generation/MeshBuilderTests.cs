using System.Numerics;
using NUnit.Framework;
using Townscape.Generation.Geometry;

namespace Townscape.Tests.Generation
{
    public sealed class MeshBuilderTests
    {
        [Test]
        public void ClockwiseFromAbove_FacesUp()
        {
            // Unity winds front faces clockwise: origin -> north -> east is clockwise seen from above.
            var builder = new MeshBuilder();
            builder.AddTriangle(Vector3.Zero, new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0f), SurfaceMaterial.Grass);

            var mesh = builder.Build("test");

            Assert.That(mesh.Normals[0].Y, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void AddQuadFacing_FlipsWindingToMatchFacing()
        {
            var builder = new MeshBuilder();
            builder.AddQuadFacing(Vector3.Zero, new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 1f), -Vector3.UnitY, SurfaceMaterial.Stone);

            var mesh = builder.Build("test");

            Assert.That(mesh.TriangleCount, Is.EqualTo(2));
            foreach (var normal in mesh.Normals)
            {
                Assert.That(normal.Y, Is.EqualTo(-1f).Within(1e-5f));
            }
        }

        [Test]
        public void DegenerateTriangles_AreSkipped()
        {
            var builder = new MeshBuilder();
            builder.AddTriangle(Vector3.Zero, Vector3.UnitX, Vector3.UnitX * 2f, SurfaceMaterial.Grass);

            Assert.That(builder.IsEmpty, Is.True);
        }

        [Test]
        public void Submeshes_AreGroupedByMaterial()
        {
            var builder = new MeshBuilder();
            builder.AddBox(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, Vector3.One, SurfaceMaterial.Stone);
            builder.AddTriangle(Vector3.Zero, Vector3.UnitZ, Vector3.UnitX, SurfaceMaterial.Road);

            var mesh = builder.Build("test");

            Assert.That(mesh.Submeshes, Has.Count.EqualTo(2));
            Assert.That(mesh.TriangleCount, Is.EqualTo(11), "five box faces (no bottom) plus one triangle");
        }

        [Test]
        public void Box_FacesPointOutwards()
        {
            var builder = new MeshBuilder();
            builder.AddBox(new Vector3(5f, 5f, 5f), Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, Vector3.One, SurfaceMaterial.Stone, includeBottom: true);

            var mesh = builder.Build("box");

            for (var i = 0; i < mesh.Positions.Length; i++)
            {
                var fromCentre = mesh.Positions[i] - new Vector3(5f, 5f, 5f);
                Assert.That(Vector3.Dot(fromCentre, mesh.Normals[i]), Is.GreaterThan(0f));
            }
        }
    }
}
