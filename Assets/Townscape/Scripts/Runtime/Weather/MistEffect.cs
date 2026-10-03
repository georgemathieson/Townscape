using System;
using Townscape.Runtime.Rendering;
using UnityEngine;
using Random = System.Random;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// Low mist: big, faint, soft-edged puffs drifting with the wind a few metres above the
    /// ground round the camera. Thicker fog for the whole scene comes from <c>TimeOfDayLighting</c>.
    /// </summary>
    public sealed class MistEffect : IWeatherEffect
    {
        private const int MaxPuffs = 160;
        private const float Radius = 85f;

        private readonly Func<Vector2, float> _groundHeight;
        private readonly ParticleSystem _mist;
        private readonly Material _material;
        private readonly Random _random = new Random(29);

        public MistEffect(Transform parent, HideFlags hideFlags, WeatherTextures textures, Func<Vector2, float> groundHeight)
        {
            _groundHeight = groundHeight;
            _material = EffectMaterials.CreateParticles("Mist", textures.Puff, softDistance: 4f, cameraFade: new Vector2(3f, 12f));
            _mist = ParticleSystems.Create("Mist", parent, hideFlags, MaxPuffs, _material);
            ParticleSystems.FadeInAndOut(_mist, 0.25f, 0.3f);
            _mist.Play();
        }

        public void Tick(in WeatherFrame frame)
        {
            var mist = frame.Conditions.Mist;
            var wanted = Mathf.RoundToInt(Mathf.Lerp(30f, MaxPuffs, mist));
            var camera = frame.CameraPosition;
            var puff = new ParticleSystem.EmitParams();
            // Top up gradually, a few puffs a frame, so mist never pops in all at once.
            var missing = Mathf.Min(4, wanted - _mist.particleCount);
            for (var i = 0; i < missing; i++)
            {
                var angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                var distance = Radius * Mathf.Sqrt((float)_random.NextDouble());
                var flat = new Vector2(camera.x + (Mathf.Cos(angle) * distance), camera.z + (Mathf.Sin(angle) * distance));
                puff.position = new Vector3(flat.x, _groundHeight(flat) + 0.5f + (2.5f * (float)_random.NextDouble()), flat.y);
                puff.velocity = (frame.Wind * 0.25f) + (Vector3.up * 0.05f);
                puff.startSize = 12f + (12f * (float)_random.NextDouble());
                puff.startLifetime = 16f + (8f * (float)_random.NextDouble());
                puff.rotation = (float)_random.NextDouble() * 360f;
                _mist.Emit(puff, 1);
            }

            // Mist glows with whatever light there is, and flares with lightning.
            var tint = (frame.Ambient * 1.6f) + new Color(0.05f, 0.05f, 0.06f);
            tint.a = 0.05f + (0.09f * mist);
            EffectMaterials.SetColour(_material, tint);
        }

        public void Dispose()
        {
            ObjectUtility.Destroy(_mist != null ? _mist.gameObject : null);
            ObjectUtility.Destroy(_material);
        }
    }
}
