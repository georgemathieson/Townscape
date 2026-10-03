using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Townscape.Runtime.UI
{
    /// <summary>
    /// The control panel's look: a dark, slightly see-through card with rounded corners, soft
    /// buttons and slim sliders, all drawn into small textures in code.
    /// </summary>
    internal sealed class PanelSkin : IDisposable
    {
        private static readonly Color Card = new Color(0.07f, 0.08f, 0.1f, 0.88f);
        private static readonly Color ButtonColour = new Color(0.2f, 0.22f, 0.26f, 1f);
        private static readonly Color ButtonHover = new Color(0.27f, 0.29f, 0.34f, 1f);
        private static readonly Color ButtonPressed = new Color(0.15f, 0.16f, 0.19f, 1f);
        private static readonly Color Accent = new Color(0.93f, 0.66f, 0.3f, 1f);
        private static readonly Color AccentHover = new Color(1f, 0.74f, 0.4f, 1f);
        private static readonly Color Track = new Color(0.32f, 0.34f, 0.39f, 1f);
        private static readonly Color Text = new Color(0.9f, 0.9f, 0.88f, 1f);
        private static readonly Color Quiet = new Color(0.62f, 0.64f, 0.68f, 1f);
        private static readonly Color Dark = new Color(0.1f, 0.08f, 0.05f, 1f);

        private readonly List<Texture2D> _textures = new List<Texture2D>();

        public PanelSkin()
        {
            var card = Rounded(32, 32, 12, Card);
            Panel = new GUIStyle
            {
                normal = { background = card },
                border = new RectOffset(12, 12, 12, 12),
                padding = new RectOffset(16, 16, 14, 14),
            };

            Title = new GUIStyle { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = Text } };
            Clock = new GUIStyle(Title) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Normal };
            Status = new GUIStyle { fontSize = 12, wordWrap = true, normal = { textColor = Quiet }, margin = new RectOffset(0, 0, 2, 4) };
            Heading = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, normal = { textColor = Quiet }, margin = new RectOffset(0, 0, 12, 6) };
            Label = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = Text }, fixedHeight = 22 };
            Value = new GUIStyle(Label) { alignment = TextAnchor.MiddleRight, normal = { textColor = Quiet } };
            Keys = new GUIStyle { fontSize = 12, richText = true, normal = { textColor = Quiet }, margin = new RectOffset(0, 0, 6, 0) };

            Button = new GUIStyle
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 28,
                border = new RectOffset(6, 6, 6, 6),
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 4, 4),
                normal = { background = Rounded(16, 16, 6, ButtonColour), textColor = Text },
                hover = { background = Rounded(16, 16, 6, ButtonHover), textColor = Text },
                active = { background = Rounded(16, 16, 6, ButtonPressed), textColor = Text },
                onNormal = { background = Rounded(16, 16, 6, Accent), textColor = Dark },
                onHover = { background = Rounded(16, 16, 6, AccentHover), textColor = Dark },
                onActive = { background = Rounded(16, 16, 6, Accent), textColor = Dark },
            };

            Slider = new GUIStyle
            {
                normal = { background = TrackTexture() },
                border = new RectOffset(4, 4, 0, 0),
                fixedHeight = 16,
                margin = new RectOffset(4, 4, 3, 3),
            };

            var thumb = Disc(16, Text);
            Thumb = new GUIStyle
            {
                normal = { background = thumb },
                hover = { background = Disc(16, Color.white) },
                active = { background = Disc(16, Accent) },
                fixedWidth = 16,
                fixedHeight = 16,
            };
        }

        public GUIStyle Panel { get; }

        public GUIStyle Title { get; }

        public GUIStyle Clock { get; }

        public GUIStyle Status { get; }

        public GUIStyle Heading { get; }

        public GUIStyle Label { get; }

        public GUIStyle Value { get; }

        public GUIStyle Keys { get; }

        /// <summary>A button; as a toggle, "on" is drawn in the accent colour.</summary>
        public GUIStyle Button { get; }

        public GUIStyle Slider { get; }

        public GUIStyle Thumb { get; }

        public void Dispose()
        {
            foreach (var texture in _textures)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }

            _textures.Clear();
        }

        // A filled rectangle with rounded, anti-aliased corners, for nine-slicing.
        private Texture2D Rounded(int width, int height, int radius, Color colour)
        {
            return Draw(width, height, (x, y) =>
            {
                var cx = Mathf.Clamp(x, radius, width - radius);
                var cy = Mathf.Clamp(y, radius, height - radius);
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                return Mathf.Clamp01(radius - distance + 0.5f);
            }, colour);
        }

        private Texture2D Disc(int size, Color colour)
        {
            var radius = size * 0.5f;
            return Draw(size, size, (x, y) => Mathf.Clamp01(radius - 1f - Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) + 0.5f), colour);
        }

        // A slim bar across the middle of the slider's height.
        private Texture2D TrackTexture()
        {
            return Draw(8, 16, (x, y) =>
            {
                var fromCentre = Mathf.Abs(y - 8f);
                var edge = Mathf.Min(x, 8f - x);
                return Mathf.Clamp01(2.5f - fromCentre) * Mathf.Clamp01(edge + 0.5f);
            }, Track);
        }

        private Texture2D Draw(int width, int height, Func<float, float, float> coverage, Color colour)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var c = colour;
                    c.a *= coverage(x + 0.5f, y + 0.5f);
                    pixels[(y * width) + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _textures.Add(texture);
            return texture;
        }
    }
}
