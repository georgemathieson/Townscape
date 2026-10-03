using System;
using UnityEngine;

namespace Townscape.Runtime.Lighting
{
    /// <summary>
    /// How the scene is lit at one hour of the day. The single directional light is the sun by
    /// day and the moon by night; its direction is given as a compass bearing and an elevation.
    /// </summary>
    [Serializable]
    public struct LightingKeyframe
    {
        public float Hour;

        [Tooltip("Degrees above the horizon the light shines from.")]
        public float Elevation;

        [Tooltip("Compass bearing the light shines from, degrees clockwise from north (+Z).")]
        public float Azimuth;

        public Color LightColour;
        public float LightIntensity;

        [Range(0f, 1f)]
        public float ShadowStrength;

        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;

        [Tooltip("Fog colour, also used as the background so the sky and horizon match.")]
        public Color FogColour;
        public float FogDensity;

        [Tooltip("Colour of the reflected sky overhead (water and wet surfaces pick this up).")]
        public Color SkyColour;

        [Tooltip("Post exposure in EV, to keep night readable.")]
        public float Exposure;

        [Range(0f, 1f)]
        [Tooltip("How dark it feels, 0 for broad day to 1 for night. Drives street lamps and lit windows.")]
        public float Darkness;

        public static LightingKeyframe Lerp(in LightingKeyframe a, in LightingKeyframe b, float t)
        {
            return new LightingKeyframe
            {
                Hour = Mathf.Lerp(a.Hour, b.Hour, t),
                Elevation = Mathf.Lerp(a.Elevation, b.Elevation, t),
                Azimuth = Mathf.LerpAngle(a.Azimuth, b.Azimuth, t),
                LightColour = Color.Lerp(a.LightColour, b.LightColour, t),
                LightIntensity = Mathf.Lerp(a.LightIntensity, b.LightIntensity, t),
                ShadowStrength = Mathf.Lerp(a.ShadowStrength, b.ShadowStrength, t),
                AmbientSky = Color.Lerp(a.AmbientSky, b.AmbientSky, t),
                AmbientEquator = Color.Lerp(a.AmbientEquator, b.AmbientEquator, t),
                AmbientGround = Color.Lerp(a.AmbientGround, b.AmbientGround, t),
                FogColour = Color.Lerp(a.FogColour, b.FogColour, t),
                FogDensity = Mathf.Lerp(a.FogDensity, b.FogDensity, t),
                SkyColour = Color.Lerp(a.SkyColour, b.SkyColour, t),
                Exposure = Mathf.Lerp(a.Exposure, b.Exposure, t),
                Darkness = Mathf.Lerp(a.Darkness, b.Darkness, t),
            };
        }
    }
}
