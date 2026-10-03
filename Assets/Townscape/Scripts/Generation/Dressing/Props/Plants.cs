using System;
using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Dressing.Props
{
    /// <summary>Small planting pieces shared by props, buildings and ground cover.</summary>
    public static class Planting
    {
        public static readonly SurfaceMaterial[] Blooms =
        {
            SurfaceMaterial.PaintPink, SurfaceMaterial.PaintPurple, SurfaceMaterial.PaintRed,
            SurfaceMaterial.PaintWhite, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintOrange,
        };

        /// <summary>A tiny four-sided flower head.</summary>
        public static void Bloom(MeshBuilder builder, Vector3 position, float size, SurfaceMaterial material)
        {
            builder.AddCone(position, size, size * 1.1f, 4, material, capBase: true, rotation: 0.4f);
        }

        /// <summary>A mound of blooms and leaves, for baskets, window boxes and flower beds.</summary>
        public static void Mound(MeshBuilder builder, Random random, Vector3 centre, Vector3 right, Vector3 forward, float halfWidth, float halfDepth, int blooms)
        {
            builder.AddBlob(centre, new Vector3(halfWidth, 0.09f, halfDepth), SurfaceMaterial.LeafGreen, random, 0.2f);
            var colour = Blooms[random.Next(Blooms.Length)];
            for (var i = 0; i < blooms; i++)
            {
                var x = (((float)random.NextDouble() * 2f) - 1f) * halfWidth * 0.9f;
                var z = (((float)random.NextDouble() * 2f) - 1f) * halfDepth * 0.9f;
                if (random.NextDouble() < 0.3)
                {
                    colour = Blooms[random.Next(Blooms.Length)];
                }

                Bloom(builder, centre + (right * x) + (forward * z) + new Vector3(0f, 0.05f, 0f), 0.045f, colour);
            }
        }

        /// <summary>A hanging basket: a dark bowl overflowing with flowers and trailing greenery.</summary>
        public static void Basket(PropFrame f, float x, float y, float z, float radius)
        {
            f.Cone(x, y + 0.18f, z, radius, -0.2f, 8, SurfaceMaterial.Iron);
            f.Prism(x, y + 0.16f, z, radius, 0.04f, 8, SurfaceMaterial.LeafDark);
            Mound(f.Builder, f.Random, f.Point(x, y + 0.24f, z), f.Right, f.Forward, radius * 1.1f, radius * 1.1f, 9);
            for (var i = 0; i < 5; i++)
            {
                var angle = MathF.PI * 2f * i / 5f;
                f.Blob(x + (MathF.Cos(angle) * radius), y + 0.02f, z + (MathF.Sin(angle) * radius), 0.07f, 0.16f, 0.07f, SurfaceMaterial.LeafGreen, 0.2f);
            }
        }

        /// <summary>Three double-sided blades of grass.</summary>
        public static void Tuft(MeshBuilder builder, Random random, Vector3 root, float height)
        {
            for (var i = 0; i < 3; i++)
            {
                var angle = (MathF.PI * 2f * i / 3f) + (float)random.NextDouble();
                var side = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle)) * 0.06f;
                var lean = new Vector3(MathF.Sin(angle), 0f, -MathF.Cos(angle)) * (height * 0.25f);
                var tip = root + lean + new Vector3(0f, height * (0.7f + (0.3f * (float)random.NextDouble())), 0f);
                var normal = Vector3.Cross(side, tip - root);
                builder.AddTriangleFacing(root - side, root + side, tip, normal, SurfaceMaterial.GrassTuft);
                builder.AddTriangleFacing(root - side, root + side, tip, -normal, SurfaceMaterial.GrassTuft);
            }
        }
    }

    /// <summary>A clump of wild flowers.</summary>
    public sealed class FlowerClump : IProp
    {
        public void Build(PropFrame f)
        {
            var colour = Planting.Blooms[f.Random.Next(Planting.Blooms.Length)];
            var count = 3 + f.Random.Next(4);
            for (var i = 0; i < count; i++)
            {
                var x = f.Range(-0.25f, 0.25f);
                var z = f.Range(-0.25f, 0.25f);
                var height = f.Range(0.18f, 0.35f);
                f.Span(x - 0.008f, 0f, z - 0.008f, x + 0.008f, height, z + 0.008f, SurfaceMaterial.LeafGreen);
                Planting.Bloom(f.Builder, f.Point(x, height, z), 0.05f, colour);
            }
        }
    }
}
