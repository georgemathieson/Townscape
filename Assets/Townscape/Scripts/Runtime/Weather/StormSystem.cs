using System;
using System.Collections.Generic;
using Townscape.Generation;
using Townscape.Runtime.Lighting;
using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;
using Townscape.State;
using UnityEngine;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// The storm. Reads the weather settings from the store, asks the weather profile what the
    /// weather is doing, runs the lightning, and ticks every effect with the result. Like the
    /// clock, its per-frame values (gusts, flashes, how wet things are) stay here and never go
    /// through the store.
    /// </summary>
    /// <remarks>
    /// Runs before other scripts so the lighting sees this frame's flash.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class StormSystem : MonoBehaviour
    {
        private readonly List<IWeatherEffect> _effects = new List<IWeatherEffect>();
        private readonly List<Strike> _newStrikes = new List<Strike>();
        private readonly List<Thunder> _thunder = new List<Thunder>();
        private readonly LightningStorm _storm = new LightningStorm();
        private IDisposable _subscription;
        private TimeOfDayLighting _lighting;
        private Transform _camera;
        private WeatherTextures _textures;
        private IWeatherProfile _profile = new ThunderstormProfile();
        private WeatherSettings _settings;
        private int _strikeRequests = -1;
        private bool _strikeRequested;

        /// <summary>A strike has just flashed.</summary>
        public event Action<Strike> Struck;

        /// <summary>
        /// Thunder from an earlier strike has just reached the camera. The audio system (a later
        /// milestone) plays a crack or a rumble from this, by distance and loudness.
        /// </summary>
        public event Action<Thunder> ThunderArrived;

        public WeatherConditions Conditions { get; private set; }

        /// <summary>How wet the town is, from 0 to 1.</summary>
        public float Wetness { get; private set; }

        public string ProfileName => _profile.Name;

        public void Initialize(Store<TownState> store, GeneratedTown town, MaterialLibrary materials, TimeOfDayLighting lighting, Transform camera, IReadOnlyList<SpawnedMesh> spawned, HideFlags hideFlags)
        {
            _lighting = lighting;
            _camera = camera;
            _textures = new WeatherTextures();

            var ground = town.Context.Ground;
            var terrain = town.Context.Terrain;
            var waterLevel = town.Context.Layout.WaterLevel;
            var core = town.Context.Settings.CoreHalfExtent;
            float GroundHeight(Vector2 p)
            {
                var flat = new System.Numerics.Vector2(p.x, p.y);
                var inCore = Mathf.Abs(p.x) < core && Mathf.Abs(p.y) < core;
                return Mathf.Max(inCore ? ground.HeightAt(flat) : terrain.FarHeightAt(flat), waterLevel);
            }

            _effects.Add(new WetSurfacesEffect(materials));
            _effects.Add(new WaterEffect(materials, _textures));
            _effects.Add(new RainEffect(transform, hideFlags, _textures, RainCatchMap.Build(town, core + 60f, 0.5f)));
            _effects.Add(new LightningEffect(transform, hideFlags, GroundHeight));
            _effects.Add(new MistEffect(transform, hideFlags, _textures, GroundHeight));
            _effects.Add(new ChimneySmokeEffect(transform, hideFlags, _textures, town.Anchors));
            _effects.Add(new WindSwayEffect(spawned));

            _subscription = store.Subscribe(state => state.Weather, OnWeatherChanged);

            // It has been raining for a while before the scene opens.
            Wetness = Simulation.Weather.Wetness.Step(0f, _settings.Rain, 1000f);
        }

        private void OnWeatherChanged(WeatherState weather)
        {
            _settings = new WeatherSettings(weather.RainIntensity, weather.LightningFrequency, weather.WindStrength);
            _profile = ProfileFor(weather.Kind);

            // The first call only records the count; later increases are requests to strike.
            if (_strikeRequests >= 0 && weather.StrikeRequests != _strikeRequests)
            {
                _strikeRequested = true;
            }

            _strikeRequests = weather.StrikeRequests;
        }

        private IWeatherProfile ProfileFor(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Storm:
                default:
                    return _profile as ThunderstormProfile ?? new ThunderstormProfile();
            }
        }

        private void Update()
        {
            if (_camera == null || _lighting == null)
            {
                return;
            }

            var time = Time.time;
            var deltaTime = Time.deltaTime;
            Conditions = _profile.Sample(_settings, time);
            Wetness = Simulation.Weather.Wetness.Step(Wetness, Conditions.Rain, deltaTime);

            var position = _camera.position;
            var forward = _camera.forward;
            var listener = new System.Numerics.Vector2(position.x, position.z);
            var facing = new System.Numerics.Vector2(forward.x, forward.z);
            _newStrikes.Clear();
            _thunder.Clear();
            if (_strikeRequested)
            {
                _newStrikes.Add(_storm.StrikeNow(time, listener, facing));
                _strikeRequested = false;
            }

            _storm.Step(time, deltaTime, Conditions.StrikesPerMinute, listener, facing, _newStrikes, _thunder);
            var flash = _storm.Flash(time);
            _lighting.Weather = new WeatherLighting(flash, 0.9f + (0.3f * Conditions.Mist));

            var key = _lighting.Current;
            var ambient = key.AmbientEquator + (key.LightColour * (key.LightIntensity * 0.2f)) + (WeatherLighting.FlashColour * (flash * 1.2f));
            var frame = new WeatherFrame(Conditions, time, deltaTime, _lighting.CurrentHour, Wetness, flash, ambient, _camera, _newStrikes);
            foreach (var effect in _effects)
            {
                effect.Tick(frame);
            }

            foreach (var strike in _newStrikes)
            {
                Struck?.Invoke(strike);
            }

            foreach (var thunder in _thunder)
            {
                ThunderArrived?.Invoke(thunder);
            }
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            foreach (var effect in _effects)
            {
                effect.Dispose();
            }

            _effects.Clear();
            _textures?.Dispose();
            if (_lighting != null)
            {
                _lighting.Weather = WeatherLighting.None;
            }
        }
    }
}
