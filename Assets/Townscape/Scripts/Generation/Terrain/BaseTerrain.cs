using System;
using System.Numerics;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Terrain
{
    /// <summary>
    /// Height of open ground everywhere: gentle hummocks in the village, rising to fells beyond
    /// it, with a valley carved along the river and a basin for the lake.
    /// </summary>
    public sealed class BaseTerrain
    {
        private const float HummockAmplitude = 0.45f;
        private const float HummockFrequency = 0.045f;
        private const float FellRampDistance = 190f;

        private readonly TownLayout _layout;
        private readonly float _coreHalfExtent;
        private readonly GradientNoise _noise;

        public BaseTerrain(TownLayout layout, GenerationSettings settings)
        {
            _layout = layout;
            _coreHalfExtent = settings.CoreHalfExtent;
            _noise = new GradientNoise(layout.Seed);
        }

        public float GrassLevel => _layout.GrassLevel;

        /// <summary>
        /// Height of open ground in the village. Hummocks fade out near features (given by
        /// <paramref name="featureDistance"/>) and towards the edge of the detailed core, so the
        /// core's border is perfectly flat and lines up with the fells mesh.
        /// </summary>
        public float HeightAt(Vector2 p, float featureDistance)
        {
            var hummocks = _noise.Fractal(p.X * HummockFrequency, p.Y * HummockFrequency, 3) * HummockAmplitude;
            var fade = GeoMath.SmoothStep(1.5f, 8f, featureDistance)
                * (1f - GeoMath.SmoothStep(_coreHalfExtent - 14f, _coreHalfExtent - 2f, GeoMath.ChebyshevLength(p)));
            return GrassLevel + (hummocks * fade) + FellHeight(p);
        }

        /// <summary>Height for the coarse terrain outside the core, where the river and lake are carved directly.</summary>
        public float FarHeightAt(Vector2 p)
        {
            var height = GrassLevel + FellHeight(p);

            var river = _layout.River;
            var riverDistance = river.Centre.Closest(p).Distance;
            var bedHeight = _layout.WaterLevel - 0.9f;
            var channel = GeoMath.SmoothStep(river.WaterHalfWidth + 3f, river.WaterHalfWidth + river.BankWidth + 6f, riverDistance);
            height = GeoMath.Lerp(bedHeight, height, channel);

            var lake = _layout.Lake;
            if (lake != null)
            {
                var basin = GeoMath.SmoothStep(1f, 0.8f, lake.EllipseDistance(p));
                height = GeoMath.Lerp(height, _layout.WaterLevel - lake.Depth, basin);
            }

            return height;
        }

        /// <summary>Material choice noise in [0, 1] for patchy grass and bracken.</summary>
        public float PatchNoise(Vector2 p, float frequency) =>
            0.5f + (0.5f * _noise.Fractal((p.X * frequency) + 311f, (p.Y * frequency) - 127f, 2));

        private float FellHeight(Vector2 p)
        {
            var excess = GeoMath.ChebyshevLength(p) - _coreHalfExtent;
            if (excess <= 0f)
            {
                return 0f;
            }

            var wobble = 1f + (0.35f * _noise.Sample((p.X * 0.01f) + 40f, (p.Y * 0.01f) - 20f));
            var ramp = GeoMath.SmoothStep(0f, FellRampDistance, excess * wobble);
            var broad = 0.5f + (0.5f * _noise.Fractal(p.X * 0.0045f, p.Y * 0.0045f, 4));
            var detail = _noise.Fractal((p.X * 0.02f) + 100f, p.Y * 0.02f, 3) * 5f;
            var height = 14f + (56f * broad) + detail;

            // Flatten into a valley along the river and around the lake.
            var valley = GeoMath.SmoothStep(14f, 120f, _layout.River.Centre.Closest(p).Distance);
            var lakeShore = _layout.Lake == null ? 1f : GeoMath.SmoothStep(1.05f, 1.6f, _layout.Lake.EllipseDistance(p));

            return MathF.Max(0f, ramp * height * valley * lakeShore);
        }
    }
}
