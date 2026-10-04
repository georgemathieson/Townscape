using Townscape.Generation.People;
using Townscape.Runtime.Rendering;
using Townscape.Simulation.People;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.People
{
    /// <summary>A figure's meshes and materials, made once for each kind of person and shared by them all.</summary>
    internal sealed class FigureParts
    {
        public FigureParts(FigureModel model, MaterialLibrary materials, System.Collections.Generic.ICollection<Object> owned)
        {
            (Mesh, Material[]) Make(Townscape.Generation.Geometry.MeshData data)
            {
                if (data == null)
                {
                    return (null, null);
                }

                var mesh = MeshConversion.ToUnityMesh(data);
                owned.Add(mesh);
                var shared = new Material[data.Submeshes.Count];
                for (var i = 0; i < shared.Length; i++)
                {
                    shared[i] = materials.Get(data.Submeshes[i].Material);
                }

                return (mesh, shared);
            }

            Body = Make(model.Body);
            LeftLeg = Make(model.LeftLeg);
            RightLeg = Make(model.RightLeg);
            LeftArm = Make(model.LeftArm);
            RightArm = Make(model.RightArm);
            Bag = Make(model.Bag);
        }

        public (Mesh Mesh, Material[] Materials) Body { get; }

        public (Mesh Mesh, Material[] Materials) LeftLeg { get; }

        public (Mesh Mesh, Material[] Materials) RightLeg { get; }

        public (Mesh Mesh, Material[] Materials) LeftArm { get; }

        public (Mesh Mesh, Material[] Materials) RightArm { get; }

        public (Mesh Mesh, Material[] Materials) Bag { get; }
    }

    /// <summary>
    /// Someone walking about the town, drawn: they turn to face the way they're going, and their
    /// legs and arms swing with every step. They have a body to bump into (that sensors, eyes and
    /// the crosshair all see past), and a burglar carries their sack once they've taken something.
    /// </summary>
    internal sealed class FigureView
    {
        // A full stride (a step with each foot) at a walk, and how far the legs swing.
        private const float StrideLength = 1.4f;
        private const float WalkSwingDegrees = 26f;
        private const float RunSwingDegrees = 42f;
        private const float TurnDegreesPerSecond = 520f;

        private readonly GameObject _root;
        private readonly Transform _leftLeg;
        private readonly Transform _rightLeg;
        private readonly Transform _leftArm;
        private readonly Transform _rightArm;
        private readonly GameObject _bag;
        private float _swing;

        public FigureView(Person person, FigureParts parts, Transform parent, HideFlags hideFlags)
        {
            Person = person;
            _root = TownMeshSpawner.CreateChild(person.Name, parent, hideFlags);
            _root.layer = 2;
            Part("Body", parts.Body, Vector3.zero);
            _leftLeg = Part("Left leg", parts.LeftLeg, ToUnity(FigureModel.LeftHip));
            _rightLeg = Part("Right leg", parts.RightLeg, ToUnity(FigureModel.RightHip));
            _leftArm = Part("Left arm", parts.LeftArm, ToUnity(FigureModel.LeftShoulder));
            _rightArm = Part("Right arm", parts.RightArm, ToUnity(FigureModel.RightShoulder));
            _bag = parts.Bag.Mesh != null ? Part("Bag", parts.Bag, Vector3.zero).gameObject : null;

            // On the Ignore Raycast layer, like the walker: you bump into them, but sensors,
            // eyes and the crosshair see straight past.
            var capsule = _root.AddComponent<CapsuleCollider>();
            capsule.height = 1.75f;
            capsule.radius = 0.24f;
            capsule.center = new Vector3(0f, 0.875f, 0f);
            _root.AddComponent<Rigidbody>().isKinematic = true;
            _root.transform.SetPositionAndRotation(ToUnity(person.Position), Quaternion.LookRotation(ToUnity(person.Walker.Facing), Vector3.up));
            Sync(0f);

            Transform Part(string name, (Mesh Mesh, Material[] Materials) part, Vector3 pivot)
            {
                var child = TownMeshSpawner.CreateChild(name, _root.transform, hideFlags);
                child.layer = 2;
                child.transform.localPosition = pivot;
                child.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = part.Materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                return child.transform;
            }
        }

        public Person Person { get; }

        public void Sync(float deltaTime)
        {
            var visible = Person.Visible;
            if (_root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            var walker = Person.Walker;
            var transform = _root.transform;
            var facing = Quaternion.LookRotation(ToUnity(walker.Facing), Vector3.up);
            var turned = deltaTime > 0f ? Quaternion.RotateTowards(transform.rotation, facing, TurnDegreesPerSecond * deltaTime) : facing;
            transform.SetPositionAndRotation(ToUnity(walker.Position), turned);

            // The legs swing with the distance walked, so they never slide; standing, they settle.
            if (walker.Moving)
            {
                var amplitude = Mathf.Lerp(WalkSwingDegrees, RunSwingDegrees, Mathf.InverseLerp(1.6f, 3.2f, walker.CurrentSpeed));
                _swing = Mathf.Sin(walker.Walked * 2f * Mathf.PI / StrideLength) * amplitude;
            }
            else
            {
                _swing = Mathf.MoveTowards(_swing, 0f, 120f * deltaTime);
            }

            // Positive swing: the left foot forward and the left arm back (as PeopleModels.Posed).
            _leftLeg.localRotation = Quaternion.AngleAxis(-_swing, Vector3.right);
            _rightLeg.localRotation = Quaternion.AngleAxis(_swing, Vector3.right);
            _leftArm.localRotation = Quaternion.AngleAxis(_swing * 0.8f, Vector3.right);
            _rightArm.localRotation = Quaternion.AngleAxis(Person.Arrested ? -15f : -_swing * 0.8f, Vector3.right);
            if (_bag != null && _bag.activeSelf != Person.Carrying)
            {
                _bag.SetActive(Person.Carrying);
            }
        }

        public void Destroy() => ObjectUtility.Destroy(_root);

        private static Vector3 ToUnity(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
