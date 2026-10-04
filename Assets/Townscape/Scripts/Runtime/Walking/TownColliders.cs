using System.Collections.Generic;
using Townscape.Generation;
using Townscape.Runtime.Rendering;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Townscape.Runtime.Walking
{
    /// <summary>
    /// Makes the town solid for walking: a mesh collider on every solid mesh (the ground, the
    /// fells, buildings, the bridge and street furniture) and a capsule round every tree trunk.
    /// Cooking the collision meshes is the slow part, so it runs on worker threads as soon as the
    /// town is built, and walking waits until it is done.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TownColliders : MonoBehaviour
    {
        private const float TrunkHeight = 3f;

        private readonly List<SpawnedMesh> _solid = new List<SpawnedMesh>();
        private IReadOnlyList<TownAnchor> _anchors;
        private HideFlags _hideFlags;
        private NativeArray<int> _meshIds;
        private JobHandle _baking;

        /// <summary>True once everything is solid.</summary>
        public bool IsReady { get; private set; }

        public void Initialize(IReadOnlyList<SpawnedMesh> spawned, IReadOnlyList<TownAnchor> anchors, HideFlags hideFlags)
        {
            _anchors = anchors;
            _hideFlags = hideFlags;
            foreach (var mesh in spawned)
            {
                if (MeshCategories.IsSolid(mesh.Generated.Category))
                {
                    _solid.Add(mesh);
                }
            }

            _meshIds = new NativeArray<int>(_solid.Count, Allocator.Persistent);
            for (var i = 0; i < _solid.Count; i++)
            {
                _meshIds[i] = _solid[i].Mesh.GetInstanceID();
            }

            _baking = new BakeJob { MeshIds = _meshIds }.Schedule(_meshIds.Length, 1);
        }

        private void Update()
        {
            if (IsReady || !_meshIds.IsCreated || !_baking.IsCompleted)
            {
                return;
            }

            _baking.Complete();
            _meshIds.Dispose();

            // The meshes are already cooked, so these just pick up the results.
            foreach (var mesh in _solid)
            {
                mesh.Renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.Mesh;
            }

            var trunks = TownMeshSpawner.CreateChild("Tree Trunks", transform, _hideFlags).transform;
            foreach (var anchor in _anchors)
            {
                if (anchor.Kind != AnchorKind.TreeTrunk)
                {
                    continue;
                }

                var trunk = TownMeshSpawner.CreateChild("Trunk", trunks, _hideFlags);
                trunk.transform.position = new Vector3(anchor.Position.X, anchor.Position.Y, anchor.Position.Z);
                var capsule = trunk.AddComponent<CapsuleCollider>();
                capsule.radius = anchor.Size;
                capsule.height = TrunkHeight;
                capsule.center = new Vector3(0f, TrunkHeight * 0.5f, 0f);
            }

            IsReady = true;
        }

        private void OnDestroy()
        {
            if (_meshIds.IsCreated)
            {
                _baking.Complete();
                _meshIds.Dispose();
            }
        }

        private struct BakeJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<int> MeshIds;

            public void Execute(int index) => Physics.BakeMesh(MeshIds[index], false);
        }
    }
}
