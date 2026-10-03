using System;

namespace Townscape.Simulation.Weather
{
    /// <summary>
    /// A looping animation of tileable water normal maps: rings spreading from raindrops, plus an
    /// optional gentle swell. Played as a flipbook on puddles and water, and scrolled downstream
    /// on the river, so no custom shader is needed.
    /// </summary>
    public sealed class RippleField
    {
        // Ring radius at the end of its life and how long a ring lasts, as a share of the loop.
        private const float MaxRadiusShare = 0.085f;
        private const float RingLife = 0.6f;
        private const float RingWidth = 2.2f;

        private static readonly (int Kx, int Ky, int Cycles, float Amplitude, float Phase)[] Swell =
        {
            (1, 2, 1, 0.55f, 0.3f),
            (3, -1, -1, 0.35f, 1.9f),
            (-2, 3, 1, 0.3f, 4.1f),
            (5, 2, 2, 0.15f, 2.6f),
        };

        private readonly (float X, float Y, float Start, float Strength)[] _drops;
        private readonly float _swell;

        /// <param name="size">Width and height of each frame in pixels.</param>
        /// <param name="frames">Frames in one loop.</param>
        /// <param name="drops">Raindrops landing in one loop.</param>
        /// <param name="swell">Strength of the swell relative to a ring; 0 for none.</param>
        public RippleField(int size, int frames, int drops, float swell, int seed)
        {
            Size = size;
            Frames = frames;
            _swell = swell;
            var random = new Random(seed);
            _drops = new (float, float, float, float)[drops];
            for (var i = 0; i < drops; i++)
            {
                _drops[i] = ((float)random.NextDouble() * size, (float)random.NextDouble() * size, (float)random.NextDouble(), 0.6f + (0.4f * (float)random.NextDouble()));
            }
        }

        public int Size { get; }

        public int Frames { get; }

        /// <summary>Surface heights for one frame, row by row from the bottom.</summary>
        public float[] Heights(int frame)
        {
            var heights = new float[Size * Size];
            var loop = Fraction((float)frame / Frames);
            AddSwell(heights, loop);

            var maxRadius = MaxRadiusShare * Size;
            foreach (var drop in _drops)
            {
                var age = Fraction(loop - drop.Start);
                if (age >= RingLife)
                {
                    continue;
                }

                var life = age / RingLife;
                var radius = life * maxRadius;
                var amplitude = drop.Strength * (1f - life) * (1f - life);
                var reach = (int)MathF.Ceiling(radius + RingWidth);
                var cx = (int)MathF.Floor(drop.X);
                var cy = (int)MathF.Floor(drop.Y);
                for (var dy = -reach; dy <= reach; dy++)
                {
                    for (var dx = -reach; dx <= reach; dx++)
                    {
                        var px = cx + dx;
                        var py = cy + dy;
                        var distance = MathF.Sqrt(Square(px + 0.5f - drop.X) + Square(py + 0.5f - drop.Y));
                        var fromRing = MathF.Abs(distance - radius);
                        if (fromRing >= RingWidth)
                        {
                            continue;
                        }

                        var crest = MathF.Cos(0.5f * MathF.PI * fromRing / RingWidth);
                        heights[Index(px, py)] += amplitude * crest * crest;
                    }
                }
            }

            return heights;
        }

        /// <summary>
        /// A tangent-space normal map for one frame as RGBA32 bytes (x in red, y in green), which
        /// URP's normal unpacking reads correctly from an uncompressed texture.
        /// </summary>
        public byte[] NormalMap(int frame, float strength)
        {
            var heights = Heights(frame);
            var pixels = new byte[Size * Size * 4];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var dx = (heights[Index(x + 1, y)] - heights[Index(x - 1, y)]) * 0.5f * strength;
                    var dy = (heights[Index(x, y + 1)] - heights[Index(x, y - 1)]) * 0.5f * strength;
                    var length = MathF.Sqrt((dx * dx) + (dy * dy) + 1f);
                    var o = ((y * Size) + x) * 4;
                    pixels[o] = Pack(-dx / length);
                    pixels[o + 1] = Pack(-dy / length);
                    pixels[o + 2] = Pack(1f / length);
                    pixels[o + 3] = 255;
                }
            }

            return pixels;
        }

        private void AddSwell(float[] heights, float loop)
        {
            if (_swell <= 0f)
            {
                return;
            }

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var sum = 0f;
                    foreach (var wave in Swell)
                    {
                        var phase = (2f * MathF.PI * (((wave.Kx * x) + (wave.Ky * y)) / (float)Size + (wave.Cycles * loop))) + wave.Phase;
                        sum += wave.Amplitude * MathF.Sin(phase);
                    }

                    heights[(y * Size) + x] = _swell * sum;
                }
            }
        }

        // Wraps around both edges, so the texture tiles seamlessly.
        private int Index(int x, int y) => (Wrap(y) * Size) + Wrap(x);

        private int Wrap(int value)
        {
            var wrapped = value % Size;
            return wrapped < 0 ? wrapped + Size : wrapped;
        }

        private static byte Pack(float component) => (byte)Math.Clamp((int)MathF.Round(((component * 0.5f) + 0.5f) * 255f), 0, 255);

        private static float Square(float value) => value * value;

        private static float Fraction(float value) => value - MathF.Floor(value);
    }
}
