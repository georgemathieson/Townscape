using System.Collections.Generic;
using Townscape.Generation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.Rendering
{
    /// <summary>Creates GameObjects with renderers for every generated mesh, grouped by category.</summary>
    public static class TownMeshSpawner
    {
        public static void Spawn(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, ICollection<Object> owned)
        {
            var groups = new Dictionary<MeshCategory, Transform>();
            foreach (var generated in town.Meshes)
            {
                if (!groups.TryGetValue(generated.Category, out var group))
                {
                    group = CreateChild(generated.Category.ToString(), parent, hideFlags).transform;
                    groups.Add(generated.Category, group);
                }

                var mesh = MeshConversion.ToUnityMesh(generated.Mesh);
                owned.Add(mesh);

                var gameObject = CreateChild(generated.Mesh.Name, group, hideFlags);
                gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = gameObject.AddComponent<MeshRenderer>();
                var sharedMaterials = new Material[generated.Mesh.Submeshes.Count];
                for (var i = 0; i < sharedMaterials.Length; i++)
                {
                    sharedMaterials[i] = materials.Get(generated.Mesh.Submeshes[i].Material);
                }

                renderer.sharedMaterials = sharedMaterials;
                renderer.shadowCastingMode = CastsShadows(generated.Category) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
        }

        public static GameObject CreateChild(string name, Transform parent, HideFlags hideFlags)
        {
            var child = new GameObject(name) { hideFlags = hideFlags };
            child.transform.SetParent(parent, false);
            return child;
        }

        private static bool CastsShadows(MeshCategory category) =>
            category != MeshCategory.Water && category != MeshCategory.Markings;
    }
}
