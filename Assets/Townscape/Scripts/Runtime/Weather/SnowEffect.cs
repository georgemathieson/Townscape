using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;
using UnityEngine;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// Snow that follows the camera: soft flakes drifting down at about a metre a second, carried
    /// along by the wind and fluttering as they go. The wind carries a flake much further than a
    /// raindrop, so flakes are born all through a box of air round the camera, not just at its top,
    /// and fade in and out over their short lives. Flakes beyond the box are left to the fog, which
    /// thickens and pales in the snow. A flake that reaches whatever the <see cref="RainCatchMap"/>
    /// says it would land on (a roof, the ground, the river) is gone.
    /// </summary>
    public sealed class SnowEffect : IWeatherEffect
    {
        private const float BoxSize = 26f;
        private const float BoxHeight = 12f;
        private const float Lifetime = 3f;
        private const int MaxFlakes = 24000;

        // Snow stays visible on the darkest night, lit by the village.
        private static readonly Color NightFloor = new Color(0.3f, 0.32f, 0.38f);

        private readonly RainCatchMap _catchMap;
        private readonly ParticleSystem _snow;
        private readonly Material _material;
        private readonly ParticleSystem.Particle[] _flakes = new ParticleSystem.Particle[MaxFlakes];

        public SnowEffect(Transform parent, HideFlags hideFlags, WeatherTextures textures, RainCatchMap catchMap)
        {
            _catchMap = catchMap;
            _material = EffectMaterials.CreateParticles("Snow", textures.Flake, softDistance: 0.5f, cameraFade: new Vector2(0.3f, 1.2f));
            _snow = ParticleSystems.Create("Snow", parent, hideFlags, MaxFlakes, _material);

            var main = _snow.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(Lifetime * 0.7f, Lifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.08f);

            var shape = _snow.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(BoxSize, BoxHeight, BoxSize);

            var velocity = _snow.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;

            // Flakes flutter and swirl on the eddies.
            var noise = _snow.noise;
            noise.enabled = true;
            noise.frequency = 0.6f;
            noise.scrollSpeed = 0.4f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;

            ParticleSystems.FadeInAndOut(_snow, 0.15f, 0.2f);
            _snow.Play();
        }

        public string Name => "Snow";

        public void Tick(in WeatherFrame frame)
        {
            var snow = frame.Conditions.Snow;
            var fall = SnowFall.Velocity(frame.Conditions.Wind);

            // Centre the box a little ahead of the camera and set it upwind by half a flake's life,
            // so the flakes are round the camera on average however hard the wind blows.
            var forward = frame.Camera.forward;
            forward.y = 0f;
            var drift = new Vector3(fall.X, 0f, fall.Z) * (Lifetime * 0.5f);
            _snow.transform.position = frame.CameraPosition + (Vector3.up * ((BoxHeight * 0.5f) - 3f)) + (forward.normalized * 4f) - drift;

            // In the wind the flakes smear downwind over their lives, so more are needed to keep the
            // air round the camera as full.
            var smear = 1f + (2f * drift.magnitude / BoxSize);
            var flakes = SnowFall.FlakesPerCubicMetre(snow) * BoxSize * BoxSize * BoxHeight * smear;
            var emission = _snow.emission;
            emission.rateOverTime = Mathf.Min(flakes, MaxFlakes) / (Lifetime * 0.85f);

            var velocity = _snow.velocityOverLifetime;
            velocity.x = fall.X;
            velocity.y = fall.Y;
            velocity.z = fall.Z;

            var noise = _snow.noise;
            noise.strength = SnowFall.Flutter(frame.Conditions.WindSpeed);

            var lit = frame.Ambient * 3f;
            var tint = new Color(Mathf.Max(lit.r, NightFloor.r), Mathf.Max(lit.g, NightFloor.g), Mathf.Max(lit.b, NightFloor.b), 0.9f);
            EffectMaterials.SetColour(_material, tint);

            Land();
        }

        public void Dispose()
        {
            ObjectUtility.Destroy(_snow != null ? _snow.gameObject : null);
            ObjectUtility.Destroy(_material);
        }

        // Ends every flake that has reached a surface, and any born inside a building.
        private void Land()
        {
            var count = _snow.GetParticles(_flakes);
            var landed = false;
            for (var i = 0; i < count; i++)
            {
                var position = _flakes[i].position;
                if (position.y <= _catchMap.HeightAt(position.x, position.z))
                {
                    _flakes[i].remainingLifetime = 0f;
                    landed = true;
                }
            }

            if (landed)
            {
                _snow.SetParticles(_flakes, count);
            }
        }
    }
}
