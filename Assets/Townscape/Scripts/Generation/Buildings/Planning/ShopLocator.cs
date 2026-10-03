using System.Collections.Generic;
using System.Linq;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Buildings.Styles;

namespace Townscape.Generation.Buildings.Planning
{
    /// <summary>Finds where a shop is in the village, so the camera can go to it.</summary>
    public static class ShopLocator
    {
        /// <summary>The building with <paramref name="shop"/> on its ground floor, or null if the village doesn't have it.</summary>
        public static BuildingPlan Find(IEnumerable<BuildingPlan> buildings, ShopDefinition shop) =>
            buildings.FirstOrDefault(plan => plan.Style is TerracedUnitStyle terraced && terraced.Design.Shop == shop);
    }
}
