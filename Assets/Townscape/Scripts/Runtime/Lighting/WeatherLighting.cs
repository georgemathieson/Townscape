using UnityEngine;

namespace Townscape.Runtime.Lighting
{
    /// <summary>
    /// How the weather changes the time of day's light this frame: the cold flash of lightning
    /// on the sky, fog and ambient light, and thicker fog in the mist.
    /// </summary>
    public readonly struct WeatherLighting
    {
        public static readonly WeatherLighting None = new WeatherLighting(0f, 1f);

        /// <summary>The colour lightning washes the sky and the shadows with.</summary>
        public static readonly Color FlashColour = new Color(0.62f, 0.68f, 0.92f);

        public WeatherLighting(float flash, float fogScale)
        {
            Flash = flash;
            FogScale = fogScale;
        }

        /// <summary>Lightning brightness, from 0 to 1.</summary>
        public float Flash { get; }

        /// <summary>Multiplies the fog density.</summary>
        public float FogScale { get; }

        public LightingKeyframe ApplyTo(LightingKeyframe key)
        {
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
