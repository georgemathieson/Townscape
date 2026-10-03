using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Townscape.Runtime.UI
{
    /// <summary>
    /// The Fellside Herald's look: black ink on warm newsprint, in a serif borrowed from the
    /// operating system (Georgia or Times on Windows and macOS), with ruled sections, boxed adverts
    /// and ink-outlined buttons. Everything is made in code; nothing is loaded from assets.
    /// </summary>
    internal sealed class PaperSkin : IDisposable
    {
        public static readonly Color Paper = new Color(0.95f, 0.93f, 0.87f, 1f);
        public static readonly Color Ink = new Color(0.12f, 0.11f, 0.09f, 1f);

        private static readonly string[] SerifFonts = { "Georgia", "Times New Roman", "Times", "Liberation Serif", "DejaVu Serif", "Noto Serif" };
        private static readonly Color Muted = new Color(0.37f, 0.34f, 0.29f, 1f);
        private static readonly Color Faint = new Color(0.87f, 0.84f, 0.76f, 1f);
        private static readonly Color Pressed = new Color(0.78f, 0.74f, 0.65f, 1f);
        private static readonly Color Red = new Color(0.62f, 0.16f, 0.13f, 1f);
        private static readonly Color RedHover = new Color(0.72f, 0.2f, 0.16f, 1f);
        private static readonly Color Green = new Color(0.17f, 0.4f, 0.19f, 1f);

        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly Font _serif;

        public PaperSkin()
        {
            _serif = Font.CreateDynamicFontFromOSFont(SerifFonts, 16);
            _serif.hideFlags = HideFlags.DontSave;

            PaperTexture = Solid(Paper);
            InkTexture = Solid(Ink);

            Masthead = Text(54, FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
            Strap = Text(12, FontStyle.Italic, Muted, TextAnchor.MiddleCenter);
            StrapLeft = new GUIStyle(Strap) { alignment = TextAnchor.MiddleLeft };
            StrapRight = new GUIStyle(Strap) { alignment = TextAnchor.MiddleRight };

            Headline = Wrapped(32, FontStyle.Bold, Ink, margin: new RectOffset(0, 0, 0, 8));
            Standfirst = Wrapped(16, FontStyle.Italic, Ink, margin: new RectOffset(0, 0, 0, 6));
            Byline = Wrapped(11, FontStyle.Italic, Muted, margin: new RectOffset(0, 0, 0, 8));
            Body = Wrapped(14, FontStyle.Normal, Ink, margin: new RectOffset(0, 0, 0, 8));
            Small = Wrapped(12, FontStyle.Normal, Ink, margin: new RectOffset(0, 0, 1, 3));
            Quiet = Wrapped(12, FontStyle.Italic, Muted, margin: new RectOffset(0, 0, 1, 3));
            Warning = Wrapped(12, FontStyle.Italic, Red, margin: new RectOffset(0, 0, 1, 3));

            Section = Text(12, FontStyle.Bold, Ink, TextAnchor.LowerLeft);
            Section.normal.background = Underline();
            Section.border = new RectOffset(0, 0, 0, 2);
            Section.padding = new RectOffset(0, 0, 0, 3);
            Section.margin = new RectOffset(0, 0, 12, 6);

            Banner = Text(14, FontStyle.Bold, Paper, TextAnchor.MiddleCenter);
            Banner.normal.background = InkTexture;

            AdTitle = Text(13, FontStyle.Bold, Ink, TextAnchor.UpperLeft);
            AdTitle.margin = new RectOffset(0, 0, 0, 2);
            Advert = new GUIStyle
            {
                normal = { background = Frame(Ink, new Color(1f, 1f, 1f, 0.18f)) },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(9, 9, 6, 6),
                margin = new RectOffset(0, 0, 0, 7),
            };

            Label = Text(13, FontStyle.Normal, Ink, TextAnchor.MiddleLeft);
            Label.fixedHeight = 22;
            Strong = new GUIStyle(Label) { fontStyle = FontStyle.Bold };
            Value = new GUIStyle(Label) { alignment = TextAnchor.MiddleRight };
            StrongValue = new GUIStyle(Value) { fontStyle = FontStyle.Bold };
            Gain = new GUIStyle(StrongValue) { normal = { textColor = Green } };
            Loss = new GUIStyle(StrongValue) { normal = { textColor = Red } };
            Column = new GUIStyle(Value) { fontSize = 11, fontStyle = FontStyle.Italic, normal = { textColor = Muted } };
            ColumnLeft = new GUIStyle(Column) { alignment = TextAnchor.MiddleLeft };

            // In a row of buttons: as tall as a button, so the text lines up with theirs.
            Count = Text(15, FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
            Count.fixedHeight = 26;
            Count.margin = new RectOffset(0, 0, 2, 2);
            Note = new GUIStyle(Count) { fontSize = 12, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleRight, normal = { textColor = Muted } };
            RowLabel = new GUIStyle(Count) { fontSize = 13, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleLeft };

            var button = Frame(Ink, new Color(1f, 1f, 1f, 0.35f));
            var hover = Frame(Ink, Faint);
            var pressed = Frame(Ink, Pressed);
            var inked = Frame(Ink, Ink);
            Button = Text(13, FontStyle.Normal, Ink, TextAnchor.MiddleCenter);
            Button.fixedHeight = 26;
            Button.border = new RectOffset(2, 2, 2, 2);
            Button.margin = new RectOffset(2, 2, 2, 2);
            Button.padding = new RectOffset(8, 8, 3, 3);
            Button.normal.background = button;
            Button.hover = new GUIStyleState { background = hover, textColor = Ink };
            Button.active = new GUIStyleState { background = pressed, textColor = Ink };
            Button.onNormal = new GUIStyleState { background = inked, textColor = Paper };
            Button.onHover = new GUIStyleState { background = inked, textColor = Paper };
            Button.onActive = new GUIStyleState { background = inked, textColor = Paper };

            Primary = new GUIStyle(Button) { fontSize = 17, fontStyle = FontStyle.Bold, fixedHeight = 40 };
            Primary.normal = new GUIStyleState { background = Frame(Red, Red), textColor = Paper };
            Primary.hover = new GUIStyleState { background = Frame(RedHover, RedHover), textColor = Paper };
            Primary.active = new GUIStyleState { background = Frame(Ink, Ink), textColor = Paper };

            Slider = new GUIStyle
            {
                normal = { background = Line(Ink) },
                border = new RectOffset(2, 2, 0, 0),
                fixedHeight = 16,
                margin = new RectOffset(4, 6, 7, 7),
            };

            Thumb = new GUIStyle
            {
                normal = { background = Disc(16, Ink) },
                hover = { background = Disc(16, Red) },
                active = { background = Disc(16, Red) },
                fixedWidth = 16,
                fixedHeight = 16,
            };
        }

        public Texture2D PaperTexture { get; }

        public Texture2D InkTexture { get; }

        public GUIStyle Masthead { get; }

        /// <summary>The small italic lines around the masthead.</summary>
        public GUIStyle Strap { get; }

        public GUIStyle StrapLeft { get; }

        public GUIStyle StrapRight { get; }

        public GUIStyle Headline { get; }

        public GUIStyle Standfirst { get; }

        public GUIStyle Byline { get; }

        public GUIStyle Body { get; }

        public GUIStyle Small { get; }

        public GUIStyle Quiet { get; }

        public GUIStyle Warning { get; }

        /// <summary>A section heading with a rule under it.</summary>
        public GUIStyle Section { get; }

        /// <summary>White on black, across the top of the classifieds.</summary>
        public GUIStyle Banner { get; }

        public GUIStyle AdTitle { get; }

        /// <summary>The box around a classified advert.</summary>
        public GUIStyle Advert { get; }

        public GUIStyle Label { get; }

        public GUIStyle Strong { get; }

        public GUIStyle Value { get; }

        public GUIStyle StrongValue { get; }

        public GUIStyle Gain { get; }

        public GUIStyle Loss { get; }

        /// <summary>A table's column heading.</summary>
        public GUIStyle Column { get; }

        public GUIStyle ColumnLeft { get; }

        public GUIStyle Count { get; }

        public GUIStyle Note { get; }

        /// <summary>A left-aligned label in a row of buttons.</summary>
        public GUIStyle RowLabel { get; }

        /// <summary>Outlined in ink; as a toggle, "on" is filled with ink.</summary>
        public GUIStyle Button { get; }

        /// <summary>The red button that opens the shop for the day.</summary>
        public GUIStyle Primary { get; }

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
            if (_serif != null)
            {
                Object.Destroy(_serif);
            }
        }

        private GUIStyle Text(int size, FontStyle style, Color colour, TextAnchor alignment) => new GUIStyle
        {
            font = _serif,
            fontSize = size,
            fontStyle = style,
            alignment = alignment,
            richText = true,
            normal = { textColor = colour },
        };

        private GUIStyle Wrapped(int size, FontStyle style, Color colour, RectOffset margin)
        {
            var wrapped = Text(size, style, colour, TextAnchor.UpperLeft);
            wrapped.wordWrap = true;
            wrapped.margin = margin;
            return wrapped;
        }

        private Texture2D Solid(Color colour) => Draw(2, 2, (x, y) => colour);

        // A box with a 1-pixel border (nine-sliced with a border of 2, so the edge stays crisp).
        private Texture2D Frame(Color edge, Color fill) => Draw(8, 8, (x, y) => x < 1 || y < 1 || x >= 7 || y >= 7 ? edge : fill);

        // Clear, with a 2-pixel rule along the bottom.
        private Texture2D Underline() => Draw(4, 6, (x, y) => y < 2 ? Ink : Color.clear);

        // A thin line across the middle of a slider's height.
        private Texture2D Line(Color colour) => Draw(4, 16, (x, y) => y >= 7 && y < 9 ? colour : Color.clear);

        private Texture2D Disc(int size, Color colour)
        {
            var radius = size * 0.5f;
            return Draw(size, size, (x, y) =>
            {
                var c = colour;
                c.a *= Mathf.Clamp01(radius - 1f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius)) + 0.5f);
                return c;
            });
        }

        private Texture2D Draw(int width, int height, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };

            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[(y * width) + x] = pixel(x, y);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _textures.Add(texture);
            return texture;
        }
    }
}
