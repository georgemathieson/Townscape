using UnityEngine;

namespace Townscape.Runtime.Lighting
{
    /// <summary>
    /// How the weather changes the time of day's light this frame: the cold flash of lightning
    /// on the sky, fog and ambient light, thicker fog in mist and snow, fog paled by falling snow,
    /// and light thrown back up from snow lying on the ground.
    /// </summary>
    public readonly struct WeatherLighting
    {
        public static readonly WeatherLighting None = new WeatherLighting(0f, 1f);

        /// <summary>The colour lightning washes the sky and the shadows with.</summary>
        public static readonly Color FlashColour = new Color(0.62f, 0.68f, 0.92f);

        /// <summary>The grey-white falling snow turns the fog, at full daylight.</summary>
        public static readonly Color SnowFogColour = new Color(0.86f, 0.89f, 0.93f);

        public WeatherLighting(float flash, float fogScale, float whiteness = 0f, float snowCover = 0f)
        {
            Flash = flash;
            FogScale = fogScale;
            Whiteness = whiteness;
            SnowCover = snowCover;
        }

        /// <summary>Lightning brightness, from 0 to 1.</summary>
        public float Flash { get; }

        /// <summary>Multiplies the fog density.</summary>
        public float FogScale { get; }

        /// <summary>How far the fog pales towards the colour of falling snow, from 0 to 1.</summary>
        public float Whiteness { get; }

        /// <summary>How much snow is lying, from 0 to 1.</summary>
        public float SnowCover { get; }

        public LightingKeyframe ApplyTo(LightingKeyframe key)
        {
            // Falling snow scatters whatever light there is, so the fog pales to grey-white but
            // stays about as bright as the time of day makes it: pale by day, dim at night.
            var fog = key.FogColour;
            var brightness = Mathf.Clamp01((Mathf.Max(fog.r, Mathf.Max(fog.g, fog.b)) * 1.25f) + 0.02f);
            key.FogColour = Color.Lerp(fog, SnowFogColour * brightness, Whiteness);

            // Lying snow throws the sky's light back up into the shadows.
            key.AmbientGround = Color.Lerp(key.AmbientGround, key.AmbientSky * 0.9f, SnowCover * 0.6f);

            // At its peak a close strike lifts a night scene to about dusk, not to broad day.
            key.AmbientSky += FlashColour * (Flash * 0.5f);
            key.AmbientEquator += FlashColour * (Flash * 0.35f);
            key.AmbientGround += FlashColour * (Flash * 0.12f);
            key.FogColour = Color.Lerp(key.FogColour, FlashColour * 0.75f, Flash * 0.4f);
            key.FogDensity *= FogScale;
            return key;
        }
    }
}
