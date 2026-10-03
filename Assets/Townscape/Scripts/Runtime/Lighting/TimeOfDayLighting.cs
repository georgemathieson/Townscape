using System;
using Townscape.State;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Townscape.Runtime.Lighting
{
    /// <summary>
    /// Owns the per-frame clock. It reads the requested time from the store and either eases
    /// towards it (preset or slider) or runs forward (auto-cycle), then lights the scene from the
    /// <see cref="LightingProfile"/>. Nothing here is dispatched back to the store each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimeOfDayLighting : MonoBehaviour
    {
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Seconds taken to blend to a newly chosen time.")]
        private float transitionSeconds = 4f;

        private IDisposable _subscription;
        private LightingProfile _profile;
        private Light _light;
        private bool _controlEnvironment;
        private Camera _camera;
        private ColorAdjustments _colorAdjustments;
        private SkyReflection _sky;
        private TimeOfDayState _state;
        private float _transitionFrom;
        private float _transitionDelta;
        private float _transitionElapsed;
        private bool _transitioning;

        /// <summary>The hour currently shown, which may lag the requested hour while blending.</summary>
        public float CurrentHour { get; private set; }

        /// <summary>The time of day's lighting before the weather changes it, so lamps don't react to lightning.</summary>
        public LightingKeyframe Current { get; private set; }

        /// <summary>Set each frame by the storm: the lightning flash and the mist's extra fog.</summary>
        public WeatherLighting Weather { get; set; } = WeatherLighting.None;

        /// <param name="controlEnvironment">
        /// When false (edit-mode preview) only the light is driven, so opening the scene does not
        /// modify and dirty its saved render settings.
        /// </param>
        public void Initialize(Store<TownState> store, Light light, LightingProfile profile, bool controlEnvironment)
        {
            _light = light;
            _profile = profile;
            _controlEnvironment = controlEnvironment;
            if (controlEnvironment)
            {
                _sky = new SkyReflection();
            }

            _subscription = store.Subscribe(state => state.TimeOfDay, OnTimeOfDayChanged);
        }

        public void AttachCamera(Camera targetCamera, ColorAdjustments colorAdjustments)
        {
            _camera = targetCamera;
            _colorAdjustments = colorAdjustments;
            Apply();
        }

        private void OnTimeOfDayChanged(TimeOfDayState state)
        {
            var first = _state == null;
            _state = state;
            if (first)
            {
                CurrentHour = state.TargetHour;
                _transitioning = false;
                Apply();
                return;
            }

            if (state.AutoCycle)
            {
                _transitioning = false;
                return;
            }

            _transitionFrom = CurrentHour;
            _transitionDelta = TimeMath.ShortestDelta(CurrentHour, state.TargetHour);
            _transitionElapsed = 0f;
            _transitioning = Mathf.Abs(_transitionDelta) > 1e-4f;
        }

        private void Update()
        {
            if (_state == null)
            {
                return;
            }

            var deltaTime = Application.isPlaying ? Time.deltaTime : 0f;
            if (_state.AutoCycle)
            {
                CurrentHour = TimeMath.WrapHour(CurrentHour + (deltaTime * _state.CycleHoursPerMinute / 60f));
            }
            else if (_transitioning)
            {
                _transitionElapsed += deltaTime;
                var t = Mathf.Clamp01(_transitionElapsed / transitionSeconds);
                CurrentHour = TimeMath.WrapHour(_transitionFrom + (_transitionDelta * Mathf.SmoothStep(0f, 1f, t)));
                _transitioning = t < 1f;
            }

            Apply();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _sky?.Dispose();
        }

        private void Apply()
        {
            if (_profile == null || _light == null)
            {
                return;
            }

            var key = _profile.Evaluate(CurrentHour);
            Current = key;

            _light.transform.rotation = Quaternion.Euler(key.Elevation, key.Azimuth + 180f, 0f);
            _light.color = key.LightColour;
            _light.intensity = key.LightIntensity;
            _light.shadowStrength = key.ShadowStrength;

            if (!_controlEnvironment)
            {
                return;
            }

            var shown = Weather.ApplyTo(key);
            RenderSettings.sun = _light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = shown.AmbientSky;
            RenderSettings.ambientEquatorColor = shown.AmbientEquator;
            RenderSettings.ambientGroundColor = shown.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = shown.FogColour;
            RenderSettings.fogDensity = shown.FogDensity;

            if (_camera != null)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = shown.FogColour;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.Override(key.Exposure);
            }

            _sky?.Update(key, Time.unscaledTime);
        }
    }
}
