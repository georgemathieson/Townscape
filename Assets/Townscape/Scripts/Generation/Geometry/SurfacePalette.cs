using System.Collections.Generic;

namespace Townscape.Generation.Geometry
{
    /// <summary>Colour (sRGB, components in [0, 1]), opacity and smoothness for a surface.</summary>
    public readonly struct SurfaceAppearance
    {
        public SurfaceAppearance(float r, float g, float b, float smoothness, float alpha = 1f)
        {
            R = r;
            G = g;
            B = b;
            Smoothness = smoothness;
            Alpha = alpha;
        }

        public float R { get; }

        public float G { get; }

        public float B { get; }

        public float Smoothness { get; }

        /// <summary>Opacity. Anything below 1 is rendered as transparent (shop window glass).</summary>
        public float Alpha { get; }

        public bool IsTransparent => Alpha < 0.999f;
    }

    /// <summary>
    /// The Lake District palette: grey-green slate and stone, whitewash, muted grass, dark wet
    /// tarmac and traditional shopfront paints. Kept engine-free so offline preview renders use
    /// exactly the same colours as Unity.
    /// </summary>
    public static class SurfacePalette
    {
        private static readonly SurfaceAppearance Glass = new SurfaceAppearance(0.10f, 0.13f, 0.16f, 0.92f);

        private static readonly Dictionary<SurfaceMaterial, SurfaceAppearance> Appearances =
            new Dictionary<SurfaceMaterial, SurfaceAppearance>
            {
                [SurfaceMaterial.Grass] = new SurfaceAppearance(0.34f, 0.47f, 0.25f, 0.18f),
                [SurfaceMaterial.GrassDark] = new SurfaceAppearance(0.28f, 0.41f, 0.22f, 0.18f),
                [SurfaceMaterial.FellGrass] = new SurfaceAppearance(0.40f, 0.45f, 0.27f, 0.12f),
                [SurfaceMaterial.Bracken] = new SurfaceAppearance(0.50f, 0.36f, 0.20f, 0.10f),
                [SurfaceMaterial.Scree] = new SurfaceAppearance(0.38f, 0.38f, 0.37f, 0.15f),
                [SurfaceMaterial.Earth] = new SurfaceAppearance(0.32f, 0.26f, 0.19f, 0.15f),
                [SurfaceMaterial.Shingle] = new SurfaceAppearance(0.46f, 0.43f, 0.38f, 0.25f),
                [SurfaceMaterial.Road] = new SurfaceAppearance(0.15f, 0.15f, 0.16f, 0.45f),
                [SurfaceMaterial.Pavement] = new SurfaceAppearance(0.42f, 0.41f, 0.39f, 0.35f),
                [SurfaceMaterial.Kerb] = new SurfaceAppearance(0.52f, 0.51f, 0.48f, 0.30f),
                [SurfaceMaterial.Gravel] = new SurfaceAppearance(0.52f, 0.47f, 0.38f, 0.15f),
                [SurfaceMaterial.RiverBed] = new SurfaceAppearance(0.22f, 0.20f, 0.16f, 0.30f),
                [SurfaceMaterial.Water] = new SurfaceAppearance(0.09f, 0.15f, 0.17f, 0.92f),
                [SurfaceMaterial.Stone] = new SurfaceAppearance(0.43f, 0.42f, 0.40f, 0.22f),
                [SurfaceMaterial.StoneDark] = new SurfaceAppearance(0.30f, 0.30f, 0.31f, 0.25f),
                [SurfaceMaterial.MarkingWhite] = new SurfaceAppearance(0.85f, 0.85f, 0.82f, 0.40f),
                [SurfaceMaterial.MarkingYellow] = new SurfaceAppearance(0.85f, 0.66f, 0.12f, 0.40f),

                [SurfaceMaterial.StoneGreen] = new SurfaceAppearance(0.40f, 0.42f, 0.38f, 0.22f),
                [SurfaceMaterial.RenderWhite] = new SurfaceAppearance(0.86f, 0.86f, 0.82f, 0.20f),
                [SurfaceMaterial.RenderCream] = new SurfaceAppearance(0.84f, 0.78f, 0.64f, 0.20f),
                [SurfaceMaterial.Slate] = new SurfaceAppearance(0.21f, 0.23f, 0.26f, 0.40f),
                [SurfaceMaterial.ChimneyPot] = new SurfaceAppearance(0.60f, 0.32f, 0.21f, 0.20f),
                [SurfaceMaterial.Timber] = new SurfaceAppearance(0.32f, 0.22f, 0.14f, 0.25f),

                [SurfaceMaterial.WindowGlass] = new SurfaceAppearance(0.10f, 0.13f, 0.16f, 0.92f),
                [SurfaceMaterial.ShopGlass] = new SurfaceAppearance(0.62f, 0.70f, 0.74f, 0.95f, 0.22f),
                [SurfaceMaterial.Interior] = new SurfaceAppearance(0.76f, 0.69f, 0.56f, 0.15f),
                [SurfaceMaterial.InteriorFloor] = new SurfaceAppearance(0.42f, 0.30f, 0.20f, 0.30f),
                [SurfaceMaterial.Screen] = new SurfaceAppearance(0.30f, 0.62f, 0.86f, 0.80f),

                [SurfaceMaterial.PaintWhite] = new SurfaceAppearance(0.90f, 0.90f, 0.87f, 0.45f),
                [SurfaceMaterial.PaintBlack] = new SurfaceAppearance(0.07f, 0.07f, 0.08f, 0.50f),
                [SurfaceMaterial.PaintDarkGreen] = new SurfaceAppearance(0.10f, 0.26f, 0.18f, 0.50f),
                [SurfaceMaterial.PaintNavy] = new SurfaceAppearance(0.10f, 0.15f, 0.30f, 0.50f),
                [SurfaceMaterial.PaintOxblood] = new SurfaceAppearance(0.42f, 0.09f, 0.11f, 0.50f),
                [SurfaceMaterial.PaintTeal] = new SurfaceAppearance(0.09f, 0.36f, 0.38f, 0.50f),
                [SurfaceMaterial.PaintCream] = new SurfaceAppearance(0.89f, 0.84f, 0.70f, 0.45f),
                [SurfaceMaterial.PaintSage] = new SurfaceAppearance(0.58f, 0.67f, 0.55f, 0.45f),
                [SurfaceMaterial.PaintDuckEgg] = new SurfaceAppearance(0.62f, 0.77f, 0.77f, 0.45f),
                [SurfaceMaterial.PaintPink] = new SurfaceAppearance(0.90f, 0.67f, 0.67f, 0.45f),
                [SurfaceMaterial.PaintButter] = new SurfaceAppearance(0.94f, 0.85f, 0.56f, 0.45f),
                [SurfaceMaterial.PaintRed] = new SurfaceAppearance(0.72f, 0.06f, 0.06f, 0.55f),
                [SurfaceMaterial.PaintGold] = new SurfaceAppearance(0.84f, 0.65f, 0.27f, 0.60f),
                [SurfaceMaterial.PaintOrange] = new SurfaceAppearance(0.93f, 0.48f, 0.12f, 0.50f),
                [SurfaceMaterial.PaintPurple] = new SurfaceAppearance(0.40f, 0.24f, 0.50f, 0.50f),
                [SurfaceMaterial.Bread] = new SurfaceAppearance(0.76f, 0.54f, 0.30f, 0.20f),

                [SurfaceMaterial.Iron] = new SurfaceAppearance(0.09f, 0.10f, 0.10f, 0.55f),
                [SurfaceMaterial.LampGlass] = new SurfaceAppearance(0.95f, 0.86f, 0.62f, 0.85f),
                [SurfaceMaterial.Beacon] = new SurfaceAppearance(0.98f, 0.55f, 0.12f, 0.70f),
                [SurfaceMaterial.Bark] = new SurfaceAppearance(0.30f, 0.24f, 0.18f, 0.10f),
                [SurfaceMaterial.BirchBark] = new SurfaceAppearance(0.82f, 0.80f, 0.74f, 0.15f),
                [SurfaceMaterial.LeafGreen] = new SurfaceAppearance(0.27f, 0.42f, 0.20f, 0.15f),
                [SurfaceMaterial.LeafDark] = new SurfaceAppearance(0.17f, 0.31f, 0.16f, 0.15f),
                [SurfaceMaterial.LeafLight] = new SurfaceAppearance(0.45f, 0.55f, 0.24f, 0.15f),
                [SurfaceMaterial.LeafAutumn] = new SurfaceAppearance(0.62f, 0.42f, 0.16f, 0.15f),
                [SurfaceMaterial.PineGreen] = new SurfaceAppearance(0.13f, 0.25f, 0.18f, 0.12f),
                [SurfaceMaterial.GrassTuft] = new SurfaceAppearance(0.40f, 0.52f, 0.26f, 0.15f),

                [SurfaceMaterial.Window0] = Glass,
                [SurfaceMaterial.Window1] = Glass,
                [SurfaceMaterial.Window2] = Glass,
                [SurfaceMaterial.Window3] = Glass,
                [SurfaceMaterial.Window4] = Glass,
                [SurfaceMaterial.Window5] = Glass,
                [SurfaceMaterial.Window6] = Glass,
                [SurfaceMaterial.Window7] = Glass,
                [SurfaceMaterial.WindowShop] = Glass,
                [SurfaceMaterial.InnWindow] = Glass,
                [SurfaceMaterial.SignGlass] = new SurfaceAppearance(0.90f, 0.88f, 0.80f, 0.80f),

                [SurfaceMaterial.RiverWater] = new SurfaceAppearance(0.09f, 0.15f, 0.17f, 0.92f),
                [SurfaceMaterial.Puddle] = new SurfaceAppearance(0.14f, 0.16f, 0.18f, 0.97f, 0.85f),
            };

        public static SurfaceAppearance Get(SurfaceMaterial material) =>
            Appearances.TryGetValue(material, out var appearance) ? appearance : new SurfaceAppearance(1f, 0f, 1f, 0f);
    }
}
