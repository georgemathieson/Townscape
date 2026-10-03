using System.Collections.Generic;

namespace Townscape.Generation.Geometry
{
    /// <summary>Colour (sRGB, components in [0, 1]) and smoothness for a surface.</summary>
    public readonly struct SurfaceAppearance
    {
        public SurfaceAppearance(float r, float g, float b, float smoothness)
        {
            R = r;
            G = g;
            B = b;
            Smoothness = smoothness;
        }

        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float Smoothness { get; }
    }

    /// <summary>
    /// The Lake District palette: grey-green slate and stone, muted grass, dark wet tarmac.
    /// Kept engine-free so offline preview renders use exactly the same colours as Unity.
    /// </summary>
    public static class SurfacePalette
    {
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
            };

        public static SurfaceAppearance Get(SurfaceMaterial material) =>
            Appearances.TryGetValue(material, out var appearance) ? appearance : new SurfaceAppearance(1f, 0f, 1f, 0f);
    }
}
