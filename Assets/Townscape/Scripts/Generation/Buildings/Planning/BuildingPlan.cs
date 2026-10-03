namespace Townscape.Generation.Buildings.Planning
{
    /// <summary>
    /// Where a building goes and how it is built. Plans are made before any geometry, so the
    /// ground can be flattened and paved under every building first.
    /// </summary>
    public sealed class BuildingPlan
    {
        public BuildingPlan(string group, Footprint footprint, IBuildingStyle style, int seed)
        {
            Group = group;
            Footprint = footprint;
            Style = style;
            Seed = seed;
        }

        /// <summary>Plans with the same group (a terrace, say) end up in the same mesh.</summary>
        public string Group { get; }

        public Footprint Footprint { get; }

        public IBuildingStyle Style { get; }

        public int Seed { get; }
    }
}
