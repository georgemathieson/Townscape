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
        Concrete,
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
        Pantile,
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
        PaintBlue,
        PaintGreen,
        Bread,

        // Street furniture and greenery
        Iron,
        LampGlass,
        Beacon,
        Bark,
        BirchBark,
        LeafGreen,
        LeafDark,
        LeafLight,
        LeafAutumn,
        PineGreen,
        GrassTuft,

        // Glass that lights up after dark. Home windows are split into groups (keep Window0 to
        // Window7 consecutive) so they come on one by one.
        Window0,
        Window1,
        Window2,
        Window3,
        Window4,
        Window5,
        Window6,
        Window7,
        WindowShop,
        InnWindow,
        SignGlass,
        // The petrol station's lights: panels in the canopy ceiling, and coloured string-light bulbs.
        CanopyLight,
        BulbRed,
        BulbGreen,
        BulbOrange,
        BulbYellow,
        BulbBlue,

        // Inside the Copper Kettle and its flat: only ever under a roof, so rain and snow leave them be.
        TileLight,
        TileDark,
        FabricSage,
        FabricRust,
        FabricNavy,
        Linen,
        Porcelain,
        Chrome,
        Cardboard,
        Sponge,
        Icing,
        Chalkboard,

        // Window glass you see through from both sides, faintly tinted so you can tell it's there.
        ClearGlass,

        // Water that moves: the river flows and puddles ripple in the rain.
        RiverWater,
        Puddle,
    }
}
