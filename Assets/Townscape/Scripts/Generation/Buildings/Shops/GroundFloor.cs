using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>The ground-floor band of a front wall, handed to an <see cref="IGroundFloorStyle"/> to fill.</summary>
    public sealed class GroundFloor
    {
        public GroundFloor(BuildContext context, WallFrame wall, float floor, float height, SurfaceMaterial wallMaterial, WindowStyle windows, SurfaceMaterial doorPaint, ShopDefinition shop)
        {
            Context = context;
            Wall = wall;
            Floor = floor;
            Height = height;
            WallMaterial = wallMaterial;
            Windows = windows;
            DoorPaint = doorPaint;
            Shop = shop;
        }

        public BuildContext Context { get; }

        public WallFrame Wall { get; }

        /// <summary>Height of the ground floor level.</summary>
        public float Floor { get; }

        /// <summary>Height of the band this style fills, from <see cref="Floor"/> up.</summary>
        public float Height { get; }

        public SurfaceMaterial WallMaterial { get; }

        public WindowStyle Windows { get; }

        /// <summary>Colour for a front door that is not part of a shopfront.</summary>
        public SurfaceMaterial DoorPaint { get; }

        /// <summary>The shop on this floor, or null for a house.</summary>
        public ShopDefinition Shop { get; }

        public float Top => Floor + Height;

        public MeshBuilder Builder => Context.Builder;
    }

    /// <summary>Strategy for one kind of ground floor: shopfront, inn, or house front.</summary>
    public interface IGroundFloorStyle
    {
        void Build(GroundFloor floor);
    }
}
