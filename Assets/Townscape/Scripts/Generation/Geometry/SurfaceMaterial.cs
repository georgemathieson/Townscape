namespace Townscape.Generation.Geometry
{
    /// <summary>
    /// Every surface the generators can emit. Each value becomes one submesh, and the Unity
    /// layer maps it to a material, so the generators never need to know about rendering.
    /// </summary>
    public enum SurfaceMaterial
    {
        // Ground
        Grass,
        GrassDark,
        FellGrass,
        Bracken,
        Scree,
        Earth,
        Shingle,
        Road,
        Pavement,
        Kerb,
        Gravel,
        RiverBed,
        Water,
        Stone,
        StoneDark,
        MarkingWhite,
        MarkingYellow,

        // Building shells
        StoneGreen,
        RenderWhite,
        RenderCream,
        Slate,
        ChimneyPot,
        Timber,

        // Glass and interiors
        WindowGlass,
        ShopGlass,
        Interior,
        InteriorFloor,
        Screen,

        // Paints for shopfronts, doors, frames and signs
        PaintWhite,
        PaintBlack,
        PaintDarkGreen,
        PaintNavy,
        PaintOxblood,
        PaintTeal,
        PaintCream,
        PaintSage,
        PaintDuckEgg,
        PaintPink,
        PaintButter,
        PaintRed,
        PaintGold,
        PaintOrange,
        PaintPurple,
        Bread,
    }
}
