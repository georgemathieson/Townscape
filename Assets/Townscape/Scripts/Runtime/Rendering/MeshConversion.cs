using Townscape.Generation.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.Rendering
{
    /// <summary>Turns engine-free <see cref="MeshData"/> into a Unity <see cref="Mesh"/>.</summary>
    public static class MeshConversion
    {
        public static Mesh ToUnityMesh(MeshData data)
        {
            var mesh = new Mesh
            {
                name = data.Name,
                hideFlags = HideFlags.DontSave,
                indexFormat = data.VertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            var vertices = new Vector3[data.VertexCount];
            var normals = new Vector3[data.VertexCount];
            for (var i = 0; i < data.VertexCount; i++)
            {
                var p = data.Positions[i];
                var n = data.Normals[i];
                vertices[i] = new Vector3(p.X, p.Y, p.Z);
                normals[i] = new Vector3(n.X, n.Y, n.Z);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.subMeshCount = data.Submeshes.Count;
            for (var i = 0; i < data.Submeshes.Count; i++)
            {
                mesh.SetTriangles(data.Submeshes[i].Indices, i, false);
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
