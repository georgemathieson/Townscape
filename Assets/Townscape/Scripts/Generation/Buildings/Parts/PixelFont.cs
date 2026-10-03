using System;
using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>
    /// A tiny 5×7 bitmap font for shop signs, built as geometry. It needs no font assets, renders
    /// identically in Unity and the offline preview, and suits the low poly look.
    /// </summary>
    public static class PixelFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;
        private const int Advance = GlyphWidth + 1;

        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#." },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { "#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###." },
            ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." },
            ['&'] = new[] { ".##..", "#..#.", "#.#..", ".#...", "#.#.#", "#..#.", ".##.#" },
            ['\''] = new[] { "..#..", "..#..", ".#...", ".....", ".....", ".....", "....." },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".##..", ".##.." },
            ['-'] = new[] { ".....", ".....", ".....", ".###.", ".....", ".....", "....." },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
            [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
        };

        public static bool Supports(char character) => Glyphs.ContainsKey(char.ToUpperInvariant(character));

        /// <summary>Width of <paramref name="text"/> in pixels.</summary>
        public static int PixelWidth(string text) => text.Length == 0 ? 0 : (text.Length * Advance) - 1;

        /// <summary>
        /// Writes <paramref name="text"/> on a wall, centred on (<paramref name="centreX"/>,
        /// <paramref name="centreY"/>), as flat quads <paramref name="z"/> out from the wall. The
        /// pixel size shrinks if needed to fit <paramref name="maxWidth"/>.
        /// </summary>
        public static void Write(MeshBuilder builder, WallFrame wall, string text, float centreX, float centreY, float maxPixel, float maxWidth, float z, SurfaceMaterial material)
        {
            text = text.ToUpperInvariant();
            var columns = PixelWidth(text);
            if (columns == 0)
            {
                return;
            }

            var pixel = Math.Min(maxPixel, maxWidth / columns);
            var left = centreX - (columns * pixel * 0.5f);
            var bottom = centreY - (GlyphHeight * pixel * 0.5f);

            for (var c = 0; c < text.Length; c++)
            {
                if (!Glyphs.TryGetValue(text[c], out var rows))
                {
                    continue;
                }

                var glyphLeft = left + (c * Advance * pixel);
                for (var r = 0; r < GlyphHeight; r++)
                {
                    var y0 = bottom + ((GlyphHeight - 1 - r) * pixel);
                    var row = rows[r];

                    // One quad per run of lit pixels.
                    var runStart = -1;
                    for (var i = 0; i <= GlyphWidth; i++)
                    {
                        var lit = i < GlyphWidth && row[i] == '#';
                        if (lit && runStart < 0)
                        {
                            runStart = i;
                        }
                        else if (!lit && runStart >= 0)
                        {
                            wall.Quad(builder, glyphLeft + (runStart * pixel), y0, glyphLeft + (i * pixel), y0 + pixel, z, material);
                            runStart = -1;
                        }
                    }
                }
            }
        }
    }
}
