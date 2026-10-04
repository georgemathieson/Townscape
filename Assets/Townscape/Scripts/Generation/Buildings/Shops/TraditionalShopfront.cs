using System;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>
    /// The classic British shopfront: pilasters and consoles framing a painted fascia with the
    /// shop's name, a cornice, a display window over a stallriser, and a recessed glazed door.
    /// Wide units get a separate front door to the flats upstairs. In a shop you can go into
    /// (<see cref="ShopDefinition.Enterable"/>) both doors open and the window looks into the shop.
    /// </summary>
    public sealed class TraditionalShopfront : IGroundFloorStyle
    {
        public static readonly TraditionalShopfront Instance = new TraditionalShopfront();

        public const float Pilaster = 0.28f;
        public const float OpeningTop = 2.75f;
        public const float StallRiser = 0.55f;
        public const float GlassDepth = 0.1f;
        public const float LobbyDepth = 0.55f;
        public const float DoorHeight = 2.35f;
        public const float FlatDoorDepth = 0.16f;
        private const float FasciaTop = 3.22f;
        private const float DisplayDepth = 1.35f;
        private const float DoorWidth = 0.95f;

        /// <summary>Where things go across a shopfront <paramref name="width"/> wide.</summary>
        public static ShopfrontLayout Layout(float width)
        {
            var sideDoor = width >= 5.2f;
            var shopRight = width - Pilaster;
            var doorRight = shopRight - 0.08f;
            return new ShopfrontLayout(
                sideDoor ? 1.5f : Pilaster,
                shopRight,
                doorRight - DoorWidth,
                doorRight,
                sideDoor,
                Pilaster + 0.12f,
                Pilaster + 0.97f);
        }

        public void Build(GroundFloor floor)
        {
            var shop = floor.Shop ?? throw new InvalidOperationException("A shopfront needs a shop.");
            var wall = floor.Wall;
            var builder = floor.Builder;
            var f = floor.Floor;
            var width = wall.Width;
            var paint = shop.Paint;

            var layout = Layout(width);
            var sideDoor = layout.FlatDoor;
            var shopLeft = layout.ShopLeft;
            var shopRight = layout.ShopRight;
            var openings = new System.Collections.Generic.List<Opening> { new Opening(shopLeft, f, shopRight, f + OpeningTop, GlassDepth) };
            Opening flatDoor = default;
            if (sideDoor)
            {
                flatDoor = new Opening(layout.FlatDoorLeft, f, layout.FlatDoorRight, f + DoorHeight, FlatDoorDepth);
                openings.Add(flatDoor);
            }

            WallBuilder.Build(builder, wall, f, floor.Top, openings, floor.WallMaterial, paint);

            var doorRight = layout.DoorRight;
            var doorLeft = layout.DoorLeft;
            BuildDoorway(floor, paint, doorLeft, doorRight, shop.Enterable);

            // Mullion between the window and the door.
            wall.Block(builder, doorLeft - 0.1f, f, doorLeft, f + OpeningTop, -GlassDepth, -0.02f, paint);
            BuildWindow(floor, shop, shopLeft, doorLeft - 0.1f);

            if (sideDoor && shop.Enterable)
            {
                // The flat's door opens into the hall; its fanlight stays put.
                Glazing.Fanlight(floor.Context, wall, flatDoor, SurfaceMaterial.PaintWhite);
                var leaf = new Opening(flatDoor.X0, flatDoor.Y0, flatDoor.X1, flatDoor.Y1 - Glazing.FanlightHeight, flatDoor.Depth);
                var hinge = wall.Point(leaf.X0, f, -leaf.Depth - 0.02f);
                floor.Context.Door("Flat door", hinge, wall.Right, -wall.Out, leaf.Width, leaf.Y1 - leaf.Y0, door =>
                {
                    Glazing.FillDoor(door, wall, leaf, floor.DoorPaint, SurfaceMaterial.PaintWhite, fanlight: false);
                    wall.QuadInward(door.Builder, leaf.X0, leaf.Y0, leaf.X1, leaf.Y1, -leaf.Depth - 0.04f, floor.DoorPaint);
                });
                wall.Block(builder, shopLeft - 0.25f, f, shopLeft, f + OpeningTop, 0f, 0.1f, paint);
            }
            else if (sideDoor)
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

        private static void BuildDoorway(GroundFloor floor, SurfaceMaterial paint, float left, float right, bool opens)
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

            void Leaf(MeshBuilder leaf, SurfaceMaterial glass)
            {
                wall.Quad(leaf, left, f, right, f + 1.0f, back, paint);
                wall.Block(leaf, left + 0.1f, f + 0.12f, right - 0.1f, f + 0.88f, back, back + 0.02f, paint);
                wall.Quad(leaf, left, f + 1.0f, right, f + DoorHeight, back, glass);
                wall.Block(leaf, left, f + 1.0f, right, f + 1.06f, back, back + 0.03f, paint);
                wall.Block(leaf, left, f + DoorHeight - 0.06f, right, f + DoorHeight, back, back + 0.03f, paint);
                wall.Block(leaf, left, f + 1.06f, left + 0.07f, f + DoorHeight - 0.06f, back, back + 0.03f, paint);
                wall.Block(leaf, right - 0.07f, f + 1.06f, right, f + DoorHeight - 0.06f, back, back + 0.03f, paint);
            }

            if (opens)
            {
                // A half-glazed door you can see through, hung on the right and opening into the shop.
                var hinge = wall.Point(right, f, back - 0.02f);
                floor.Context.Door("Shop door", hinge, -wall.Right, -wall.Out, right - left, DoorHeight, door =>
                {
                    Leaf(door.Builder, SurfaceMaterial.ShopGlass);
                    wall.QuadInward(door.Builder, left, f, right, f + 1.0f, back - 0.04f, paint);
                    wall.Block(door.Builder, left, f + 1.0f, right, f + 1.06f, back - 0.04f, back, paint);
                    wall.Block(door.Builder, left, f + DoorHeight - 0.06f, right, f + DoorHeight, back - 0.04f, back, paint);
                    wall.Block(door.Builder, left, f + 1.06f, left + 0.07f, f + DoorHeight - 0.06f, back - 0.04f, back, paint);
                    wall.Block(door.Builder, right - 0.07f, f + 1.06f, right, f + DoorHeight - 0.06f, back - 0.04f, back, paint);
                });
            }
            else
            {
                Leaf(builder, SurfaceMaterial.WindowShop);
            }

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

            floor.Context.Anchor(AnchorKind.ShopWindow, wall.Point((left + right) * 0.5f, (bed + top) * 0.5f, -0.6f), wall.Out, right - left);
            if (shop.Enterable)
            {
                // No display box: the window looks into the shop itself.
                return;
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

    /// <summary>Where the parts of a traditional shopfront fall across its width.</summary>
    public readonly struct ShopfrontLayout
    {
        public ShopfrontLayout(float shopLeft, float shopRight, float doorLeft, float doorRight, bool flatDoor, float flatDoorLeft, float flatDoorRight)
        {
            ShopLeft = shopLeft;
            ShopRight = shopRight;
            DoorLeft = doorLeft;
            DoorRight = doorRight;
            FlatDoor = flatDoor;
            FlatDoorLeft = flatDoorLeft;
            FlatDoorRight = flatDoorRight;
        }

        /// <summary>The left edge of the shop's opening (window and door).</summary>
        public float ShopLeft { get; }

        public float ShopRight { get; }

        /// <summary>The shop door, at the back of its lobby.</summary>
        public float DoorLeft { get; }

        public float DoorRight { get; }

        /// <summary>Whether there's a separate front door to the flats upstairs, on the left.</summary>
        public bool FlatDoor { get; }

        public float FlatDoorLeft { get; }

        public float FlatDoorRight { get; }
    }
}
