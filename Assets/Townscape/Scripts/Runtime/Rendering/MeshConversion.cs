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
            if (data.Uvs != null)
            {
                var uvs = new Vector2[data.VertexCount];
                for (var i = 0; i < data.VertexCount; i++)
                {
                    uvs[i] = new Vector2(data.Uvs[i].X, data.Uvs[i].Y);
                }

                mesh.SetUVs(0, uvs);
            }

            // Plants are re-shaped every frame by the wind.
            if (data.SwayHeights != null)
            {
                mesh.MarkDynamic();
            }

            mesh.subMeshCount = data.Submeshes.Count;
            for (var i = 0; i < data.Submeshes.Count; i++)
            {
                mesh.SetTriangles(data.Submeshes[i].Indices, i, false);
            }

            // Textured surfaces (water, puddles) carry normal maps, which need tangents.
            if (data.Uvs != null)
            {
                mesh.RecalculateTangents();
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
