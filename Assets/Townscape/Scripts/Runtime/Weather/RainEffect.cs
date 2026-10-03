using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;
using UnityEngine;
using Random = System.Random;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// Rain that follows the camera: streaks falling through a box above it, slanted by the wind.
    /// Each drop stops where the <see cref="RainCatchMap"/> says it first hits something (a roof,
    /// an awning, the road, the river) and splashes there.
    /// </summary>
    public sealed class RainEffect : IWeatherEffect
    {
        private const float BoxSize = 44f;
        private const float EmitHeight = 16f;
        private const int MaxDrops = 12000;
        private const int MaxSplashes = 3000;
        private const float SplashRadius = 24f;

        // Rain stays faintly visible on the darkest night, as it would in the glow of the village.
        private static readonly Color NightFloor = new Color(0.16f, 0.18f, 0.22f);

        private readonly RainCatchMap _catchMap;
        private readonly ParticleSystem _rain;
        private readonly ParticleSystem _splashes;
        private readonly Material _rainMaterial;
        private readonly Material _splashMaterial;
        private readonly ParticleSystem.Particle[] _drops = new ParticleSystem.Particle[MaxDrops];
        private readonly Random _random = new Random(19);

        public RainEffect(Transform parent, HideFlags hideFlags, WeatherTextures textures, RainCatchMap catchMap)
        {
            _catchMap = catchMap;
            _rainMaterial = EffectMaterials.CreateParticles("Rain", textures.RainStreak);
            _splashMaterial = EffectMaterials.CreateParticles("Splash", textures.Droplet);

            _rain = ParticleSystems.Create("Rain", parent, hideFlags, MaxDrops, _rainMaterial);
            var main = _rain.main;
            main.startLifetime = (EmitHeight + 6f) / RainFall.FallSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.022f);
            main.startColor = new Color(1f, 1f, 1f, 0.5f);

            var shape = _rain.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(BoxSize, 0.5f, BoxSize);

            var velocity = _rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;

            // Streaks are stretched along their velocity.
            var renderer = _rain.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.045f;
            renderer.lengthScale = 2f;

            _splashes = ParticleSystems.Create("Splashes", parent, hideFlags, MaxSplashes, _splashMaterial);
            var splashMain = _splashes.main;
            splashMain.gravityModifier = 1.2f;
            var size = _splashes.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            _rain.Play();
            _splashes.Play();
        }

        public void Tick(in WeatherFrame frame)
        {
            var rain = frame.Conditions.Rain;
            var fall = RainFall.Velocity(frame.Conditions.Wind);
            var wind = frame.Wind;

            // Emit upwind, so the drops arrive round the camera, and a little ahead of the view.
            var forward = frame.Camera.forward;
            forward.y = 0f;
            var upwind = wind * (-0.85f * EmitHeight / RainFall.FallSpeed);
            _rain.transform.position = frame.CameraPosition + (Vector3.up * EmitHeight) + upwind + (forward.normalized * 6f);

            var emission = _rain.emission;
            emission.rateOverTime = RainFall.DropsPerSecond(rain);

            var velocity = _rain.velocityOverLifetime;
            velocity.x = fall.X;
            velocity.y = fall.Y;
            velocity.z = fall.Z;

            var lit = frame.Ambient * 2.2f;
            var tint = new Color(Mathf.Max(lit.r, NightFloor.r), Mathf.Max(lit.g, NightFloor.g), Mathf.Max(lit.b, NightFloor.b), 1f);
            EffectMaterials.SetColour(_rainMaterial, tint);
            EffectMaterials.SetColour(_splashMaterial, tint);

            Land(frame.CameraPosition, RainFall.SplashChance(rain));
        }

        public void Dispose()
        {
            ObjectUtility.Destroy(_rain != null ? _rain.gameObject : null);
            ObjectUtility.Destroy(_splashes != null ? _splashes.gameObject : null);
            ObjectUtility.Destroy(_rainMaterial);
            ObjectUtility.Destroy(_splashMaterial);
        }

        // Ends every drop that has reached a surface, splashing the ones near the camera.
        private void Land(Vector3 camera, float splashChance)
        {
            var count = _rain.GetParticles(_drops);
            var landed = false;
            var splash = new ParticleSystem.EmitParams();
            for (var i = 0; i < count; i++)
            {
                var position = _drops[i].position;
                var surface = _catchMap.HeightAt(position.x, position.z);
                if (position.y > surface)
                {
                    continue;
                }

                _drops[i].remainingLifetime = 0f;
                landed = true;

                var dx = position.x - camera.x;
                var dz = position.z - camera.z;
                if ((dx * dx) + (dz * dz) > SplashRadius * SplashRadius || _random.NextDouble() > splashChance)
                {
                    continue;
                }

                splash.position = new Vector3(position.x, surface + 0.02f, position.z);
                splash.velocity = new Vector3(Signed() * 0.5f, 0.7f + (0.9f * (float)_random.NextDouble()), Signed() * 0.5f);
                splash.startSize = 0.03f + (0.025f * (float)_random.NextDouble());
                splash.startLifetime = 0.18f + (0.14f * (float)_random.NextDouble());
                splash.startColor = new Color(1f, 1f, 1f, 0.6f);
                _splashes.Emit(splash, 1);
            }

            if (landed)
            {
                _rain.SetParticles(_drops, count);
            }
        }

        private float Signed() => ((float)_random.NextDouble() * 2f) - 1f;
    }
}
