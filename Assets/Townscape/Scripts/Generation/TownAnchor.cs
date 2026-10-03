using System.Numerics;

namespace Townscape.Generation
{
    public enum AnchorKind
    {
        /// <summary>Top of a chimney pot, for smoke.</summary>
        Chimney,

        /// <summary>Middle of an upstairs window, for lit windows at night.</summary>
        Window,

        /// <summary>Middle of a shop display, for the warm glow of a shop window.</summary>
        ShopWindow,

        /// <summary>A lamp over a door or a pub sign.</summary>
        DoorLamp,

        /// <summary>The lantern of a street lamp.</summary>
        StreetLamp,

        /// <summary>The orange globe of a Belisha beacon at a zebra crossing.</summary>
        Beacon,

        /// <summary>A sign that is lit from inside, like the TELEPHONE sign on a phone box.</summary>
        LitSign,

        /// <summary>The glazed fanlight over a front door.</summary>
        Fanlight,
    }

    /// <summary>
    /// A point of interest left by a generator for later systems: lights, smoke, lit windows.
    /// Generators stay engine-free; the Unity layer decides what to put at each anchor.
    /// </summary>
    public readonly struct TownAnchor
    {
        public TownAnchor(AnchorKind kind, Vector3 position, Vector3 facing, float size, int seed)
        {
            Kind = kind;
            Position = position;
            Facing = facing;
            Size = size;
            Seed = seed;
        }

        public AnchorKind Kind { get; }

        public Vector3 Position { get; }

        /// <summary>Direction the anchor faces (out of a window, up a chimney).</summary>
        public Vector3 Facing { get; }

        public float Size { get; }

        /// <summary>Per-anchor random seed, so lights can vary consistently.</summary>
        public int Seed { get; }
    }
}
