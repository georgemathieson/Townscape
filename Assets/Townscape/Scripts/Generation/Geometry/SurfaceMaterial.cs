namespace Townscape.Generation.Geometry
{
    /// <summary>
    /// Every surface the generators can emit. Each value becomes one submesh, and the Unity
    /// layer maps it to a material, so the generators never need to know about rendering.
    /// </summary>
    public enum SurfaceMaterial
    {
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
    }
}
