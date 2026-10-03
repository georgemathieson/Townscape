using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation.Geometry
{
    /// <summary>One submesh: the triangles that share a surface material.</summary>
    public sealed class SubmeshData
    {
        public SubmeshData(SurfaceMaterial material, int[] indices)
        {
            Material = material;
            Indices = indices;
        }

        public SurfaceMaterial Material { get; }

        public int[] Indices { get; }
    }

    /// <summary>
    /// Engine-free mesh. Positions use Unity's convention (x east, y up, z north, left-handed),
    /// and triangles are wound clockwise when seen from the front, as Unity expects.
    /// </summary>
    public sealed class MeshData
    {
        public MeshData(string name, Vector3[] positions, Vector3[] normals, IReadOnlyList<SubmeshData> submeshes)
        {
            Name = name;
            Positions = positions;
            Normals = normals;
            Submeshes = submeshes;
        }

        public string Name { get; }

        public Vector3[] Positions { get; }

        public Vector3[] Normals { get; }

        public IReadOnlyList<SubmeshData> Submeshes { get; }

        public int VertexCount => Positions.Length;

        public int TriangleCount
        {
            get
            {
                var count = 0;
                foreach (var submesh in Submeshes)
                {
                    count += submesh.Indices.Length / 3;
                }

                return count;
            }
        }
    }
}
