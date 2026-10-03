using System;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>
    /// The classic British shopfront: pilasters and consoles framing a painted fascia with the
    /// shop's name, a cornice, a display window over a stallriser, and a recessed glazed door.
    /// Wide units get a separate front door to the flats upstairs.
    /// </summary>
    public sealed class TraditionalShopfront : IGroundFloorStyle
    {
        public static readonly TraditionalShopfront Instance = new TraditionalShopfront();

        private const float Pilaster = 0.28f;
        private const float OpeningTop = 2.75f;
        private const float FasciaTop = 3.22f;
        private const float StallRiser = 0.55f;
        private const float GlassDepth = 0.1f;
        private const float DisplayDepth = 1.35f;
        private const float LobbyDepth = 0.55f;
        private const float DoorWidth = 0.95f;
        private const float DoorHeight = 2.35f;

        public void Build(GroundFloor floor)
        {
            var shop = floor.Shop ?? throw new InvalidOperationException("A shopfront needs a shop.");
            var wall = floor.Wall;
            var builder = floor.Builder;
            var f = floor.Floor;
            var width = wall.Width;
            var paint = shop.Paint;

            var sideDoor = width >= 5.2f;
            var shopLeft = sideDoor ? 1.5f : Pilaster;
            var shopRight = width - Pilaster;
            var openings = new System.Collections.Generic.List<Opening> { new Opening(shopLeft, f, shopRight, f + OpeningTop, GlassDepth) };
            Opening flatDoor = default;
            if (sideDoor)
            {
                flatDoor = new Opening(Pilaster + 0.12f, f, Pilaster + 0.97f, f + DoorHeight, 0.16f);
                openings.Add(flatDoor);
            }

            WallBuilder.Build(builder, wall, f, floor.Top, openings, floor.WallMaterial, paint);

            var doorRight = shopRight - 0.08f;
            var doorLeft = doorRight - DoorWidth;
            BuildDoorway(floor, paint, doorLeft, doorRight);

            // Mullion between the window and the door.
            wall.Block(builder, doorLeft - 0.1f, f, doorLeft, f + OpeningTop, -GlassDepth, -0.02f, paint);
            BuildWindow(floor, shop, shopLeft, doorLeft - 0.1f);

            if (sideDoor)
            {
                Glazing.FillDoor(floor.Context, wall, flatDoor, floor.DoorPaint, SurfaceMaterial.PaintWhite);
                wall.Block(builder, shopLeft - 0.25f, f, shopLeft, f + OpeningTop, 0f, 0.1f, paint);
            }

            BuildFrame(floor, shop);
            if (shop.Awning)
            {
                BuildAwning(floor, shop, shopLeft, shopRight);
            }

            if (shop.HangingSign)
            {
                HangingSign.Build(floor.Context, wall, 0.14f, f + OpeningTop + 0.95f, paint, shop.Lettering);
            }
        }

        private static void BuildDoorway(GroundFloor floor, SurfaceMaterial paint, float left, float right)
        {
            var wall = floor.Wall;
            var builder = floor.Builder;
            var f = floor.Floor;
            var front = -GlassDepth;
            var back = -LobbyDepth;
            var up = Vector3.UnitY;

            // Lobby: tiled floor, panelled sides and ceiling, half-glazed door at the back.
            builder.AddQuadFacing(wall.Point(left, f + 0.005f, front), wall.Point(right, f + 0.005f, front), wall.Point(right, f + 0.005f, back), wall.Point(left, f + 0.005f, back), up, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(wall.Point(left, f, front), wall.Point(left, f + DoorHeight, front), wall.Point(left, f + DoorHeight, back), wall.Point(left, f, back), wall.Right, paint);
            builder.AddQuadFacing(wall.Point(right, f, front), wall.Point(right, f + DoorHeight, front), wall.Point(right, f + DoorHeight, back), wall.Point(right, f, back), -wall.Right, paint);
            builder.AddQuadFacing(wall.Point(left, f + DoorHeight, front), wall.Point(right, f + DoorHeight, front), wall.Point(right, f + DoorHeight, back), wall.Point(left, f + DoorHeight, back), -up, paint);

            wall.Quad(builder, left, f, right, f + 1.0f, back, paint);
            wall.Block(builder, left + 0.1f, f + 0.12f, right - 0.1f, f + 0.88f, back, back + 0.02f, paint);
            wall.Quad(builder, left, f + 1.0f, right, f + DoorHeight, back, SurfaceMaterial.WindowShop);
            wall.Block(builder, left, f + 1.0f, right, f + 1.06f, back, back + 0.03f, paint);
            wall.Block(builder, left, f + DoorHeight - 0.06f, right, f + DoorHeight, back, back + 0.03f, paint);
            wall.Block(builder, left, f + 1.06f, left + 0.07f, f + DoorHeight - 0.06f, back, back + 0.03f, paint);
            wall.Block(builder, right - 0.07f, f + 1.06f, right, f + DoorHeight - 0.06f, back, back + 0.03f, paint);

            // Transom light over the lobby.
            wall.Quad(builder, left, f + DoorHeight, right, f + OpeningTop, front, SurfaceMaterial.WindowShop);
            wall.Block(builder, left, f + DoorHeight, right, f + DoorHeight + 0.07f, front, front + 0.04f, paint);
        }

        private static void BuildWindow(GroundFloor floor, ShopDefinition shop, float left, float right)
        {
            var wall = floor.Wall;
            var builder = floor.Builder;
            var f = floor.Floor;
            var paint = shop.Paint;
            var bed = f + StallRiser;
            var top = f + OpeningTop;

            // Stallriser with a raised panel.
            wall.Quad(builder, left, f, right, bed, -0.03f, paint);
            wall.Block(builder, left + 0.12f, f + 0.1f, right - 0.12f, bed - 0.1f, -0.03f, -0.01f, paint);
            wall.Block(builder, left, bed - 0.04f, right, bed, -0.03f, 0.03f, paint);

            // Glass with its frame, mullions and transom.
            wall.Quad(builder, left, bed, right, top, -0.08f, shop.Display == null ? SurfaceMaterial.WindowGlass : SurfaceMaterial.ShopGlass);
            const float bar = 0.06f;
            wall.Block(builder, left, bed, left + bar, top, -0.1f, -0.04f, paint);
            wall.Block(builder, right - bar, bed, right, top, -0.1f, -0.04f, paint);
            wall.Block(builder, left, top - bar, right, top, -0.1f, -0.04f, paint);
            wall.Block(builder, left, top - 0.48f, right, top - 0.44f, -0.1f, -0.05f, paint);
            var panes = Math.Max(1, (int)MathF.Round((right - left) / 1.2f));
            for (var i = 1; i < panes; i++)
            {
                var x = left + ((right - left) * i / panes);
                wall.Block(builder, x - 0.025f, bed, x + 0.025f, top, -0.1f, -0.05f, paint);
            }

            // The display space behind the glass.
            var box = new DisplayBox(floor.Context, wall, left, right, bed, top, GlassDepth, DisplayDepth);
            var back = -DisplayDepth;
            var up = Vector3.UnitY;
            wall.Quad(builder, left, bed, right, top, back, SurfaceMaterial.Interior);
            builder.AddQuadFacing(wall.Point(left, bed, -0.03f), wall.Point(right, bed, -0.03f), wall.Point(right, bed, back), wall.Point(left, bed, back), up, SurfaceMaterial.InteriorFloor);
            builder.AddQuadFacing(wall.Point(left, top, -GlassDepth), wall.Point(right, top, -GlassDepth), wall.Point(right, top, back), wall.Point(left, top, back), -up, SurfaceMaterial.Interior);
            builder.AddQuadFacing(wall.Point(left, bed, -GlassDepth), wall.Point(left, top, -GlassDepth), wall.Point(left, top, back), wall.Point(left, bed, back), wall.Right, SurfaceMaterial.Interior);
            builder.AddQuadFacing(wall.Point(right, bed, -GlassDepth), wall.Point(right, top, -GlassDepth), wall.Point(right, top, back), wall.Point(right, bed, back), -wall.Right, SurfaceMaterial.Interior);
            shop.Display?.Furnish(box);

            floor.Context.Anchor(AnchorKind.ShopWindow, wall.Point((left + right) * 0.5f, (bed + top) * 0.5f, -0.6f), wall.Out, right - left);
        }

        private static void BuildFrame(GroundFloor floor, ShopDefinition shop)
        {
            var wall = floor.Wall;
            var builder = floor.Builder;
            var f = floor.Floor;
            var width = wall.Width;
            var paint = shop.Paint;

            // Pilasters with consoles at the top.
            foreach (var x in new[] { 0f, width - Pilaster })
            {
                wall.Block(builder, x, f, x + Pilaster, f + OpeningTop, 0f, 0.1f, paint);
                wall.Block(builder, x + 0.02f, f + OpeningTop, x + Pilaster - 0.02f, FasciaTop + f, 0f, 0.2f, paint);
            }

            // Fascia with lettering, and the cornice above it.
            wall.Block(builder, Pilaster, f + OpeningTop + 0.03f, width - Pilaster, f + FasciaTop - 0.02f, 0f, 0.12f, paint);
            PixelFont.Write(builder, wall, shop.Name, width * 0.5f, f + ((OpeningTop + FasciaTop) * 0.5f) + 0.005f, 0.045f, width - (2f * Pilaster) - 0.4f, 0.124f, shop.Lettering);
            wall.Block(builder, -0.04f, f + FasciaTop, width + 0.04f, f + FasciaTop + 0.1f, 0f, 0.24f, paint);
        }

        private static void BuildAwning(GroundFloor floor, ShopDefinition shop, float left, float right)
        {
            var wall = floor.Wall;
            var builder = floor.Builder;
            var top = floor.Floor + OpeningTop + 0.02f;
            var bottom = floor.Floor + 2.3f;
            const float reach = 1.3f;
            const float stripe = 0.32f;
            var up = Vector3.UnitY;

            var index = 0;
            for (var x = left; x < right - 1e-3f; x += stripe, index++)
            {
                var x1 = Math.Min(right, x + stripe);
                var colour = index % 2 == 0 ? shop.Paint : shop.AwningStripe;
                var a = wall.Point(x, top, 0.13f);
                var b = wall.Point(x1, top, 0.13f);
                var c = wall.Point(x1, bottom, reach);
                var d = wall.Point(x, bottom, reach);
                builder.AddQuadFacing(a, b, c, d, up + wall.Out, colour);
                builder.AddQuadFacing(a, b, c, d, -up - wall.Out, colour);

                // Valance hanging from the front edge.
                builder.AddQuadFacing(d, c, wall.Point(x1, bottom - 0.2f, reach), wall.Point(x, bottom - 0.2f, reach), wall.Out, colour);
                builder.AddQuadFacing(d, c, wall.Point(x1, bottom - 0.2f, reach), wall.Point(x, bottom - 0.2f, reach), -wall.Out, colour);
            }
        }
    }

    /// <summary>A small board hanging from an iron bracket, at right angles to the wall.</summary>
    public static class HangingSign
    {
        public static void Build(BuildContext context, WallFrame wall, float x, float y, SurfaceMaterial board, SurfaceMaterial border)
        {
            var builder = context.Builder;
            wall.Block(builder, x - 0.025f, y + 0.55f, x + 0.025f, y + 0.6f, 0f, 1.0f, SurfaceMaterial.PaintBlack);
            wall.Block(builder, x - 0.015f, y + 0.5f, x + 0.015f, y + 0.55f, 0.3f, 0.34f, SurfaceMaterial.PaintBlack);
            wall.Block(builder, x - 0.015f, y + 0.5f, x + 0.015f, y + 0.55f, 0.86f, 0.9f, SurfaceMaterial.PaintBlack);
            wall.Block(builder, x - 0.03f, y - 0.05f, x + 0.03f, y + 0.5f, 0.25f, 0.95f, border);
            wall.Block(builder, x - 0.04f, y, x + 0.04f, y + 0.45f, 0.3f, 0.9f, board);
        }
    }
}
