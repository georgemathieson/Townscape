using System.Collections.Generic;
using System.Diagnostics;
using Townscape.Generation;
using Townscape.Runtime.Rendering;
using Townscape.Simulation;
using Townscape.Simulation.Diagnostics;
using UnityEngine;

namespace Townscape.Runtime.Lighting
{
    /// <summary>
    /// The town after dark. Puts a point light at every lamp, shop window, inn lantern, beacon and
    /// the phone box, and drives the glow of lanterns, windows, signs and screens. What is lit and
    /// how brightly comes from <see cref="NightLights"/>; this class only applies it to Unity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TownLights : MonoBehaviour
    {
        private const float FadePerSecond = 2.5f;

        private readonly List<AnchoredLight> _lights = new List<AnchoredLight>();
        private readonly SmoothedTiming _timing = new SmoothedTiming();
        private MaterialLibrary _materials;
        private TimeOfDayLighting _time;

        public int LightCount => _lights.Count;

        /// <summary>How long updating the lights and glowing materials takes a frame, smoothed.</summary>
        public float UpdateMilliseconds => _timing.Milliseconds;

        public void Initialize(GeneratedTown town, MaterialLibrary materials, TimeOfDayLighting time, HideFlags hideFlags)
        {
            _materials = materials;
            _time = time;

            foreach (var anchor in town.Anchors)
            {
                if (!NightLights.TryGetLight(anchor.Kind, out var spec))
                {
                    continue;
                }

                var lightObject = TownMeshSpawner.CreateChild($"{anchor.Kind} light", transform, hideFlags);
                var facing = new Vector3(anchor.Facing.X, anchor.Facing.Y, anchor.Facing.Z);
                lightObject.transform.position = new Vector3(anchor.Position.X, anchor.Position.Y, anchor.Position.Z) + (facing * spec.Outward);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = spec.Range;
                // NightLights colours are linear; Unity treats light colours as sRGB.
                light.color = ToColor(spec.Colour).gamma;
                light.intensity = 0f;
                light.shadows = LightShadows.None;
                light.enabled = false;

                _lights.Add(new AnchoredLight(light, anchor.Kind, anchor.Seed, spec));
            }

            foreach (var material in NightLights.EmissiveMaterials)
            {
                materials.EnableEmission(material);
            }

            Apply(immediate: true);
        }

        private void Update()
        {
            if (_time != null)
            {
                var started = Stopwatch.GetTimestamp();
                Apply(immediate: !Application.isPlaying);
                _timing.Add((float)((Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency));
            }
        }

        private void Apply(bool immediate)
        {
            var hour = _time.CurrentHour;
            var darkness = _time.Current.Darkness;
            var time = Application.isPlaying ? Time.time : 0f;
            var step = Time.deltaTime * FadePerSecond;

            foreach (var material in NightLights.EmissiveMaterials)
            {
                _materials.SetEmission(material, ToColor(NightLights.Emission(material, hour, darkness, time)));
            }

            foreach (var light in _lights)
            {
                var target = NightLights.LightLevel(light.Kind, light.Seed, hour, darkness, time);

                // Lamps stutter as they come on.
                if (light.Kind == AnchorKind.StreetLamp || light.Kind == AnchorKind.DoorLamp)
                {
                    if (target > 0.5f && light.OnSince < 0f)
                    {
                        light.OnSince = time;
                    }
                    else if (target < 0.5f)
                    {
                        light.OnSince = -1f;
                    }

                    if (light.OnSince >= 0f && Application.isPlaying)
                    {
                        target *= LightSchedule.SwitchOnFlicker(time - light.OnSince, light.Seed);
                    }
                }

                // Beacons flash crisply; everything else fades.
                light.Level = immediate || light.Kind == AnchorKind.Beacon ? target : Mathf.MoveTowards(light.Level, target, step);
                light.Light.intensity = light.Spec.Intensity * light.Level;
                light.Light.enabled = light.Level > 0.01f;
            }
        }

        private static Color ToColor(Rgb colour) => new Color(colour.R, colour.G, colour.B, 1f);

        private sealed class AnchoredLight
        {
            public AnchoredLight(Light light, AnchorKind kind, int seed, LightSpec spec)
            {
                Light = light;
                Kind = kind;
                Seed = seed;
                Spec = spec;
            }

            public Light Light { get; }

            public AnchorKind Kind { get; }

            public int Seed { get; }

            public LightSpec Spec { get; }

            public float Level { get; set; }

            public float OnSince { get; set; } = -1f;
        }
    }
}
