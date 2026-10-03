using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>
    /// A Lake District inn: whitewashed, small-paned windows with black surrounds either side of a
    /// central door, a long name board, lanterns by the door and a hanging sign.
    /// </summary>
    public sealed class InnFrontage : IGroundFloorStyle
    {
        public static readonly InnFrontage Instance = new InnFrontage();

        public void Build(GroundFloor floor)
        {
            var shop = floor.Shop;
            var wall = floor.Wall;
            var builder = floor.Builder;
            var f = floor.Floor;
            var width = wall.Width;
            var windows = new WindowStyle(SurfaceMaterial.PaintBlack, GlazingPattern.SixOverSix, SurfaceMaterial.Stone, SurfaceMaterial.PaintBlack);

            var door = new Opening((width * 0.5f) - 0.55f, f, (width * 0.5f) + 0.55f, f + 2.3f, 0.2f);
            var left = new Opening(0.55f, f + 0.75f, (width * 0.5f) - 1.15f, f + 2.15f, 0.16f);
            var right = new Opening((width * 0.5f) + 1.15f, f + 0.75f, width - 0.55f, f + 2.15f, 0.16f);
            var openings = new[] { door, left, right };
            WallBuilder.Build(builder, wall, f, floor.Top, openings, floor.WallMaterial);

            Glazing.FillWindow(floor.Context, wall, left, windows, AnchorKind.ShopWindow);
            Glazing.FillWindow(floor.Context, wall, right, windows, AnchorKind.ShopWindow);
            Glazing.FillDoor(floor.Context, wall, door, shop?.Paint ?? SurfaceMaterial.PaintOxblood, SurfaceMaterial.PaintBlack);

            // Black surround to the door and a step.
            wall.Quad(builder, door.X0 - 0.13f, door.Y1, door.X1 + 0.13f, door.Y1 + 0.13f, 0.012f, SurfaceMaterial.PaintBlack);
            wall.Quad(builder, door.X0 - 0.13f, f, door.X0, door.Y1, 0.012f, SurfaceMaterial.PaintBlack);
            wall.Quad(builder, door.X1, f, door.X1 + 0.13f, door.Y1, 0.012f, SurfaceMaterial.PaintBlack);
            wall.Block(builder, door.X0 - 0.2f, f - 0.15f, door.X1 + 0.2f, f, -0.05f, 0.35f, SurfaceMaterial.Stone);

            // Lanterns either side of the door.
            foreach (var x in new[] { door.X0 - 0.4f, door.X1 + 0.4f })
            {
                wall.Block(builder, x - 0.02f, f + 2.05f, x + 0.02f, f + 2.09f, 0f, 0.22f, SurfaceMaterial.PaintBlack);
                wall.Block(builder, x - 0.09f, f + 1.8f, x + 0.09f, f + 2.05f, 0.13f, 0.31f, SurfaceMaterial.ShopGlass);
                wall.Block(builder, x - 0.11f, f + 2.05f, x + 0.11f, f + 2.1f, 0.11f, 0.33f, SurfaceMaterial.PaintBlack);
                floor.Context.Anchor(AnchorKind.DoorLamp, wall.Point(x, f + 1.92f, 0.22f), wall.Out, 0.2f);
            }

            // Name board between the ground and first floors.
            var name = shop?.Name ?? "INN";
            wall.Block(builder, 0.35f, f + 2.5f, width - 0.35f, f + 2.98f, 0f, 0.06f, SurfaceMaterial.PaintBlack);
            PixelFont.Write(builder, wall, name, width * 0.5f, f + 2.74f, 0.045f, width - 1.1f, 0.064f, shop?.Lettering ?? SurfaceMaterial.PaintGold);

            HangingSign.Build(floor.Context, wall, 0.2f, f + 3.3f, shop?.Paint ?? SurfaceMaterial.PaintOxblood, SurfaceMaterial.PaintGold);
        }
    }
}
