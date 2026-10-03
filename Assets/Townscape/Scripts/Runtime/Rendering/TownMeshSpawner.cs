using System.Collections.Generic;
using Townscape.Generation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.Rendering
{
    /// <summary>A generated mesh as it was spawned, for systems that animate it (wind sway).</summary>
    public sealed class SpawnedMesh
    {
        public SpawnedMesh(GeneratedMesh generated, Mesh mesh, MeshRenderer renderer)
        {
            Generated = generated;
            Mesh = mesh;
            Renderer = renderer;
        }

        public GeneratedMesh Generated { get; }

        public Mesh Mesh { get; }

        public MeshRenderer Renderer { get; }
    }

    /// <summary>Creates GameObjects with renderers for every generated mesh, grouped by category.</summary>
    public static class TownMeshSpawner
    {
        public static IReadOnlyList<SpawnedMesh> Spawn(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, ICollection<Object> owned)
        {
            var spawned = new List<SpawnedMesh>();
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
                spawned.Add(new SpawnedMesh(generated, mesh, renderer));
            }

            return spawned;
        }

        public static GameObject CreateChild(string name, Transform parent, HideFlags hideFlags)
        {
            var child = new GameObject(name) { hideFlags = hideFlags };
            child.transform.SetParent(parent, false);
            return child;
        }

        private static bool CastsShadows(MeshCategory category) =>
            category != MeshCategory.Water && category != MeshCategory.Markings && category != MeshCategory.Puddles;
    }
}
