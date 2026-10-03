using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation;
using Townscape.Generation.Geometry;

namespace Townscape.Simulation.Weather
{
    /// <summary>
    /// The height of whatever rain lands on first, on a grid: roofs, awnings, the bridge, the
    /// ground and the water. Rain stops (and splashes) there, so it never falls through a roof or
    /// lands under a shop awning. Built once by rasterising the generated meshes from above.
    /// </summary>
    public sealed class RainCatchMap
    {
        private readonly float[] _heights;
        private readonly int _columns;
        private readonly int _rows;

        public RainCatchMap(Vector2 min, float cellSize, int columns, int rows)
        {
            Min = min;
            CellSize = cellSize;
            _columns = columns;
            _rows = rows;
            _heights = new float[columns * rows];
            Array.Fill(_heights, float.NegativeInfinity);
        }

        public Vector2 Min { get; }

        public float CellSize { get; }

        /// <summary>
        /// A map of <paramref name="halfExtent"/> metres either side of the origin, covering the
        /// ground, water, buildings, structures and street furniture. Trees are left out: rain
        /// falling through leaves looks right, and splashing on top of them would not.
        /// </summary>
        public static RainCatchMap Build(GeneratedTown town, float halfExtent, float cellSize)
        {
            var cells = (int)MathF.Ceiling(2f * halfExtent / cellSize);
            var map = new RainCatchMap(new Vector2(-halfExtent), cellSize, cells, cells);
            foreach (var generated in town.Meshes)
            {
                if (generated.Category != MeshCategory.Vegetation && generated.Category != MeshCategory.Markings)
                {
                    map.Add(generated.Mesh);
                }
            }

            return map;
        }

        /// <summary>The height rain stops at, or negative infinity outside the map or where nothing was drawn.</summary>
        public float HeightAt(float x, float z)
        {
            var column = (int)MathF.Floor((x - Min.X) / CellSize);
            var row = (int)MathF.Floor((z - Min.Y) / CellSize);
            if (column < 0 || row < 0 || column >= _columns || row >= _rows)
            {
                return float.NegativeInfinity;
            }

            return _heights[(row * _columns) + column];
        }

        /// <summary>Raises the map to the top of every triangle in <paramref name="mesh"/> seen from above.</summary>
        public void Add(MeshData mesh)
        {
            foreach (var submesh in mesh.Submeshes)
            {
                var indices = submesh.Indices;
                for (var i = 0; i + 2 < indices.Length; i += 3)
                {
                    AddTriangle(mesh.Positions[indices[i]], mesh.Positions[indices[i + 1]], mesh.Positions[indices[i + 2]]);
                }
            }
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            // Twice the area seen from above; walls are edge-on and catch nothing.
            var area = ((b.X - a.X) * (c.Z - a.Z)) - ((c.X - a.X) * (b.Z - a.Z));
            if (MathF.Abs(area) < 1e-6f)
            {
                return;
            }

            var minColumn = Math.Max(0, (int)MathF.Floor((MathF.Min(a.X, MathF.Min(b.X, c.X)) - Min.X) / CellSize));
            var maxColumn = Math.Min(_columns - 1, (int)MathF.Floor((MathF.Max(a.X, MathF.Max(b.X, c.X)) - Min.X) / CellSize));
            var minRow = Math.Max(0, (int)MathF.Floor((MathF.Min(a.Z, MathF.Min(b.Z, c.Z)) - Min.Y) / CellSize));
            var maxRow = Math.Min(_rows - 1, (int)MathF.Floor((MathF.Max(a.Z, MathF.Max(b.Z, c.Z)) - Min.Y) / CellSize));

            for (var row = minRow; row <= maxRow; row++)
            {
                var z = Min.Y + ((row + 0.5f) * CellSize);
                for (var column = minColumn; column <= maxColumn; column++)
                {
                    var x = Min.X + ((column + 0.5f) * CellSize);

                    // Barycentric coordinates of the cell centre.
                    var u = (((b.X - x) * (c.Z - z)) - ((c.X - x) * (b.Z - z))) / area;
                    var v = (((c.X - x) * (a.Z - z)) - ((a.X - x) * (c.Z - z))) / area;
                    var w = 1f - u - v;
                    if (u < -1e-4f || v < -1e-4f || w < -1e-4f)
                    {
                        continue;
                    }

                    var height = (u * a.Y) + (v * b.Y) + (w * c.Y);
                    var index = (row * _columns) + column;
                    if (height > _heights[index])
                    {
                        _heights[index] = height;
                    }
                }
            }
        }
    }
}
