using System;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;

namespace Townscape.Generation.Buildings.Planning
{
    /// <summary>Chooses the look of each terraced unit, so a row varies like a real street.</summary>
    public static class TerraceDesigner
    {
        private static readonly SurfaceMaterial[] Walls =
        {
            SurfaceMaterial.Stone, SurfaceMaterial.StoneGreen, SurfaceMaterial.RenderWhite, SurfaceMaterial.RenderCream, SurfaceMaterial.Stone, SurfaceMaterial.RenderWhite,
        };

        private static readonly SurfaceMaterial[] Doors =
        {
            SurfaceMaterial.PaintNavy, SurfaceMaterial.PaintOxblood, SurfaceMaterial.PaintDarkGreen, SurfaceMaterial.PaintBlack,
            SurfaceMaterial.PaintTeal, SurfaceMaterial.PaintSage, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintDuckEgg,
        };

        private static readonly SurfaceMaterial[] Trims = { SurfaceMaterial.PaintBlack, SurfaceMaterial.PaintWhite, SurfaceMaterial.Timber };

        public static UnitDesign Design(TerraceUnit unit, Footprint footprint, Random random)
        {
            var shop = unit.Shop;
            var wall = shop?.Wall ?? Walls[random.Next(Walls.Length)];
            var rendered = wall == SurfaceMaterial.RenderWhite || wall == SurfaceMaterial.RenderCream;
            var frame = shop?.WindowFrames ?? (rendered && random.NextDouble() < 0.35 ? SurfaceMaterial.PaintBlack : SurfaceMaterial.PaintWhite);
            var surround = shop?.Wall != null || (rendered && random.NextDouble() < 0.45) ? SurfaceMaterial.PaintBlack : (SurfaceMaterial?)null;
            var pattern = random.NextDouble() < 0.65 ? GlazingPattern.TwoOverTwo : GlazingPattern.SixOverSix;

            var design = new UnitDesign
            {
                Shop = shop,
                Wall = wall,
                GroundFloorHeight = shop != null ? 3.45f : 3.0f,
                UpperFloors = shop != null ? (random.NextDouble() < 0.7 ? 2 : 1) : (random.NextDouble() < 0.4 ? 2 : 1),
                Windows = new WindowStyle(frame, pattern, rendered ? SurfaceMaterial.StoneDark : SurfaceMaterial.Kerb, surround),
                DoorPaint = Doors[random.Next(Doors.Length)],
                Trim = Trims[random.Next(Trims.Length)],
                Dormer = random.NextDouble() < 0.35,
                Pots = 2 + random.Next(3),
                GroundFloor = shop != null ? (shop.Frontage ?? TraditionalShopfront.Instance) : new HouseFrontage(random.NextDouble() < 0.5),
            };
            design.WindowBoxes = random.NextDouble() < (shop != null ? 0.25 : 0.45);

            var pitch = (37f + (6f * (float)random.NextDouble())) * MathF.PI / 180f;
            design.Eaves = BuildingLevels.Floor + design.GroundFloorHeight + (design.UpperFloors * design.UpperFloorHeight);
            design.Ridge = design.Eaves + (footprint.Depth * 0.5f * MathF.Tan(pitch));
            return design;
        }
    }
}
