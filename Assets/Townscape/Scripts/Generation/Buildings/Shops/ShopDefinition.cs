using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>Everything that makes one shop itself: its sign, paint colours and window display.</summary>
    public sealed class ShopDefinition
    {
        public ShopDefinition(string name, SurfaceMaterial paint, IShopDisplay display, SurfaceMaterial lettering = SurfaceMaterial.PaintGold)
        {
            Name = name;
            Paint = paint;
            Display = display;
            Lettering = lettering;
        }

        /// <summary>Text on the fascia sign (the pixel font supports A–Z, 0–9 and &amp; ' . - +).</summary>
        public string Name { get; }

        /// <summary>Colour of the shopfront woodwork: pilasters, fascia, frames and door.</summary>
        public SurfaceMaterial Paint { get; }

        public SurfaceMaterial Lettering { get; }

        public IShopDisplay Display { get; }

        /// <summary>Striped canvas awning over the window.</summary>
        public bool Awning { get; init; }

        public SurfaceMaterial AwningStripe { get; init; } = SurfaceMaterial.PaintCream;

        /// <summary>Small sign hanging from a bracket, seen down the street.</summary>
        public bool HangingSign { get; init; }

        /// <summary>Overrides the building's wall finish (an inn is whitewashed, say).</summary>
        public SurfaceMaterial? Wall { get; init; }

        /// <summary>Overrides the upstairs window frames (an inn has black frames).</summary>
        public SurfaceMaterial? WindowFrames { get; init; }

        /// <summary>
        /// You can go inside: the shop and the flat above are built with rooms, stairs and
        /// furniture, their doors open, and their windows are clear glass.
        /// </summary>
        public bool Enterable { get; init; }

        /// <summary>The sign leaves room at its right-hand end for a burglar alarm's bell box.</summary>
        public bool BellBox { get; init; }

        /// <summary>How the ground floor is laid out. Null means a traditional shopfront.</summary>
        public IGroundFloorStyle Frontage { get; init; }
    }
}
