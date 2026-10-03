using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Terrain
{
    /// <summary>
    /// The coarse low poly fells around the village: a big square ring of terrain with the detailed
    /// core cut out of the middle. Steep faces turn to scree and the high ground to bracken.
    /// </summary>
    public sealed class FellsGenerator
    {
        private const int MaxCellsPerMesh = 9000;

        private readonly BaseTerrain _terrain;
        private readonly TownLayout _layout;
        private readonly GenerationSettings _settings;

        public FellsGenerator(BaseTerrain terrain, TownLayout layout, GenerationSettings settings)
        {
            _terrain = terrain;
            _layout = layout;
            _settings = settings;
        }

        public IReadOnlyList<MeshData> Generate()
        {
            var cell = _settings.FarCellSize;
            var cells = (int)MathF.Round(_settings.FarHalfExtent * 2f / cell);
            var origin = new Vector2(-_settings.FarHalfExtent, -_settings.FarHalfExtent);
            var core = _settings.CoreHalfExtent;

            var heights = new float[(cells + 1) * (cells + 1)];
            for (var j = 0; j <= cells; j++)
            {
                for (var i = 0; i <= cells; i++)
                {
                    heights[(j * (cells + 1)) + i] = _terrain.FarHeightAt(origin + new Vector2(i * cell, j * cell));
                }
            }

            var meshes = new List<MeshData>();
            var builder = new MeshBuilder();
            var cellsInBuilder = 0;
            for (var j = 0; j < cells; j++)
            {
                for (var i = 0; i < cells; i++)
                {
                    var min = origin + new Vector2(i * cell, j * cell);
                    var max = min + new Vector2(cell, cell);
                    var insideCore = min.X >= -core - 1e-3f && max.X <= core + 1e-3f && min.Y >= -core - 1e-3f && max.Y <= core + 1e-3f;
                    if (insideCore)
                    {
                        continue;
                    }

                    var p00 = GeoMath.At(min, heights[(j * (cells + 1)) + i]);
                    var p10 = GeoMath.At(new Vector2(max.X, min.Y), heights[(j * (cells + 1)) + i + 1]);
                    var p11 = GeoMath.At(max, heights[((j + 1) * (cells + 1)) + i + 1]);
                    var p01 = GeoMath.At(new Vector2(min.X, max.Y), heights[((j + 1) * (cells + 1)) + i]);

                    // Alternate diagonals for a less regular, more hand-made facet pattern.
                    if (((i + j) & 1) == 0)
                    {
                        AddFacet(builder, p00, p01, p11);
                        AddFacet(builder, p00, p11, p10);
                    }
                    else
                    {
                        AddFacet(builder, p00, p01, p10);
                        AddFacet(builder, p01, p11, p10);
                    }

                    if (++cellsInBuilder >= MaxCellsPerMesh)
                    {
                        meshes.Add(builder.Build($"Fells {meshes.Count}"));
                        builder = new MeshBuilder();
                        cellsInBuilder = 0;
                    }
                }
            }

            if (!builder.IsEmpty)
            {
                meshes.Add(builder.Build($"Fells {meshes.Count}"));
            }

            return meshes;
        }

        private void AddFacet(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c)
        {
            builder.AddTriangleFacing(a, b, c, Vector3.UnitY, MaterialFor(a, b, c));
        }

        private SurfaceMaterial MaterialFor(Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            var centre = (a + b + c) / 3f;
            var flat = GeoMath.Flat(centre);
            var steepness = 1f - MathF.Abs(normal.Y);

            if (centre.Y < _layout.WaterLevel + 0.35f)
            {
                return SurfaceMaterial.Shingle;
            }

            if (steepness > 0.32f)
            {
                return SurfaceMaterial.Scree;
            }

            // Lush village grass near the core, giving way to rough fell grass and bracken further out.
            var distanceOut = GeoMath.ChebyshevLength(flat) - _settings.CoreHalfExtent;
            var patch = _terrain.PatchNoise(flat, 0.02f);
            if (distanceOut < 25f + (patch * 50f))
            {
                return patch > 0.62f ? SurfaceMaterial.GrassDark : SurfaceMaterial.Grass;
            }

            if (centre.Y > 12f && patch > 0.55f)
            {
                return SurfaceMaterial.Bracken;
            }

            return patch < 0.3f ? SurfaceMaterial.GrassDark : SurfaceMaterial.FellGrass;
        }
    }
}
