using System;
using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation.Geometry
{
    /// <summary>
    /// Accumulates flat-shaded triangles. Vertices are never shared between triangles, so every
    /// face gets its own normal: the faceted low poly look comes for free.
    /// </summary>
    public sealed class MeshBuilder
    {
        private const float MinDoubleArea = 1e-7f;

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly Dictionary<SurfaceMaterial, List<int>> _indices = new Dictionary<SurfaceMaterial, List<int>>();

        public int VertexCount => _positions.Count;

        public bool IsEmpty => _positions.Count == 0;

        /// <summary>Adds a triangle wound clockwise as seen from its front (Unity's convention).</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, SurfaceMaterial material)
        {
            var cross = Vector3.Cross(b - a, c - a);
            var length = cross.Length();
            if (length < MinDoubleArea)
            {
                return;
            }

            var normal = cross / length;
            if (!_indices.TryGetValue(material, out var list))
            {
                list = new List<int>();
                _indices.Add(material, list);
            }

            var start = _positions.Count;
            _positions.Add(a);
            _positions.Add(b);
            _positions.Add(c);
            _normals.Add(normal);
            _normals.Add(normal);
            _normals.Add(normal);
            list.Add(start);
            list.Add(start + 1);
            list.Add(start + 2);
        }

        /// <summary>Adds a triangle, flipping its winding if needed so it faces towards <paramref name="facing"/>.</summary>
        public void AddTriangleFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 facing, SurfaceMaterial material)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f)
            {
                AddTriangle(a, c, b, material);
            }
            else
            {
                AddTriangle(a, b, c, material);
            }
        }

        /// <summary>Adds the quad a-b-c-d (corners in order around the edge), wound clockwise from the front.</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, SurfaceMaterial material)
        {
            AddTriangle(a, b, c, material);
            AddTriangle(a, c, d, material);
        }

        /// <summary>Adds the quad a-b-c-d (corners in order around the edge) so that it faces towards <paramref name="facing"/>.</summary>
        public void AddQuadFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing, SurfaceMaterial material)
        {
            var normal = Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a);
            if (Vector3.Dot(normal, facing) < 0f)
            {
                AddQuad(a, d, c, b, material);
            }
            else
            {
                AddQuad(a, b, c, d, material);
            }
        }

        /// <summary>
        /// Adds an axis-aligned box in a local frame. <paramref name="right"/>, <paramref name="up"/> and
        /// <paramref name="forward"/> must be orthonormal; <paramref name="halfSize"/> is measured along them.
        /// </summary>
        public void AddBox(Vector3 centre, Vector3 right, Vector3 up, Vector3 forward, Vector3 halfSize, SurfaceMaterial material, bool includeBottom = false)
        {
            var x = right * halfSize.X;
            var y = up * halfSize.Y;
            var z = forward * halfSize.Z;

            Vector3 Corner(int sx, int sy, int sz) => centre + (x * sx) + (y * sy) + (z * sz);

            // +X and -X
            AddQuadFacing(Corner(1, -1, -1), Corner(1, 1, -1), Corner(1, 1, 1), Corner(1, -1, 1), right, material);
            AddQuadFacing(Corner(-1, -1, -1), Corner(-1, -1, 1), Corner(-1, 1, 1), Corner(-1, 1, -1), -right, material);

            // +Z and -Z
            AddQuadFacing(Corner(-1, -1, 1), Corner(1, -1, 1), Corner(1, 1, 1), Corner(-1, 1, 1), forward, material);
            AddQuadFacing(Corner(-1, -1, -1), Corner(-1, 1, -1), Corner(1, 1, -1), Corner(1, -1, -1), -forward, material);

            // Top, and optionally bottom
            AddQuadFacing(Corner(-1, 1, -1), Corner(-1, 1, 1), Corner(1, 1, 1), Corner(1, 1, -1), up, material);
            if (includeBottom)
            {
                AddQuadFacing(Corner(-1, -1, -1), Corner(1, -1, -1), Corner(1, -1, 1), Corner(-1, -1, 1), -up, material);
            }
        }

        /// <summary>Adds a four-sided pyramid whose base is the top of a box, for caps on pillars and posts.</summary>
        public void AddPyramid(Vector3 baseCentre, Vector3 right, Vector3 forward, Vector2 halfSize, float height, Vector3 up, SurfaceMaterial material)
        {
            var x = right * halfSize.X;
            var z = forward * halfSize.Y;
            var apex = baseCentre + (up * height);
            var c0 = baseCentre - x - z;
            var c1 = baseCentre + x - z;
            var c2 = baseCentre + x + z;
            var c3 = baseCentre - x + z;

            AddTriangleFacing(c0, c1, apex, -forward, material);
            AddTriangleFacing(c1, c2, apex, right, material);
            AddTriangleFacing(c2, c3, apex, forward, material);
            AddTriangleFacing(c3, c0, apex, -right, material);
        }

        public MeshData Build(string name)
        {
            var submeshes = new List<SubmeshData>();
            foreach (SurfaceMaterial material in Enum.GetValues(typeof(SurfaceMaterial)))
            {
                if (_indices.TryGetValue(material, out var list) && list.Count > 0)
                {
                    submeshes.Add(new SubmeshData(material, list.ToArray()));
                }
            }

            return new MeshData(name, _positions.ToArray(), _normals.ToArray(), submeshes);
        }
    }
}
