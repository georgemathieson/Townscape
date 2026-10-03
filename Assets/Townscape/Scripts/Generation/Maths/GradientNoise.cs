using System;

namespace Townscape.Generation.Maths
{
    /// <summary>
    /// Deterministic 2D gradient (Perlin-style) noise. Same seed, same town: generation never
    /// depends on UnityEngine.Random or the time the scene was opened.
    /// </summary>
    public sealed class GradientNoise
    {
        private readonly uint _seed;

        public GradientNoise(int seed)
        {
            _seed = unchecked((uint)seed * 0x9E3779B9u) ^ 0x85EBCA6Bu;
        }

        /// <summary>Noise in roughly [-1, 1].</summary>
        public float Sample(float x, float y)
        {
            var x0 = (int)MathF.Floor(x);
            var y0 = (int)MathF.Floor(y);
            var fx = x - x0;
            var fy = y - y0;

            var n00 = Dot(x0, y0, fx, fy);
            var n10 = Dot(x0 + 1, y0, fx - 1f, fy);
            var n01 = Dot(x0, y0 + 1, fx, fy - 1f);
            var n11 = Dot(x0 + 1, y0 + 1, fx - 1f, fy - 1f);

            var u = Fade(fx);
            var v = Fade(fy);
            var nx0 = GeoMath.Lerp(n00, n10, u);
            var nx1 = GeoMath.Lerp(n01, n11, u);
            return GeoMath.Lerp(nx0, nx1, v) * 1.41421356f;
        }

        /// <summary>Fractal sum of octaves, normalised back to roughly [-1, 1].</summary>
        public float Fractal(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            var sum = 0f;
            var amplitude = 1f;
            var frequency = 1f;
            var norm = 0f;
            for (var i = 0; i < octaves; i++)
            {
                // Offset each octave so their lattices do not line up.
                sum += Sample((x * frequency) + (i * 17.31f), (y * frequency) - (i * 9.73f)) * amplitude;
                norm += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return sum / norm;
        }

        /// <summary>Deterministic hash of a lattice cell to [0, 1), handy for per-cell variation.</summary>
        public float Hash01(int x, int y)
        {
            return (Hash(x, y) & 0xFFFFFF) / 16777216f;
        }

        private static float Fade(float t) => t * t * t * ((t * ((t * 6f) - 15f)) + 10f);

        private float Dot(int ix, int iy, float dx, float dy)
        {
            var angle = (Hash(ix, iy) & 0xFFFF) / 65536f * MathF.PI * 2f;
            return (MathF.Cos(angle) * dx) + (MathF.Sin(angle) * dy);
        }

        private uint Hash(int x, int y)
        {
            unchecked
            {
                var h = _seed;
                h ^= (uint)x * 0x27D4EB2Du;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0x165667B1u;
                h *= 0x85EBCA77u;
                h ^= h >> 15;
                h *= 0xC2B2AE3Du;
                h ^= h >> 13;
                return h;
            }
        }
    }
}
