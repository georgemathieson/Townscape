using System;
using System.Collections.Generic;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Simulation.Weather
{
    /// <summary>
    /// What settling snow does to surfaces. Each kind of surface whitens over its own part of the
    /// build-up: cold roofs and open grass first, flagstones next, and tarmac last, where the
    /// traffic keeps it to a grey slush. Walls, glass and water never take snow. Like
    /// <see cref="Wetness"/>, it is applied to the shared materials, so no custom shader is needed.
    /// </summary>
    public static class SnowCover
    {
        /// <summary>The colour of fresh snow, in sRGB like the palette.</summary>
        public static readonly SurfaceAppearance Fresh = new SurfaceAppearance(0.93f, 0.95f, 0.98f, 0.2f);

        // Slush is wet, so it shines more than powder.
        private const float SlushSmoothness = 0.62f;

        private static readonly Dictionary<SurfaceMaterial, Settling> Response = new Dictionary<SurfaceMaterial, Settling>();

        static SnowCover()
        {
            Set(new Settling(1f, 0f, 0.55f, 1f), SurfaceMaterial.Slate, SurfaceMaterial.Pantile);
            Set(new Settling(1f, 0.05f, 0.7f, 1f), SurfaceMaterial.Grass, SurfaceMaterial.GrassDark, SurfaceMaterial.FellGrass, SurfaceMaterial.Bracken, SurfaceMaterial.Earth, SurfaceMaterial.Gravel, SurfaceMaterial.Shingle);
            Set(new Settling(0.9f, 0.05f, 0.75f, 1f), SurfaceMaterial.Scree, SurfaceMaterial.GrassTuft);
            Set(new Settling(0.92f, 0.15f, 0.85f, 1f), SurfaceMaterial.Pavement, SurfaceMaterial.Concrete);
            Set(new Settling(0.6f, 0.2f, 0.9f, 1f), SurfaceMaterial.Kerb);
            Set(new Settling(0.55f, 0.35f, 1f, 0.72f), SurfaceMaterial.Road);
            Set(new Settling(0.6f, 0.35f, 1f, 0.9f), SurfaceMaterial.MarkingWhite, SurfaceMaterial.MarkingYellow);
            Set(new Settling(0.62f, 0.1f, 0.8f, 1f), SurfaceMaterial.LeafGreen, SurfaceMaterial.LeafDark, SurfaceMaterial.LeafLight, SurfaceMaterial.LeafAutumn);
            Set(new Settling(0.6f, 0.1f, 0.8f, 1f), SurfaceMaterial.PineGreen);
        }

        /// <summary>Materials that snow settles on.</summary>
        public static IEnumerable<SurfaceMaterial> Affected => Response.Keys;

        /// <summary>
        /// <paramref name="bare"/> as it looks under <paramref name="cover"/> (0 bare, 1 deep snow).
        /// Materials that snow doesn't settle on come back as they were.
        /// </summary>
        public static SurfaceAppearance Apply(SurfaceMaterial material, SurfaceAppearance bare, float cover)
        {
            if (!Response.TryGetValue(material, out var settling) || cover <= 0f)
            {
                return bare;
            }

            var white = settling.Weight * GeoMath.SmoothStep(settling.Start, settling.End, cover);
            var shade = settling.Shade;
            var smoothness = shade < 1f ? SlushSmoothness : Fresh.Smoothness;
            return new SurfaceAppearance(
                GeoMath.Lerp(bare.R, Fresh.R * shade, white),
                GeoMath.Lerp(bare.G, Fresh.G * shade, white),
                GeoMath.Lerp(bare.B, Fresh.B * shade, white),
                GeoMath.Lerp(bare.Smoothness, smoothness, white),
                bare.Alpha);
        }

        /// <summary>
        /// Moves <paramref name="cover"/> on by <paramref name="deltaTime"/> seconds: snow settles in
        /// about a minute and a half of heavy snow, thaws over several minutes once it stops, and
        /// rain washes it away in about one.
        /// </summary>
        public static float Step(float cover, float snow, float rain, float deltaTime)
        {
            if (snow > 0.02f && rain < 0.05f)
            {
                return MathF.Min(1f, cover + ((0.0015f + (0.013f * snow)) * deltaTime));
            }

            return MathF.Max(0f, cover - ((0.0025f + (0.015f * rain)) * deltaTime));
        }

        private static void Set(Settling settling, params SurfaceMaterial[] materials)
        {
            foreach (var material in materials)
            {
                Response[material] = settling;
            }
        }

        /// <summary>
        /// How snow settles on one kind of surface: how white it goes at most, over which part of
        /// the build-up, and how clean the snow stays (1 white, less for grey slush).
        /// </summary>
        private readonly struct Settling
        {
            public Settling(float weight, float start, float end, float shade)
            {
                Weight = weight;
                Start = start;
                End = end;
                Shade = shade;
            }

            public float Weight { get; }

            public float Start { get; }

            public float End { get; }

            public float Shade { get; }
        }
    }
}
