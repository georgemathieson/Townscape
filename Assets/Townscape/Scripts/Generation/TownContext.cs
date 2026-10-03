using System.Collections.Generic;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Terrain;

namespace Townscape.Generation
{
    /// <summary>Shared inputs that every generator can read.</summary>
    public sealed class TownContext
    {
        public TownContext(TownLayout layout, GenerationSettings settings, BaseTerrain terrain, GroundModel ground, IReadOnlyList<BuildingPlan> buildings)
        {
            Layout = layout;
            Settings = settings;
            Terrain = terrain;
            Ground = ground;
            Buildings = buildings;
        }

        public TownLayout Layout { get; }

        public GenerationSettings Settings { get; }

        public BaseTerrain Terrain { get; }

        public GroundModel Ground { get; }

        /// <summary>Every planned building, decided before the ground is generated.</summary>
        public IReadOnlyList<BuildingPlan> Buildings { get; }
    }
}
