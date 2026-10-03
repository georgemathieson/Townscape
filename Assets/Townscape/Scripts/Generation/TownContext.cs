using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Terrain;

namespace Townscape.Generation
{
    /// <summary>Shared inputs that every generator can read.</summary>
    public sealed class TownContext
    {
        public TownContext(TownLayout layout, GenerationSettings settings, BaseTerrain terrain, GroundModel ground)
        {
            Layout = layout;
            Settings = settings;
            Terrain = terrain;
            Ground = ground;
        }

        public TownLayout Layout { get; }

        public GenerationSettings Settings { get; }

        public BaseTerrain Terrain { get; }

        public GroundModel Ground { get; }
    }
}
