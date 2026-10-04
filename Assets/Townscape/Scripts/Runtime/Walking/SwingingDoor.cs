using System.Collections.Generic;
using Townscape.Generation;
using Townscape.Runtime.Rendering;
using UnityEngine;

namespace Townscape.Runtime.Walking
{
    /// <summary>
    /// A door (or roof window) that opens and shuts when you look at it and press E: it swings
    /// about its hinge into the room over about half a second, easing in and out.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwingingDoor : MonoBehaviour, IInteractable
    {
        private const float SwingSeconds = 0.6f;

        private TownDoor _door;
        private float _side = 1f;
        private Vector3 _axis = Vector3.up;
        private float _amount;
        private bool _open;

        public string Prompt => _door == null ? string.Empty : (_open ? "Close the " : "Open the ") + _door.Noun;

        public bool IsOpen => _open;

        /// <summary>Creates every door in the town, hung shut. In play mode each gets a collider that swings with it.</summary>
        public static IReadOnlyList<SwingingDoor> SpawnAll(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, ICollection<Object> owned, bool solid)
        {
            var spawned = new List<SwingingDoor>();
            if (town.Doors.Count == 0)
            {
                return spawned;
            }

            var group = TownMeshSpawner.CreateChild("Doors", parent, hideFlags).transform;
            foreach (var door in town.Doors)
            {
                var hinge = TownMeshSpawner.CreateChild(door.Name, group, hideFlags);
                hinge.transform.position = new Vector3(door.Hinge.X, door.Hinge.Y, door.Hinge.Z);

                var mesh = MeshConversion.ToUnityMesh(door.Leaf);
                owned.Add(mesh);
                var leaf = TownMeshSpawner.CreateChild("Leaf", hinge.transform, hideFlags);
                leaf.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = leaf.AddComponent<MeshRenderer>();
                var shared = new Material[door.Leaf.Submeshes.Count];
                for (var i = 0; i < shared.Length; i++)
                {
                    shared[i] = materials.Get(door.Leaf.Submeshes[i].Material);
                }

                renderer.sharedMaterials = shared;
                if (solid)
                {
                    var collider = leaf.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    collider.convex = true;
                    hinge.AddComponent<Rigidbody>().isKinematic = true;
                }

                var swinging = hinge.AddComponent<SwingingDoor>();
                swinging.Initialize(door);
                spawned.Add(swinging);
            }

            return spawned;
        }

        public void Interact() => _open = !_open;

        private void Initialize(TownDoor door)
        {
            _door = door;

            // Swing whichever way about the hinge carries the latch into the room.
            var along = new Vector3(door.Along.X, door.Along.Y, door.Along.Z);
            var inward = new Vector3(door.Inward.X, door.Inward.Y, door.Inward.Z);
            _axis = new Vector3(door.Axis.X, door.Axis.Y, door.Axis.Z);
            _side = Vector3.Dot(Quaternion.AngleAxis(90f, _axis) * along, inward) >= 0f ? 1f : -1f;
        }

        private void Update()
        {
            if (_door == null)
            {
                return;
            }

            var target = _open ? 1f : 0f;
            if (Mathf.Approximately(_amount, target))
            {
                return;
            }

            _amount = Mathf.MoveTowards(_amount, target, Time.deltaTime / SwingSeconds);
            var eased = _amount * _amount * (3f - (2f * _amount));
            transform.localRotation = Quaternion.AngleAxis(_side * _door.OpenDegrees * eased, _axis);
        }
    }
}
