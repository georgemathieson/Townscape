using System;
using System.Collections.Generic;
using Townscape.Generation.Maths;
using Townscape.Simulation.Weather;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// The few textures the storm needs, drawn in code like everything else: a rain streak, a
    /// splash droplet, a soft puff for mist and smoke, and the ripple flipbooks for water.
    /// </summary>
    public sealed class WeatherTextures : IDisposable
    {
        private readonly List<Texture> _owned = new List<Texture>();

        public WeatherTextures()
        {
            RainStreak = Create("Rain Streak", 8, 64, (u, v) =>
            {
                var across = 1f - Mathf.Abs((u * 2f) - 1f);
                var along = Mathf.Sin(v * Mathf.PI);
                return across * across * along;
            });

            Droplet = Create("Droplet", 16, 16, (u, v) =>
            {
                var r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                return Mathf.Clamp01(1f - (r * r));
            });

            var noise = new GradientNoise(23);
            Puff = Create("Puff", 64, 64, (u, v) =>
            {
                var r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                var lumpy = r * (1f + (0.25f * noise.Fractal(u * 4f, v * 4f, 3)));
                var soft = Mathf.Clamp01(1f - lumpy);
                return soft * soft * (3f - (2f * soft));
            });

            PuddleRipples = Flipbook("Puddle Ripples", new RippleField(128, 24, 70, 0f, 31), 2.5f);
            WaterRipples = Flipbook("Water Ripples", new RippleField(128, 24, 45, 1f, 37), 2f);
        }

        public Texture2D RainStreak { get; }

        public Texture2D Droplet { get; }

        public Texture2D Puff { get; }

        /// <summary>Rings from raindrops, for puddles.</summary>
        public Texture2D[] PuddleRipples { get; }

        /// <summary>Rings from raindrops on a gentle swell, for the river and the lake.</summary>
        public Texture2D[] WaterRipples { get; }

        public void Dispose()
        {
            foreach (var texture in _owned)
            {
                if (texture != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(texture);
                    }
                    else
                    {
                        Object.DestroyImmediate(texture);
                    }
                }
            }

            _owned.Clear();
        }

        // White, with the shape in the alpha channel.
        private Texture2D Create(string name, int width, int height, Func<float, float, float> alpha)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = $"Townscape {name}",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var a = Mathf.Clamp01(alpha((x + 0.5f) / width, (y + 0.5f) / height));
                    pixels[(y * width) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            _owned.Add(texture);
            return texture;
        }

        private Texture2D[] Flipbook(string name, RippleField field, float strength)
        {
            var frames = new Texture2D[field.Frames];
            for (var i = 0; i < frames.Length; i++)
            {
                // Linear, not sRGB: these hold directions, not colours.
                var texture = new Texture2D(field.Size, field.Size, TextureFormat.RGBA32, true, true)
                {
                    name = $"Townscape {name} {i}",
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 4,
                };

                texture.SetPixelData(field.NormalMap(i, strength), 0);
                texture.Apply(true, true);
                _owned.Add(texture);
                frames[i] = texture;
            }

            return frames;
        }
    }
}
