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
        private List<Vector2> _uvs;
        private List<float> _sway;

        public int VertexCount => _positions.Count;

        public bool IsEmpty => _positions.Count == 0;

        /// <summary>
        /// While set, every vertex added records its height above this level, so the wind can sway
        /// it. Set it to the ground level under a plant while building the plant, then clear it.
        /// </summary>
        public float? SwayBase { get; set; }

        /// <summary>Adds a triangle wound clockwise as seen from its front (Unity's convention).</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, SurfaceMaterial material)
        {
            Add(a, b, c, material, null);
        }

        /// <summary>Adds a triangle with texture coordinates, wound clockwise as seen from its front.</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, SurfaceMaterial material)
        {
            Add(a, b, c, material, (uvA, uvB, uvC));
        }

        /// <summary>Adds the quad a-b-c-d with texture coordinates, wound clockwise from the front.</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, SurfaceMaterial material)
        {
            Add(a, b, c, material, (uvA, uvB, uvC));
            Add(a, c, d, material, (uvA, uvC, uvD));
        }

        private void Add(Vector3 a, Vector3 b, Vector3 c, SurfaceMaterial material, (Vector2 A, Vector2 B, Vector2 C)? uvs)
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
            if (uvs.HasValue)
            {
                EnsureUvs();
                _uvs.Add(uvs.Value.A);
                _uvs.Add(uvs.Value.B);
                _uvs.Add(uvs.Value.C);
            }
            else if (_uvs != null)
            {
                _uvs.Add(PlanUv(a));
                _uvs.Add(PlanUv(b));
                _uvs.Add(PlanUv(c));
            }

            if (SwayBase.HasValue)
            {
                EnsureSway();
                _sway.Add(MathF.Max(0f, a.Y - SwayBase.Value));
                _sway.Add(MathF.Max(0f, b.Y - SwayBase.Value));
                _sway.Add(MathF.Max(0f, c.Y - SwayBase.Value));
            }
            else if (_sway != null)
            {
                _sway.Add(0f);
                _sway.Add(0f);
                _sway.Add(0f);
            }

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

        /// <summary>
        /// Adds an upright prism (a low poly cylinder) standing on <paramref name="baseCentre"/>,
        /// with its top capped. <paramref name="sides"/> of 6–8 reads as round at a distance.
        /// </summary>
        public void AddPrism(Vector3 baseCentre, float radius, float height, int sides, SurfaceMaterial material, bool capBottom = false, float rotation = 0f)
        {
            var top = baseCentre + (Vector3.UnitY * height);
            for (var i = 0; i < sides; i++)
            {
                var a0 = rotation + (MathF.PI * 2f * i / sides);
                var a1 = rotation + (MathF.PI * 2f * (i + 1) / sides);
                var d0 = new Vector3(MathF.Cos(a0), 0f, MathF.Sin(a0)) * radius;
                var d1 = new Vector3(MathF.Cos(a1), 0f, MathF.Sin(a1)) * radius;
                var outward = (d0 + d1) * 0.5f;
                AddQuadFacing(baseCentre + d0, baseCentre + d1, top + d1, top + d0, outward, material);
                AddTriangleFacing(top, top + d0, top + d1, Vector3.UnitY, material);
                if (capBottom)
                {
                    AddTriangleFacing(baseCentre, baseCentre + d0, baseCentre + d1, -Vector3.UnitY, material);
                }
            }
        }

        /// <summary>A cone (a many-sided pyramid) standing on <paramref name="baseCentre"/>, for conifers and caps.</summary>
        public void AddCone(Vector3 baseCentre, float radius, float height, int sides, SurfaceMaterial material, bool capBase = false, float rotation = 0f)
        {
            var apex = baseCentre + (Vector3.UnitY * height);
            for (var i = 0; i < sides; i++)
            {
                var a0 = rotation + (MathF.PI * 2f * i / sides);
                var a1 = rotation + (MathF.PI * 2f * (i + 1) / sides);
                var p0 = baseCentre + (new Vector3(MathF.Cos(a0), 0f, MathF.Sin(a0)) * radius);
                var p1 = baseCentre + (new Vector3(MathF.Cos(a1), 0f, MathF.Sin(a1)) * radius);
                AddTriangleFacing(p0, p1, apex, ((p0 + p1) * 0.5f) - baseCentre + (Vector3.UnitY * radius * 0.5f), material);
                if (capBase)
                {
                    AddTriangleFacing(baseCentre, p0, p1, -Vector3.UnitY, material);
                }
            }
        }

        /// <summary>
        /// A faceted ball: an icosahedron whose corners are pushed in or out by up to
        /// <paramref name="jitter"/> of the radius, for tree crowns, bushes and globes.
        /// </summary>
        public void AddBlob(Vector3 centre, Vector3 radii, SurfaceMaterial material, Random random = null, float jitter = 0f)
        {
            var corners = new Vector3[IcosahedronCorners.Length];
            for (var i = 0; i < corners.Length; i++)
            {
                var scale = 1f + (random == null ? 0f : (((float)random.NextDouble() * 2f) - 1f) * jitter);
                corners[i] = centre + (IcosahedronCorners[i] * radii * scale);
            }

            for (var i = 0; i < IcosahedronFaces.Length; i += 3)
            {
                var a = corners[IcosahedronFaces[i]];
                var b = corners[IcosahedronFaces[i + 1]];
                var c = corners[IcosahedronFaces[i + 2]];
                AddTriangleFacing(a, b, c, ((a + b + c) / 3f) - centre, material);
            }
        }

        private static readonly Vector3[] IcosahedronCorners = CreateIcosahedronCorners();

        private static readonly int[] IcosahedronFaces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        private static Vector3[] CreateIcosahedronCorners()
        {
            var t = (1f + MathF.Sqrt(5f)) / 2f;
            var corners = new[]
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f),
            };

            for (var i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector3.Normalize(corners[i]);
            }

            return corners;
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

            return new MeshData(name, _positions.ToArray(), _normals.ToArray(), submeshes)
            {
                Uvs = _uvs?.ToArray(),
                SwayHeights = _sway?.ToArray(),
            };
        }

        // Vertices added before the first one with texture coordinates are mapped from above, in metres.
        private static Vector2 PlanUv(Vector3 p) => new Vector2(p.X, p.Z);

        private void EnsureUvs()
        {
            if (_uvs == null)
            {
                _uvs = new List<Vector2>(_positions.Count + 3);
                foreach (var position in _positions)
                {
                    _uvs.Add(PlanUv(position));
                }
            }
        }

        private void EnsureSway()
        {
            if (_sway == null)
            {
                _sway = new List<float>(_positions.Count + 3);
                _sway.AddRange(new float[_positions.Count]);
            }
        }
    }
}
