using System;
using System.Collections.Generic;
using Townscape.State;
using UnityEngine;

namespace Townscape.Runtime.Lighting
{
    /// <summary>A day's worth of <see cref="LightingKeyframe"/>s, blended by hour.</summary>
    public sealed class LightingProfile
    {
        private readonly LightingKeyframe[] _keyframes;

        public LightingProfile(IEnumerable<LightingKeyframe> keyframes)
        {
            _keyframes = new List<LightingKeyframe>(keyframes).ToArray();
            if (_keyframes.Length == 0)
            {
                throw new ArgumentException("A lighting profile needs at least one keyframe.", nameof(keyframes));
            }

            Array.Sort(_keyframes, (a, b) => a.Hour.CompareTo(b.Hour));
        }

        /// <summary>The look for an hour, blending between the keyframes either side (wrapping at midnight).</summary>
        public LightingKeyframe Evaluate(float hour)
        {
            hour = TimeMath.WrapHour(hour);
            var count = _keyframes.Length;
            for (var i = 0; i < count; i++)
            {
                var from = _keyframes[i];
                var to = _keyframes[(i + 1) % count];
                var span = TimeMath.WrapHour(to.Hour - from.Hour);
                if (span <= 0f)
                {
                    span = TimeMath.HoursPerDay;
                }

                var into = TimeMath.WrapHour(hour - from.Hour);
                if (into <= span)
                {
                    return LightingKeyframe.Lerp(from, to, Mathf.SmoothStep(0f, 1f, into / span));
                }
            }

            return _keyframes[0];
        }

        /// <summary>
        /// The stormy Lake District day: heavy overcast, a warm break under the clouds at dawn and
        /// dusk, and a cold blue night. Colours are sRGB.
        /// </summary>
        public static LightingProfile CreateStorm()
        {
            return new LightingProfile(new[]
            {
                Key(0f, elevation: 42f, azimuth: 190f, light: Rgb(0.52f, 0.62f, 0.90f), intensity: 0.22f, shadows: 0.55f,
                    sky: Rgb(0.07f, 0.09f, 0.16f), equator: Rgb(0.05f, 0.06f, 0.10f), ground: Rgb(0.02f, 0.02f, 0.03f),
                    fog: Rgb(0.05f, 0.06f, 0.10f), density: 0.011f, skyColour: Rgb(0.07f, 0.09f, 0.15f), exposure: 0.7f, darkness: 1f),
                Key(4.75f, elevation: 18f, azimuth: 245f, light: Rgb(0.50f, 0.58f, 0.85f), intensity: 0.14f, shadows: 0.4f,
                    sky: Rgb(0.09f, 0.10f, 0.18f), equator: Rgb(0.07f, 0.07f, 0.12f), ground: Rgb(0.02f, 0.02f, 0.03f),
                    fog: Rgb(0.08f, 0.09f, 0.14f), density: 0.012f, skyColour: Rgb(0.09f, 0.10f, 0.17f), exposure: 0.6f, darkness: 0.95f),
                Key(6.25f, elevation: 4f, azimuth: 82f, light: Rgb(1.00f, 0.64f, 0.46f), intensity: 0.85f, shadows: 0.6f,
                    sky: Rgb(0.36f, 0.35f, 0.46f), equator: Rgb(0.42f, 0.33f, 0.32f), ground: Rgb(0.10f, 0.09f, 0.08f),
                    fog: Rgb(0.42f, 0.37f, 0.42f), density: 0.0105f, skyColour: Rgb(0.45f, 0.42f, 0.52f), exposure: 0.35f, darkness: 0.55f),
                Key(9f, elevation: 24f, azimuth: 120f, light: Rgb(0.92f, 0.90f, 0.86f), intensity: 1.1f, shadows: 0.55f,
                    sky: Rgb(0.50f, 0.55f, 0.61f), equator: Rgb(0.42f, 0.45f, 0.48f), ground: Rgb(0.15f, 0.14f, 0.12f),
                    fog: Rgb(0.52f, 0.56f, 0.60f), density: 0.0085f, skyColour: Rgb(0.58f, 0.62f, 0.67f), exposure: 0f, darkness: 0.12f),
                Key(12.5f, elevation: 38f, azimuth: 175f, light: Rgb(0.90f, 0.92f, 0.95f), intensity: 1.25f, shadows: 0.5f,
                    sky: Rgb(0.55f, 0.60f, 0.66f), equator: Rgb(0.47f, 0.50f, 0.53f), ground: Rgb(0.17f, 0.16f, 0.14f),
                    fog: Rgb(0.56f, 0.60f, 0.64f), density: 0.0072f, skyColour: Rgb(0.62f, 0.66f, 0.71f), exposure: 0f, darkness: 0.08f),
                Key(16.5f, elevation: 20f, azimuth: 235f, light: Rgb(0.95f, 0.85f, 0.72f), intensity: 1.05f, shadows: 0.55f,
                    sky: Rgb(0.48f, 0.50f, 0.57f), equator: Rgb(0.43f, 0.42f, 0.45f), ground: Rgb(0.14f, 0.13f, 0.11f),
                    fog: Rgb(0.50f, 0.51f, 0.56f), density: 0.008f, skyColour: Rgb(0.56f, 0.57f, 0.63f), exposure: 0.05f, darkness: 0.18f),
                Key(19.25f, elevation: 3f, azimuth: 288f, light: Rgb(1.00f, 0.50f, 0.30f), intensity: 1.1f, shadows: 0.65f,
                    sky: Rgb(0.26f, 0.27f, 0.40f), equator: Rgb(0.38f, 0.28f, 0.30f), ground: Rgb(0.08f, 0.07f, 0.07f),
                    fog: Rgb(0.30f, 0.27f, 0.34f), density: 0.0095f, skyColour: Rgb(0.34f, 0.30f, 0.42f), exposure: 0.3f, darkness: 0.55f),
                Key(20.5f, elevation: 10f, azimuth: 250f, light: Rgb(0.45f, 0.50f, 0.78f), intensity: 0.08f, shadows: 0.3f,
                    sky: Rgb(0.13f, 0.15f, 0.27f), equator: Rgb(0.11f, 0.11f, 0.18f), ground: Rgb(0.03f, 0.03f, 0.04f),
                    fog: Rgb(0.11f, 0.12f, 0.20f), density: 0.0105f, skyColour: Rgb(0.13f, 0.15f, 0.26f), exposure: 0.5f, darkness: 0.9f),
                Key(23f, elevation: 38f, azimuth: 170f, light: Rgb(0.52f, 0.62f, 0.90f), intensity: 0.2f, shadows: 0.55f,
                    sky: Rgb(0.07f, 0.09f, 0.16f), equator: Rgb(0.05f, 0.06f, 0.10f), ground: Rgb(0.02f, 0.02f, 0.03f),
                    fog: Rgb(0.06f, 0.07f, 0.11f), density: 0.011f, skyColour: Rgb(0.07f, 0.09f, 0.15f), exposure: 0.7f, darkness: 1f),
            });
        }

        private static Color Rgb(float r, float g, float b) => new Color(r, g, b, 1f);

        private static LightingKeyframe Key(
            float hour,
            float elevation,
            float azimuth,
            Color light,
            float intensity,
            float shadows,
            Color sky,
            Color equator,
            Color ground,
            Color fog,
            float density,
            Color skyColour,
            float exposure,
            float darkness)
        {
            return new LightingKeyframe
            {
                Hour = hour,
                Elevation = elevation,
                Azimuth = azimuth,
                LightColour = light,
                LightIntensity = intensity,
                ShadowStrength = shadows,
                AmbientSky = sky,
                AmbientEquator = equator,
                AmbientGround = ground,
                FogColour = fog,
                FogDensity = density,
                SkyColour = skyColour,
                Exposure = exposure,
                Darkness = darkness,
            };
        }
    }
}
