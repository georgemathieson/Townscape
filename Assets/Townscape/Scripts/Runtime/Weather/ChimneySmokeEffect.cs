using System.Collections.Generic;
using System.Linq;
using Townscape.Generation;
using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;
using UnityEngine;
using Random = System.Random;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// Wood smoke from the chimneys with a fire lit (more in the evening), billowing up and
    /// streaming away downwind.
    /// </summary>
    public sealed class ChimneySmokeEffect : IWeatherEffect
    {
        private const int MaxPuffs = 1800;
        private const float Range = 170f;
        private const float RefreshSeconds = 4f;

        private readonly TownAnchor[] _chimneys;
        private readonly float[] _due;
        private readonly List<int> _burning = new List<int>();
        private readonly ParticleSystem _smoke;
        private readonly Material _material;
        private readonly Random _random = new Random(41);
        private float _nextRefresh;

        public ChimneySmokeEffect(Transform parent, HideFlags hideFlags, WeatherTextures textures, IEnumerable<TownAnchor> anchors)
        {
            _chimneys = anchors.Where(a => a.Kind == AnchorKind.Chimney).ToArray();
            _due = new float[_chimneys.Length];
            _material = EffectMaterials.CreateParticles("Smoke", textures.Puff, softDistance: 1f);
            _smoke = ParticleSystems.Create("Chimney Smoke", parent, hideFlags, MaxPuffs, _material);
            ParticleSystems.FadeInAndOut(_smoke, 0.08f, 0.6f);

            var size = _smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1f));

            var force = _smoke.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;

            // Hot smoke shoots up and slows as it cools, while the wind keeps pushing it over: a curved plume.
            var rise = _smoke.velocityOverLifetime;
            rise.enabled = true;
            rise.space = ParticleSystemSimulationSpace.World;
            rise.x = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 0f));
            rise.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1.3f, 1f, 0.15f));
            rise.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 0f));

            var noise = _smoke.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.3f;

            _smoke.Play();
        }

        public string Name => "Smoke";

        public void Tick(in WeatherFrame frame)
        {
            if (frame.Time >= _nextRefresh)
            {
                Refresh(frame);
                _nextRefresh = frame.Time + RefreshSeconds;
            }

            // The wind carries the smoke off and keeps nudging it along.
            var force = _smoke.forceOverLifetime;
            force.x = frame.Wind.x * 0.05f;
            force.y = 0f;
            force.z = frame.Wind.z * 0.05f;

            var interval = 1f / ChimneySmoke.PuffsPerSecond(frame.Conditions.WindSpeed);
            var puff = new ParticleSystem.EmitParams();
            foreach (var index in _burning)
            {
                if (frame.Time < _due[index])
                {
                    continue;
                }

                _due[index] = frame.Time + (interval * (0.7f + (0.6f * (float)_random.NextDouble())));
                var top = _chimneys[index].Position;
                puff.position = new Vector3(top.X, top.Y + 0.1f, top.Z);
                puff.velocity = frame.Wind * 0.4f;
                puff.startSize = 2.6f + (0.8f * (float)_random.NextDouble());
                puff.startLifetime = 5.5f + (2.5f * (float)_random.NextDouble());
                puff.rotation = (float)_random.NextDouble() * 360f;
                _smoke.Emit(puff, 1);
            }

            var tint = (frame.Ambient * 1.4f) + new Color(0.06f, 0.06f, 0.065f);
            tint.a = 0.32f;
            EffectMaterials.SetColour(_material, tint);
        }

        public void Dispose()
        {
            ObjectUtility.Destroy(_smoke != null ? _smoke.gameObject : null);
            ObjectUtility.Destroy(_material);
        }

        // Which chimneys near the camera have a fire in, at this time of day.
        private void Refresh(in WeatherFrame frame)
        {
            _burning.Clear();
            var camera = frame.CameraPosition;
            for (var i = 0; i < _chimneys.Length; i++)
            {
                var top = _chimneys[i].Position;
                var dx = top.X - camera.x;
                var dz = top.Z - camera.z;
                if ((dx * dx) + (dz * dz) < Range * Range && ChimneySmoke.IsBurning(_chimneys[i].Seed, frame.Hour, frame.SnowCover))
                {
                    _burning.Add(i);
                }
            }
        }
    }
}
