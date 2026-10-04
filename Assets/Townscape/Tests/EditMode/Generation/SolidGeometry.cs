using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation;

namespace Townscape.Tests.Generation
{
    /// <summary>
    /// The town's solid triangles (buildings, structures and furniture, not the ground), bucketed
    /// on a grid so a test can ask quickly whether a straight line hits any of them.
    /// </summary>
    internal sealed class SolidGeometry
    {
        private const float Cell = 4f;

        private static SolidGeometry _village;

        private readonly List<(Vector3 A, Vector3 B, Vector3 C)> _triangles = new List<(Vector3, Vector3, Vector3)>();
        private readonly Dictionary<(int, int), List<int>> _cells = new Dictionary<(int, int), List<int>>();

        private SolidGeometry(GeneratedTown town)
        {
            foreach (var generated in town.Meshes)
            {
                var category = generated.Category;
                if (!MeshCategories.IsSolid(category) || category == MeshCategory.Ground || category == MeshCategory.Fells)
                {
                    continue;
                }

                var mesh = generated.Mesh;
                foreach (var submesh in mesh.Submeshes)
                {
                    var indices = submesh.Indices;
                    for (var i = 0; i + 2 < indices.Length; i += 3)
                    {
                        Add(mesh.Positions[indices[i]], mesh.Positions[indices[i + 1]], mesh.Positions[indices[i + 2]]);
                    }
                }
            }
        }

        public static SolidGeometry Village => _village ??= new SolidGeometry(GeneratedVillage.Town);

        /// <summary>Whether the straight line from <paramref name="p"/> to <paramref name="q"/> passes through a solid triangle.</summary>
        public bool Hits(Vector3 p, Vector3 q)
        {
            var seen = new HashSet<int>();
            foreach (var cell in Cells(Math.Min(p.X, q.X), Math.Min(p.Z, q.Z), Math.Max(p.X, q.X), Math.Max(p.Z, q.Z)))
            {
                if (!_cells.TryGetValue(cell, out var list))
                {
                    continue;
                }

                foreach (var index in list)
                {
                    if (seen.Add(index))
                    {
                        var (a, b, c) = _triangles[index];
                        if (Crosses(p, q, a, b, c))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // Möller–Trumbore, limited to the segment.
        public static bool Crosses(Vector3 p, Vector3 q, Vector3 a, Vector3 b, Vector3 c)
        {
            var direction = q - p;
            var e1 = b - a;
            var e2 = c - a;
            var h = Vector3.Cross(direction, e2);
            var det = Vector3.Dot(e1, h);
            if (Math.Abs(det) < 1e-9f)
            {
                return false;
            }

            var s = p - a;
            var u = Vector3.Dot(s, h) / det;
            var k = Vector3.Cross(s, e1);
            var v = Vector3.Dot(direction, k) / det;
            var t = Vector3.Dot(e2, k) / det;
            return u >= 0f && v >= 0f && u + v <= 1f && t >= 0f && t <= 1f;
        }

        private void Add(Vector3 a, Vector3 b, Vector3 c)
        {
            var index = _triangles.Count;
            _triangles.Add((a, b, c));
            var minX = Math.Min(a.X, Math.Min(b.X, c.X));
            var maxX = Math.Max(a.X, Math.Max(b.X, c.X));
            var minZ = Math.Min(a.Z, Math.Min(b.Z, c.Z));
            var maxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));
            foreach (var cell in Cells(minX, minZ, maxX, maxZ))
            {
                if (!_cells.TryGetValue(cell, out var list))
                {
                    list = new List<int>();
                    _cells[cell] = list;
                }

                list.Add(index);
            }
        }

        private static IEnumerable<(int, int)> Cells(float minX, float minZ, float maxX, float maxZ)
        {
            var x0 = (int)MathF.Floor(minX / Cell);
            var x1 = (int)MathF.Floor(maxX / Cell);
            var z0 = (int)MathF.Floor(minZ / Cell);
            var z1 = (int)MathF.Floor(maxZ / Cell);
            if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > 4096)
            {
                // Something huge (the fells, a water plane): it can't be bucketed usefully.
                x1 = x0;
                z1 = z0;
            }

            for (var x = x0; x <= x1; x++)
            {
                for (var z = z0; z <= z1; z++)
                {
                    yield return (x, z);
                }
            }
        }
    }
}
