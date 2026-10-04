using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Dressing;
using Townscape.Generation.Dressing.Props;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;

namespace Townscape.Tests.Generation
{
    public sealed class DressingTests
    {
        private static Vector2 Flat(Vector3 v) => new Vector2(v.X, v.Z);

        [Test]
        public void StreetLamps_StandOnPavementsAndVergesNotRoads()
        {
            var ground = GeneratedVillage.Town.Context.Ground;
            var lamps = GeneratedVillage.Town.Anchors.Where(a => a.Kind == AnchorKind.StreetLamp).ToList();

            Assert.That(lamps.Count, Is.GreaterThan(20));
            foreach (var lamp in lamps)
            {
                var kind = ground.Classify(Flat(lamp.Position)).Kind;
                Assert.That(kind, Is.EqualTo(RegionKind.Pavement).Or.EqualTo(RegionKind.OpenGround), $"lamp at {lamp.Position} is on {kind}");
            }
        }

        [Test]
        public void StreetLamps_AreSpacedOut()
        {
            var lamps = GeneratedVillage.Town.Anchors.Where(a => a.Kind == AnchorKind.StreetLamp).Select(a => Flat(a.Position)).ToList();
            for (var i = 0; i < lamps.Count; i++)
            {
                for (var j = i + 1; j < lamps.Count; j++)
                {
                    Assert.That(Vector2.Distance(lamps[i], lamps[j]), Is.GreaterThan(5f));
                }
            }
        }

        [Test]
        public void ZebraCrossing_HasTwoBeacons()
        {
            Assert.That(GeneratedVillage.Town.Anchors.Count(a => a.Kind == AnchorKind.Beacon), Is.EqualTo(2));
        }

        [Test]
        public void PhoneBox_HasALitSign()
        {
            // The phone box stands by the bridge; the petrol station's price sign is lit too.
            var phoneBox = new Vector2(-12.5f, 9f);
            var signs = GeneratedVillage.Town.Anchors.Where(a => a.Kind == AnchorKind.LitSign).ToList();
            Assert.That(signs.Count(a => Vector2.Distance(new Vector2(a.Position.X, a.Position.Z), phoneBox) < 2f), Is.EqualTo(1));
        }

        [Test]
        public void Dressing_ProducesFurnitureAndVegetation()
        {
            var meshes = GeneratedVillage.Town.Meshes;
            var furniture = meshes.Where(m => m.Category == MeshCategory.Furniture).Sum(m => m.Mesh.TriangleCount);
            var vegetation = meshes.Where(m => m.Category == MeshCategory.Vegetation).Sum(m => m.Mesh.TriangleCount);

            Assert.That(furniture, Is.GreaterThan(10000), "lamps, walls, railings and props");
            Assert.That(vegetation, Is.GreaterThan(20000), "trees, grass and flowers");
            Assert.That(meshes.Count, Is.LessThan(150), "dressing should be chunked, not one mesh per prop");
        }

        [Test]
        public void Occupancy_KeepsLaterPlacementsApart()
        {
            var context = new DressingContext(GeneratedVillage.Town.Context, new System.Collections.Generic.List<TownAnchor>());
            context.Place(new Bench(), new Vector2(-30f, 30f), Vector2.UnitY, DressingLayer.Furniture, new Random(1));

            Assert.That(context.IsOccupied(new Vector2(-30.5f, 30f), 0.5f), Is.True);
            Assert.That(context.IsOccupied(new Vector2(-25f, 30f), 0.5f), Is.False);
        }

        [Test]
        public void Blob_FacesOutwards()
        {
            var builder = new MeshBuilder();
            builder.AddBlob(new Vector3(3f, 4f, 5f), new Vector3(1f, 2f, 1f), SurfaceMaterial.LeafGreen, new Random(3), 0.2f);
            var mesh = builder.Build("blob");

            Assert.That(mesh.TriangleCount, Is.EqualTo(20));
            for (var i = 0; i < mesh.Positions.Length; i += 3)
            {
                var centroid = (mesh.Positions[i] + mesh.Positions[i + 1] + mesh.Positions[i + 2]) / 3f;
                Assert.That(Vector3.Dot(centroid - new Vector3(3f, 4f, 5f), mesh.Normals[i]), Is.GreaterThan(0f));
            }
        }

        [Test]
        public void Trees_DoNotGrowOnRoadsPathsOrYards()
        {
            // Every broadleaf trunk is a bark prism; check each one's base is on open ground.
            var ground = GeneratedVillage.Town.Context.Ground;
            var core = GenerationSettings.Default.CoreHalfExtent - 1f;
            foreach (var mesh in GeneratedVillage.Town.Meshes.Where(m => m.Category == MeshCategory.Vegetation))
            {
                foreach (var submesh in mesh.Mesh.Submeshes.Where(s => s.Material == SurfaceMaterial.Bark || s.Material == SurfaceMaterial.BirchBark))
                {
                    for (var i = 0; i < submesh.Indices.Length; i += 3)
                    {
                        var p = mesh.Mesh.Positions[submesh.Indices[i]];
                        var flat = Flat(p);
                        if (Math.Abs(flat.X) > core || Math.Abs(flat.Y) > core)
                        {
                            continue;
                        }

                        var kind = ground.Classify(flat).Kind;
                        Assert.That(kind, Is.Not.EqualTo(RegionKind.Road).And.Not.EqualTo(RegionKind.Yard).And.Not.EqualTo(RegionKind.RiverBed), $"trunk at {flat}");
                    }
                }
            }
        }
    }
}
