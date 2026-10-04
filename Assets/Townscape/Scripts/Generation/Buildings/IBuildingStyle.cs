using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings
{
    /// <summary>Strategy for building one kind of building on a footprint: a terraced shop, a cottage, a church.</summary>
    public interface IBuildingStyle
    {
        void Build(Footprint footprint, BuildContext context);
    }

    /// <summary>
    /// A building style that wants its plot surfaced with something other than flagstones, like a
    /// petrol station's concrete forecourt.
    /// </summary>
    public interface IYardFinish
    {
        SurfaceMaterial Yard { get; }
    }

    /// <summary>Shared levels so every building sits on the ground the same way.</summary>
    public static class BuildingLevels
    {
        /// <summary>Ground floor level, a step above the pavement.</summary>
        public const float Floor = 0.15f;

        /// <summary>Walls start this far down so they never float above uneven ground.</summary>
        public const float Base = -0.45f;
    }
}
