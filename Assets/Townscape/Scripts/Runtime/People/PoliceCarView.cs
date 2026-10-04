using System.Collections.Generic;
using Townscape.Generation.Geometry;
using Townscape.Generation.People;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.Security;
using Townscape.Simulation.Audio;
using Townscape.Simulation.People;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.People
{
    /// <summary>
    /// The police car, drawn: it follows the road's rise and fall (over the bridge's hump), its
    /// wheels turn, the two blue lenses on its roof flash in turn (each throwing blue light about
    /// the street), and the siren wails on the way in.
    /// </summary>
    internal sealed class PoliceCarView
    {
        private static readonly Color Blue = new Color(0.12f, 0.3f, 1f);
        private static readonly Color LensUnlit = new Color(0.08f, 0.16f, 0.5f);

        private readonly GameObject _root;
        private readonly List<Transform> _wheels = new List<Transform>();
        private readonly List<AlarmGlow> _lenses = new List<AlarmGlow>();
        private readonly List<Light> _lights = new List<Light>();
        private readonly AudioSource _siren;
        private Vector3 _last;
        private float _spin;
        private bool _placed;

        public PoliceCarView(CarModel model, MaterialLibrary materials, Transform parent, HideFlags hideFlags, ICollection<Object> owned)
        {
            _root = TownMeshSpawner.CreateChild("Police car", parent, hideFlags);
            Part("Body", model.Body, Vector3.zero, _root.transform);
            foreach (var wheel in model.Wheels)
            {
                _wheels.Add(Part("Wheel", model.Wheel, ToUnity(wheel), _root.transform));
            }

            // Something solid to walk into; on the Ignore Raycast layer so it doesn't hide anyone.
            var box = _root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.8f, 0f);
            box.size = new Vector3(PeopleModels.CarWidth, 1.1f, PeopleModels.CarLength);
            _root.layer = 2;
            _root.AddComponent<Rigidbody>().isKinematic = true;

            // The light bar: a glow over each lens, and a blue light above it.
            foreach (var (centre, half) in model.BlueLights)
            {
                var position = ToUnity(centre);
                _lenses.Add(new AlarmGlow("Blue light", _root.transform, hideFlags, materials.Get(SurfaceMaterial.AlarmStrobe), LensUnlit, position, Vector3.forward, ToUnity(half) * 2.06f, owned));
                var light = TownMeshSpawner.CreateChild("Blue glow", _root.transform, hideFlags).AddComponent<Light>();
                light.transform.localPosition = position + (Vector3.up * 0.25f);
                light.type = LightType.Point;
                light.color = Blue;
                light.range = 14f;
                light.shadows = LightShadows.None;
                light.enabled = false;
                _lights.Add(light);
            }

            var samples = PoliceSounds.Siren();
            var clip = AudioClip.Create("Siren", samples.Length, 1, ProceduralSounds.SampleRate, false);
            clip.SetData(samples, 0);
            owned.Add(clip);
            _siren = _root.AddComponent<AudioSource>();
            _siren.clip = clip;
            _siren.loop = true;
            _siren.playOnAwake = false;
            _siren.spatialBlend = 1f;
            _siren.rolloffMode = AudioRolloffMode.Logarithmic;
            _siren.minDistance = 8f;
            _siren.maxDistance = 300f;
            _siren.dopplerLevel = 0.6f;
            _root.SetActive(false);

            Transform Part(string name, MeshData data, Vector3 at, Transform under)
            {
                var mesh = MeshConversion.ToUnityMesh(data);
                owned.Add(mesh);
                var child = TownMeshSpawner.CreateChild(name, under, hideFlags);
                child.layer = 2;
                child.transform.localPosition = at;
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                var shared = new Material[data.Submeshes.Count];
                for (var i = 0; i < shared.Length; i++)
                {
                    shared[i] = materials.Get(data.Submeshes[i].Material);
                }

                renderer.sharedMaterials = shared;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                return child.transform;
            }
        }

        public void Sync(PoliceCar car, float deltaTime)
        {
            var visible = car != null && car.Visible;
            if (_root.activeSelf != visible)
            {
                _root.SetActive(visible);
                _placed = false;
            }

            if (!visible)
            {
                return;
            }

            // Along the road, pitched with its slope (worked out from where it's just been).
            var position = ToUnity(car.Position);
            var transform = _root.transform;
            var level = ToUnity(car.Facing);
            var moved = position - _last;
            var travel = moved.magnitude;
            var forward = _placed && travel > 0.02f && Vector3.Dot(moved, level) > 0f ? moved / travel : level;
            var facing = Quaternion.LookRotation(forward, Vector3.up);
            transform.SetPositionAndRotation(position, _placed && deltaTime > 0f ? Quaternion.RotateTowards(transform.rotation, facing, 180f * deltaTime) : facing);
            _spin += (_placed ? travel : 0f) / PeopleModels.WheelRadius * Mathf.Rad2Deg;
            foreach (var wheel in _wheels)
            {
                wheel.localRotation = Quaternion.AngleAxis(_spin, Vector3.right);
            }

            _last = position;
            _placed = true;

            // The blue lights: left, left, right, right, twice a second.
            var phase = Mathf.Repeat(Time.time * 2f, 1f);
            for (var i = 0; i < _lenses.Count; i++)
            {
                var start = i * 0.5f;
                var t = phase - start;
                var on = car.Lights && t >= 0f && t < 0.5f && (t < 0.16f || (t > 0.24f && t < 0.4f));
                _lenses[i].Set(on ? Blue.linear * 12f : Color.black);
                _lights[i].enabled = on;
                _lights[i].intensity = on ? 7f : 0f;
            }

            if (car.Siren && !_siren.isPlaying)
            {
                _siren.Play();
            }
            else if (!car.Siren && _siren.isPlaying)
            {
                _siren.Stop();
            }
        }

        public void Destroy() => ObjectUtility.Destroy(_root);

        private static Vector3 ToUnity(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
