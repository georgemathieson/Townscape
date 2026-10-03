using System;
using System.Collections.Generic;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Simulation.Weather
{
    /// <summary>
    /// What rain does to surfaces. Wet things go darker as water fills their pores and glossier
    /// as a film of water covers them: tarmac and flagstones most, painted wood and render less,
    /// glass and water not at all. Applied to the shared materials, so no custom shader is needed.
    /// </summary>
    public static class Wetness
    {
        private static readonly Dictionary<SurfaceMaterial, (float Darken, float Smoothness)> Response =
            new Dictionary<SurfaceMaterial, (float, float)>();

        static Wetness()
        {
            // Wet tarmac blurs reflections; puddles (a separate material) give sharp ones.
            Set(0.45f, 0.84f, SurfaceMaterial.Road);
            Set(0.08f, 0.8f, SurfaceMaterial.MarkingWhite, SurfaceMaterial.MarkingYellow);
            Set(0.4f, 0.78f, SurfaceMaterial.Pavement, SurfaceMaterial.Kerb, SurfaceMaterial.Stone, SurfaceMaterial.StoneDark, SurfaceMaterial.StoneGreen);
            Set(0.35f, 0.6f, SurfaceMaterial.Gravel, SurfaceMaterial.Shingle, SurfaceMaterial.Scree, SurfaceMaterial.RiverBed);
            Set(0.3f, 0.84f, SurfaceMaterial.Slate);
            Set(0.3f, 0.55f, SurfaceMaterial.Earth);
            Set(0.18f, 0.4f, SurfaceMaterial.RenderWhite, SurfaceMaterial.RenderCream, SurfaceMaterial.ChimneyPot);
            Set(0.25f, 0.5f, SurfaceMaterial.Timber, SurfaceMaterial.Bark, SurfaceMaterial.BirchBark);
            Set(0.18f, 0.35f, SurfaceMaterial.Grass, SurfaceMaterial.GrassDark, SurfaceMaterial.FellGrass, SurfaceMaterial.Bracken, SurfaceMaterial.GrassTuft);
            Set(0.12f, 0.5f, SurfaceMaterial.LeafGreen, SurfaceMaterial.LeafDark, SurfaceMaterial.LeafLight, SurfaceMaterial.LeafAutumn, SurfaceMaterial.PineGreen);
            Set(0.05f, 0.8f, SurfaceMaterial.Iron);
            Set(0.06f, 0.75f,
                SurfaceMaterial.PaintWhite, SurfaceMaterial.PaintBlack, SurfaceMaterial.PaintDarkGreen, SurfaceMaterial.PaintNavy,
                SurfaceMaterial.PaintOxblood, SurfaceMaterial.PaintTeal, SurfaceMaterial.PaintCream, SurfaceMaterial.PaintSage,
                SurfaceMaterial.PaintDuckEgg, SurfaceMaterial.PaintPink, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintRed,
                SurfaceMaterial.PaintGold, SurfaceMaterial.PaintOrange, SurfaceMaterial.PaintPurple);
        }

        /// <summary>Materials that change when wet.</summary>
        public static IEnumerable<SurfaceMaterial> Affected => Response.Keys;

        /// <summary>
        /// <paramref name="dry"/> as it looks at <paramref name="wetness"/> (0 dry, 1 soaked).
        /// Materials that rain doesn't change come back as they were.
        /// </summary>
        public static SurfaceAppearance Apply(SurfaceMaterial material, SurfaceAppearance dry, float wetness)
        {
            if (!Response.TryGetValue(material, out var response) || wetness <= 0f)
            {
                return dry;
            }

            var w = GeoMath.Clamp01(wetness);
            var shade = 1f - (response.Darken * w);
            var smoothness = MathF.Max(dry.Smoothness, GeoMath.Lerp(dry.Smoothness, response.Smoothness, w));
            return new SurfaceAppearance(dry.R * shade, dry.G * shade, dry.B * shade, smoothness, dry.Alpha);
        }

        /// <summary>
        /// Moves <paramref name="wetness"/> on by <paramref name="deltaTime"/> seconds: surfaces soak
        /// up in about half a minute of heavy rain and take several minutes to dry.
        /// </summary>
        public static float Step(float wetness, float rain, float deltaTime)
        {
            var target = GeoMath.SmoothStep(0f, 0.3f, rain);
            var rate = wetness < target ? 0.02f + (0.06f * rain) : 0.004f;
            var step = rate * deltaTime;
            return wetness < target ? MathF.Min(target, wetness + step) : MathF.Max(target, wetness - step);
        }

        private static void Set(float darken, float smoothness, params SurfaceMaterial[] materials)
        {
            foreach (var material in materials)
            {
                Response[material] = (darken, smoothness);
            }
        }
    }
}
