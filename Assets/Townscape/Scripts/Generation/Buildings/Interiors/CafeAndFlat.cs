using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Interiors
{
    /// <summary>
    /// The inside of a terraced café with a flat above, for a unit you can walk into. On the
    /// ground floor the café runs across most of the width with a storeroom behind it, and a
    /// narrow hall runs up the left from the flat's front door. A dog-leg stair climbs the left
    /// side: up the hall to a landing at the back of the first floor, forward to the front of the
    /// second, and back again into the attic. The first floor has a living room and a kitchen,
    /// the second a bedroom and a bathroom, and the attic is one room under the slates, lit by
    /// the dormer.
    /// </summary>
    /// <remarks>
    /// Laid out in <see cref="UnitSpace"/> metres for a unit about 7 m wide and 9.5 m deep, which
    /// is what <see cref="Fits"/> checks. Walls are 0.15 m thick inside the shell; floors are
    /// 0.12 m thick.
    /// </remarks>
    public static class CafeAndFlat
    {
        public const float MinimumWidth = 6.6f;
        public const float MinimumDepth = 8.8f;

        // Inner faces of the shell.
        private const float Side = 0.15f;
        private const float FrontFace = 0.16f;
        private const float Slab = 0.12f;

        // The stair strips up the left: A next to the party wall, B beside it.
        private const float StripA = 1.35f;
        private const float StripB = 2.45f;
        private const float HallWall = 1.45f;
        private const float CafeLeft = 1.5f;

        // Where the flights start and finish, back from the front.
        private const float FirstFoot = 1.3f;
        private const float Turn = 5.8f;
        private const float UpperFoot = 1.6f;
        private const float BackLanding = 7.0f;

        // The partitions.
        private const float StoreWall = 6.6f;
        private const float RoomsWall = 4.9f;
        private const float Thin = 0.1f;

        public static bool Fits(Footprint footprint) => footprint.FrontWidth >= MinimumWidth && footprint.Depth >= MinimumDepth;

        /// <summary>The levels of the floors, from the ground floor up to the attic.</summary>
        public static float[] Floors(UnitDesign design)
        {
            var ground = BuildingLevels.Floor;
            var first = ground + design.GroundFloorHeight;
            return new[] { ground, first, first + design.UpperFloorHeight, first + (2f * design.UpperFloorHeight) };
        }

        /// <summary>The three flights, from the ground floor to the attic.</summary>
        public static StairFlight[] Flights(UnitDesign design)
        {
            var f = Floors(design);
            return new[]
            {
                new StairFlight(Side, StripA, FirstFoot, Turn, f[0], f[1], 18),
                new StairFlight(StripA, StripB, Turn, UpperFoot, f[1], f[2], 14),
                new StairFlight(Side, StripA, UpperFoot, Turn, f[2], f[3], 14),
            };
        }

        public static void Build(BuildContext context, Footprint footprint, UnitDesign design, GableRoof roof, Dormer.Span? dormer)
        {
            var space = new UnitSpace(footprint);
            var builder = context.Builder;
            var f = Floors(design);
            var flights = Flights(design);
            var w = space.Width;
            var back = space.Depth - FrontFace;
            var right = w - Side;
            var wallTop = new[] { f[1] - Slab, f[2] - Slab, f[3] - Slab };

            // Floors and ceilings, each with a hole over the flight that climbs through it.
            Floor(space, builder, f[0], Side, FrontFace, right, back, null, SurfaceMaterial.InteriorFloor);
            Tiles(space, builder, f[0] + 0.004f, CafeLeft, FrontFace, right, StoreWall);
            Slabs(space, builder, f[1], Side, FrontFace, right, back, Hole(flights[0]));
            Slabs(space, builder, f[2], Side, FrontFace, right, back, Hole(flights[1]));
            Slabs(space, builder, f[3], Side, FrontFace, right, back, Hole(flights[2]));

            foreach (var flight in flights)
            {
                flight.Build(space, builder, SurfaceMaterial.Timber, SurfaceMaterial.Interior);
            }

            GroundFloor(context, space, design, f, wallTop[0]);
            UpperFloor(context, space, design, f[1], wallTop[1], firstFloor: true);
            UpperFloor(context, space, design, f[2], wallTop[2], firstFloor: false);
            Banisters(space, builder, flights, f);
            Attic(context, space, roof, f[3], dormer);
        }

        // ---- Ground floor -------------------------------------------------------------------

        private static void GroundFloor(BuildContext context, UnitSpace space, UnitDesign design, float[] f, float top)
        {
            var builder = context.Builder;
            var w = space.Width;
            var right = w - Side;
            var back = space.Depth - FrontFace;
            var floor = f[0];
            var plaster = SurfaceMaterial.Interior;
            var layout = TraditionalShopfront.Layout(w);
            var shopTop = floor + TraditionalShopfront.OpeningTop;
            var doorTop = floor + TraditionalShopfront.DoorHeight;
            var lobby = TraditionalShopfront.LobbyDepth;

            // The café's front: above the shop opening, under the window, round the lobby, and
            // the strip behind the right-hand pilaster.
            space.WallAcross(builder, FrontFace, CafeLeft, right, shopTop, top, true, plaster);
            space.WallAcross(builder, FrontFace, CafeLeft, layout.DoorLeft - 0.1f, floor, floor + TraditionalShopfront.StallRiser, true, SurfaceMaterial.Timber);
            space.Box(builder, CafeLeft, floor + TraditionalShopfront.StallRiser, TraditionalShopfront.GlassDepth, layout.DoorLeft - 0.1f, floor + TraditionalShopfront.StallRiser + 0.03f, FrontFace + 0.12f, SurfaceMaterial.Timber);
            space.WallAcross(builder, FrontFace, layout.DoorRight, right, floor, shopTop, true, plaster);
            space.WallAlong(builder, layout.DoorLeft, FrontFace, lobby, floor, shopTop, false, plaster);
            space.WallAlong(builder, layout.DoorRight, FrontFace, lobby, floor, shopTop, true, plaster);
            space.WallAcross(builder, lobby, layout.DoorLeft, layout.DoorRight, doorTop, shopTop, true, plaster);
            space.Ceiling(builder, layout.DoorLeft, FrontFace, layout.DoorRight, lobby, shopTop, plaster);

            // The hall: the inside of the front round the flat's door, and the wall to the café.
            space.WallAcross(builder, FrontFace, Side, HallWall, floor, top, true, plaster,
                new Hole(layout.FlatDoorLeft, floor, layout.FlatDoorRight, floor + TraditionalShopfront.DoorHeight));
            space.WallAlong(builder, HallWall, FrontFace, back, floor, top, false, plaster);
            space.WallAlong(builder, CafeLeft, FrontFace, back, floor, top, true, plaster);
            space.Box(builder, Side, floor, FrontFace - 0.04f, layout.FlatDoorLeft, floor + TraditionalShopfront.DoorHeight, FrontFace, plaster);
            space.Box(builder, layout.FlatDoorRight, floor, FrontFace - 0.04f, HallWall, floor + TraditionalShopfront.DoorHeight, FrontFace, plaster);

            // Party walls, the back, and the wall between the café and its storeroom.
            space.WallAlong(builder, Side, FrontFace, back, floor, top, true, plaster);
            space.WallAlong(builder, right, FrontFace, back, floor, top, false, plaster);
            var storeDoor = new Hole(CafeLeft + 0.5f, floor, CafeLeft + 1.4f, floor + 2.1f);
            space.WallAcross(builder, StoreWall, CafeLeft, right, floor, top, false, plaster, storeDoor);
            space.WallAcross(builder, StoreWall + Thin, CafeLeft, right, floor, top, true, plaster, storeDoor);
            space.Ceiling(builder, CafeLeft, StoreWall, CafeLeft + 1.4f, StoreWall + Thin, floor + 2.1f, plaster);
            space.WallAcross(builder, back, Side, right, floor, top, false, plaster, BackWindow(space, floor, ground: true));

            // The storeroom's back door, shut, and a light in each space.
            var backDoorX = w * 0.3f;
            space.Box(builder, backDoorX - 0.45f, floor, back - 0.04f, backDoorX + 0.45f, floor + 2.1f, back, design.DoorPaint);
            Light(context, space, (CafeLeft + right) * 0.5f, top, 3.6f, AnchorKind.ShopWindow);
            Light(context, space, (CafeLeft + right) * 0.5f, top, (StoreWall + back) * 0.5f, AnchorKind.RoomLight);
            Light(context, space, (Side + HallWall) * 0.5f, top, 0.8f, AnchorKind.RoomLight);

            Cafe(context, space, floor, right);
            Storeroom(context, space, floor, right, back);
        }

        private static void Cafe(BuildContext context, UnitSpace space, float floor, float right)
        {
            var builder = context.Builder;

            // The serving counter across the back of the café, with the till and the cake cabinet.
            const float counterFront = 4.6f;
            const float counterBack = 5.2f;
            var counterLeft = 3.2f;
            var counterRight = right - 0.55f;
            space.Box(builder, counterLeft, floor, counterFront, counterRight, floor + 0.98f, counterBack, SurfaceMaterial.PaintSage);
            space.Box(builder, counterLeft - 0.03f, floor + 0.98f, counterFront - 0.05f, counterRight + 0.03f, floor + 1.03f, counterBack + 0.02f, SurfaceMaterial.Timber);
            var y = floor + 1.03f;

            // Till: a dark box with a lit screen turned to the customer.
            space.Box(builder, counterRight - 0.7f, y, counterFront + 0.15f, counterRight - 0.3f, y + 0.12f, counterFront + 0.45f, SurfaceMaterial.PaintBlack);
            space.Box(builder, counterRight - 0.65f, y + 0.12f, counterFront + 0.32f, counterRight - 0.35f, y + 0.32f, counterFront + 0.36f, SurfaceMaterial.PaintBlack);
            space.Box(builder, counterRight - 0.62f, y + 0.15f, counterFront + 0.315f, counterRight - 0.38f, y + 0.3f, counterFront + 0.32f, SurfaceMaterial.Screen);

            // Cake cabinet: a glass box on a plinth, with cakes on two shelves.
            var cabinetLeft = counterLeft + 0.15f;
            var cabinetRight = cabinetLeft + 1.3f;
            space.Box(builder, cabinetLeft, y, counterFront + 0.05f, cabinetRight, y + 0.06f, counterBack - 0.05f, SurfaceMaterial.PaintWhite);
            space.Box(builder, cabinetLeft + 0.02f, y + 0.3f, counterFront + 0.07f, cabinetRight - 0.02f, y + 0.32f, counterBack - 0.07f, SurfaceMaterial.ShopGlass);
            GlassBox(space, builder, cabinetLeft, y + 0.06f, counterFront + 0.05f, cabinetRight, y + 0.6f, counterBack - 0.05f);
            for (var shelf = 0; shelf < 2; shelf++)
            {
                var shelfY = y + 0.06f + (shelf * 0.26f);
                for (var i = 0; i < 4; i++)
                {
                    var x = cabinetLeft + 0.18f + (i * 0.31f);
                    var d = (counterFront + counterBack) * 0.5f;
                    var radius = 0.1f + (0.02f * ((i + shelf) % 2));
                    space.Round(builder, x, shelfY, d, radius, 0.1f, SurfaceMaterial.Sponge);
                    space.Round(builder, x, shelfY + 0.1f, d, radius, 0.025f, (i + shelf) % 3 == 0 ? SurfaceMaterial.PaintBlack : SurfaceMaterial.Icing);
                }
            }

            // The back counter, against the storeroom wall: espresso machine, grinder and cups.
            const float backFront = 6.05f;
            const float wallFace = StoreWall;
            var backLeft = CafeLeft + 1.6f;
            space.Box(builder, backLeft, floor, backFront, right, floor + 0.92f, wallFace, SurfaceMaterial.PaintSage);
            space.Box(builder, backLeft, floor + 0.92f, backFront - 0.03f, right, floor + 0.96f, wallFace, SurfaceMaterial.Timber);
            var top = floor + 0.96f;
            var machine = (backLeft + right) * 0.5f + 0.4f;
            space.Box(builder, machine - 0.4f, top, backFront + 0.08f, machine + 0.4f, top + 0.45f, wallFace - 0.02f, SurfaceMaterial.Chrome);
            space.Box(builder, machine - 0.4f, top + 0.45f, backFront + 0.1f, machine + 0.4f, top + 0.5f, wallFace - 0.02f, SurfaceMaterial.PaintOxblood);
            foreach (var head in new[] { machine - 0.2f, machine + 0.2f })
            {
                space.Box(builder, head - 0.05f, top + 0.24f, backFront + 0.02f, head + 0.05f, top + 0.3f, backFront + 0.1f, SurfaceMaterial.PaintBlack);
                space.Round(builder, head, top, backFront + 0.12f, 0.035f, 0.07f, SurfaceMaterial.Porcelain, 6);
            }

            space.Round(builder, machine - 0.75f, top, wallFace - 0.2f, 0.09f, 0.32f, SurfaceMaterial.PaintBlack, 8);
            space.Round(builder, machine - 0.75f, top + 0.32f, wallFace - 0.2f, 0.12f, 0.12f, SurfaceMaterial.ShopGlass, 8);
            for (var i = 0; i < 5; i++)
            {
                space.Round(builder, backLeft + 0.2f + (i * 0.16f), top, backFront + 0.25f, 0.045f, 0.09f, SurfaceMaterial.Porcelain, 6);
            }

            // Shelves of jars above the back counter, and the chalkboard menu.
            foreach (var shelfY in new[] { floor + 1.75f, floor + 2.15f })
            {
                space.Box(builder, backLeft, shelfY, wallFace - 0.25f, backLeft + 1.6f, shelfY + 0.03f, wallFace, SurfaceMaterial.Timber);
                for (var i = 0; i < 6; i++)
                {
                    space.Round(builder, backLeft + 0.15f + (i * 0.25f), shelfY + 0.03f, wallFace - 0.12f, 0.06f, 0.16f, i % 2 == 0 ? SurfaceMaterial.Cardboard : SurfaceMaterial.ShopGlass, 8);
                }
            }

            var boardLeft = machine - 0.75f;
            var boardRight = right - 0.1f;
            var board = WallFrame.FromBase(space.At(boardLeft, 0f, wallFace - 0.03f).ToFlat(), space.At(boardRight, 0f, wallFace - 0.03f).ToFlat());
            board.Block(builder, 0f, floor + 1.55f, board.Width, floor + 2.45f, 0f, 0.03f, SurfaceMaterial.Timber);
            board.Quad(builder, 0.05f, floor + 1.6f, board.Width - 0.05f, floor + 2.4f, 0.035f, SurfaceMaterial.Chalkboard);
            PixelFont.Write(builder, board, "COFFEE", board.Width * 0.5f, floor + 2.22f, 0.03f, board.Width - 0.3f, 0.04f, SurfaceMaterial.PaintWhite);
            PixelFont.Write(builder, board, "TEA 2.50", board.Width * 0.5f, floor + 1.98f, 0.022f, board.Width - 0.3f, 0.04f, SurfaceMaterial.PaintCream);
            PixelFont.Write(builder, board, "CAKE 3.20", board.Width * 0.5f, floor + 1.76f, 0.022f, board.Width - 0.3f, 0.04f, SurfaceMaterial.PaintCream);

            // Tables with chairs round them, and a bar along the window with stools.
            foreach (var (x, d) in new[] { (2.3f, 1.6f), (2.3f, 3.5f), (4.2f, 2.6f) })
            {
                Table(space, builder, x, floor, d, 0.36f);
                Chair(space, builder, x - 0.62f, floor, d, 1f, 0f, SurfaceMaterial.Timber);
                Chair(space, builder, x + 0.62f, floor, d, -1f, 0f, SurfaceMaterial.Timber);
            }

            var layout = TraditionalShopfront.Layout(space.Width);
            var barRight = layout.DoorLeft - 0.25f;
            space.Box(builder, CafeLeft + 0.1f, floor + 1.02f, FrontFace + 0.05f, barRight, floor + 1.06f, FrontFace + 0.4f, SurfaceMaterial.Timber);
            for (var x = CafeLeft + 0.5f; x < barRight - 0.2f; x += 0.75f)
            {
                space.Round(builder, x, floor, FrontFace + 0.7f, 0.03f, 0.7f, SurfaceMaterial.Iron, 6);
                space.Round(builder, x, floor + 0.7f, FrontFace + 0.7f, 0.17f, 0.05f, SurfaceMaterial.FabricRust, 10);
            }

            // A potted plant in the corner by the storeroom door, clear of the shop door's swing.
            space.Round(builder, CafeLeft + 0.3f, floor, StoreWall - 0.6f, 0.18f, 0.35f, SurfaceMaterial.PaintOxblood, 8);
            builder.AddBlob(space.At(CafeLeft + 0.3f, floor + 0.7f, StoreWall - 0.6f), new Vector3(0.3f, 0.4f, 0.3f), SurfaceMaterial.LeafGreen, context.Random, 0.15f);
        }

        private static void Storeroom(BuildContext context, UnitSpace space, float floor, float right, float back)
        {
            var builder = context.Builder;
            var near = StoreWall + Thin;

            // Shelving down the right with boxes, sacks of beans on the floor, and a fridge.
            for (var level = 0; level < 3; level++)
            {
                var y = floor + 0.3f + (level * 0.6f);
                space.Box(builder, right - 0.5f, y, near + 0.2f, right, y + 0.03f, back - 0.2f, SurfaceMaterial.Timber);
                for (var d = near + 0.4f; d < back - 0.4f; d += 0.5f)
                {
                    var tall = 0.2f + (0.15f * (float)context.Random.NextDouble());
                    space.Box(builder, right - 0.45f, y + 0.03f, d - 0.18f, right - 0.08f, y + 0.03f + tall, d + 0.18f, SurfaceMaterial.Cardboard);
                }
            }

            foreach (var d in new[] { near + 0.5f, near + 1.0f })
            {
                space.Box(builder, right - 1.45f, floor, d - 0.2f, right - 0.85f, floor + 0.45f, d + 0.2f, SurfaceMaterial.Linen);
            }

            space.Box(builder, CafeLeft + 1.6f, floor, back - 0.7f, CafeLeft + 2.3f, floor + 1.85f, back - 0.05f, SurfaceMaterial.Chrome);
        }

        // ---- Upper floors -------------------------------------------------------------------

        private static void UpperFloor(BuildContext context, UnitSpace space, UnitDesign design, float floor, float top, bool firstFloor)
        {
            var builder = context.Builder;
            var w = space.Width;
            var right = w - Side;
            var back = space.Depth - FrontFace;
            var plaster = SurfaceMaterial.Interior;

            space.WallAcross(builder, FrontFace, Side, right, floor, top, true, plaster, FrontWindows(space, floor, firstFloor));
            space.WallAcross(builder, back, Side, right, floor, top, false, plaster, BackWindow(space, floor, ground: false));
            space.WallAlong(builder, Side, FrontFace, back, floor, top, true, plaster);
            space.WallAlong(builder, right, FrontFace, back, floor, top, false, plaster);

            // The wall between the stairs and the rooms, with a doorway from the landing: at the
            // back on the first floor, at the front on the second.
            var doorway = firstFloor ? new Hole(Turn + 0.2f, floor, BackLanding - 0.1f, floor + 2.05f) : new Hole(FrontFace + 0.3f, floor, UpperFoot - 0.1f, floor + 2.05f);
            space.WallAlong(builder, StripB, FrontFace, back, floor, top, false, plaster, doorway);
            space.WallAlong(builder, StripB + Thin, FrontFace, back, floor, top, true, plaster, doorway);
            space.Ceiling(builder, StripB, doorway.From, StripB + Thin, doorway.To, doorway.Top, plaster);

            // The wall between the front and back rooms, with a doorway.
            var between = new Hole(StripB + 0.6f, floor, StripB + 1.5f, floor + 2.05f);
            space.WallAcross(builder, RoomsWall, StripB + Thin, right, floor, top, false, plaster, between);
            space.WallAcross(builder, RoomsWall + Thin, StripB + Thin, right, floor, top, true, plaster, between);
            space.Ceiling(builder, between.From, RoomsWall, between.To, RoomsWall + Thin, between.Top, plaster);

            // The first floor's back landing ends at a wall; the space behind is a cupboard.
            if (firstFloor)
            {
                space.WallAcross(builder, BackLanding, Side, StripB, floor, top, false, plaster);
            }

            var roomsCentre = (StripB + Thin + right) * 0.5f;
            Light(context, space, roomsCentre, top, (FrontFace + RoomsWall) * 0.5f, AnchorKind.RoomLight);
            Light(context, space, roomsCentre, top, (RoomsWall + back) * 0.5f, AnchorKind.RoomLight);
            Light(context, space, (Side + StripB) * 0.5f, top, firstFloor ? (Turn + BackLanding) * 0.5f : 0.9f, AnchorKind.RoomLight);

            if (firstFloor)
            {
                LivingRoom(context, space, floor, right);
                Kitchen(context, space, floor, right, back);
            }
            else
            {
                Bedroom(context, space, floor, right);
                Bathroom(context, space, floor, right, back);
            }
        }

        private static void LivingRoom(BuildContext context, UnitSpace space, float floor, float right)
        {
            var builder = context.Builder;
            var left = StripB + Thin;

            // Rug, sofa against the back wall facing the windows, armchair, coffee table.
            space.Box(builder, left + 0.7f, floor, 1.4f, right - 0.6f, floor + 0.012f, 3.7f, SurfaceMaterial.FabricRust);
            Sofa(space, builder, left + 0.9f, right - 1.1f, floor, RoomsWall - 0.95f, RoomsWall - 0.05f, SurfaceMaterial.FabricSage);
            Armchair(space, builder, right - 0.9f, floor, 2.2f, SurfaceMaterial.FabricNavy);
            space.Box(builder, left + 1.3f, floor + 0.38f, 2.3f, right - 1.8f, floor + 0.42f, 3.0f, SurfaceMaterial.Timber);
            space.Box(builder, left + 1.4f, floor, 2.35f, right - 1.9f, floor + 0.38f, 2.95f, SurfaceMaterial.Timber);

            // Television on a low cabinet between the front windows.
            var tv = (4.04f + 5.39f) * 0.5f * space.Width / 7.07f;
            space.Box(builder, tv - 0.55f, floor, FrontFace + 0.02f, tv + 0.55f, floor + 0.5f, FrontFace + 0.45f, SurfaceMaterial.Timber);
            space.Box(builder, tv - 0.45f, floor + 0.5f, FrontFace + 0.15f, tv + 0.45f, floor + 1.05f, FrontFace + 0.2f, SurfaceMaterial.PaintBlack);
            space.Box(builder, tv - 0.42f, floor + 0.53f, FrontFace + 0.2f, tv + 0.42f, floor + 1.02f, FrontFace + 0.205f, SurfaceMaterial.Screen);

            // Bookcase against the left wall.
            Bookcase(context, space, left + 0.02f, floor, 1.0f, 2.4f, 1.9f);
        }

        private static void Kitchen(BuildContext context, UnitSpace space, float floor, float right, float back)
        {
            var builder = context.Builder;
            var left = StripB + Thin;

            // Units along the back wall under the window: sink, cooker, and a tall fridge.
            space.Box(builder, left + 0.05f, floor, back - 0.6f, right - 0.75f, floor + 0.88f, back, SurfaceMaterial.PaintCream);
            space.Box(builder, left + 0.02f, floor + 0.88f, back - 0.62f, right - 0.72f, floor + 0.92f, back, SurfaceMaterial.Timber);
            var sink = space.Width * 0.5f;
            space.Box(builder, sink - 0.3f, floor + 0.92f, back - 0.5f, sink + 0.3f, floor + 0.93f, back - 0.1f, SurfaceMaterial.Chrome);
            space.Round(builder, sink, floor + 0.92f, back - 0.07f, 0.02f, 0.3f, SurfaceMaterial.Chrome, 6);
            var cooker = sink + 1.4f;
            space.Box(builder, cooker - 0.3f, floor + 0.92f, back - 0.55f, cooker + 0.3f, floor + 0.94f, back - 0.05f, SurfaceMaterial.PaintBlack);
            foreach (var (dx, dd) in new[] { (-0.14f, -0.38f), (0.14f, -0.38f), (-0.14f, -0.18f), (0.14f, -0.18f) })
            {
                space.Round(builder, cooker + dx, floor + 0.94f, back + dd, 0.08f, 0.01f, SurfaceMaterial.Iron, 8);
            }

            space.Box(builder, right - 0.7f, floor, back - 0.65f, right - 0.02f, floor + 1.85f, back, SurfaceMaterial.PaintWhite);
            // Wall cupboards beside the window, not over it.
            foreach (var x0 in new[] { sink + 0.6f, sink + 1.55f })
            {
                space.Box(builder, x0, floor + 1.5f, back - 0.35f, x0 + 0.9f, floor + 2.2f, back, SurfaceMaterial.PaintCream);
            }

            // A table with two chairs.
            var table = (left + right) * 0.5f;
            var middle = (RoomsWall + Thin + back - 0.6f) * 0.5f;
            space.Box(builder, table - 0.6f, floor + 0.72f, middle - 0.4f, table + 0.6f, floor + 0.76f, middle + 0.4f, SurfaceMaterial.Timber);
            foreach (var (lx, ld) in new[] { (-0.5f, -0.3f), (0.5f, -0.3f), (-0.5f, 0.3f), (0.5f, 0.3f) })
            {
                space.Box(builder, table + lx - 0.03f, floor, middle + ld - 0.03f, table + lx + 0.03f, floor + 0.72f, middle + ld + 0.03f, SurfaceMaterial.Timber);
            }

            Chair(space, builder, table, floor, middle - 0.65f, 0f, 1f, SurfaceMaterial.PaintSage);
            Chair(space, builder, table, floor, middle + 0.65f, 0f, -1f, SurfaceMaterial.PaintSage);
        }

        private static void Bedroom(BuildContext context, UnitSpace space, float floor, float right)
        {
            var builder = context.Builder;
            var left = StripB + Thin;
            var centre = (left + right) * 0.5f;

            // A double bed with its head against the back wall of the room.
            var bedFoot = RoomsWall - 2.1f;
            space.Box(builder, centre - 0.8f, floor, bedFoot, centre + 0.8f, floor + 0.35f, RoomsWall - 0.05f, SurfaceMaterial.Timber);
            space.Box(builder, centre - 0.8f, floor, RoomsWall - 0.12f, centre + 0.8f, floor + 1.05f, RoomsWall - 0.04f, SurfaceMaterial.Timber);
            space.Box(builder, centre - 0.76f, floor + 0.35f, bedFoot + 0.04f, centre + 0.76f, floor + 0.55f, RoomsWall - 0.14f, SurfaceMaterial.Linen);
            space.Box(builder, centre - 0.78f, floor + 0.55f, bedFoot + 0.02f, centre + 0.78f, floor + 0.6f, RoomsWall - 0.75f, SurfaceMaterial.FabricNavy);
            foreach (var x in new[] { centre - 0.4f, centre + 0.4f })
            {
                space.Box(builder, x - 0.3f, floor + 0.55f, RoomsWall - 0.6f, x + 0.3f, floor + 0.7f, RoomsWall - 0.2f, SurfaceMaterial.Linen);
            }

            // Bedside tables with lamps, a wardrobe, and a rug.
            foreach (var x in new[] { centre - 1.15f, centre + 1.15f })
            {
                space.Box(builder, x - 0.22f, floor, RoomsWall - 0.5f, x + 0.22f, floor + 0.5f, RoomsWall - 0.05f, SurfaceMaterial.Timber);
                space.Round(builder, x, floor + 0.5f, RoomsWall - 0.28f, 0.06f, 0.25f, SurfaceMaterial.Porcelain, 8);
                builder.AddCone(space.At(x, floor + 0.72f, RoomsWall - 0.28f), 0.14f, 0.16f, 8, SurfaceMaterial.Linen, capBase: true);
            }

            space.Box(builder, right - 0.6f, floor, 1.9f, right - 0.02f, floor + 2.0f, 3.1f, SurfaceMaterial.Timber);
            space.Box(builder, centre - 1.0f, floor, bedFoot - 1.2f, centre + 1.0f, floor + 0.012f, bedFoot + 0.3f, SurfaceMaterial.FabricSage);
        }

        private static void Bathroom(BuildContext context, UnitSpace space, float floor, float right, float back)
        {
            var builder = context.Builder;
            var left = StripB + Thin;
            space.Box(builder, left, floor + 0.004f, RoomsWall + Thin, right, floor + 0.01f, back, SurfaceMaterial.TileLight);

            // Bath along the back wall, basin and toilet along the right.
            space.Box(builder, left + 0.1f, floor, back - 0.75f, left + 1.8f, floor + 0.55f, back - 0.02f, SurfaceMaterial.Porcelain);
            space.Box(builder, left + 0.18f, floor + 0.551f, back - 0.67f, left + 1.72f, floor + 0.555f, back - 0.1f, SurfaceMaterial.ShopGlass);
            space.Round(builder, left + 1.65f, floor + 0.55f, back - 0.1f, 0.02f, 0.25f, SurfaceMaterial.Chrome, 6);

            var basin = RoomsWall + 1.2f;
            space.Round(builder, right - 0.3f, floor, basin, 0.08f, 0.8f, SurfaceMaterial.Porcelain, 8);
            space.Box(builder, right - 0.5f, floor + 0.8f, basin - 0.28f, right - 0.02f, floor + 0.9f, basin + 0.28f, SurfaceMaterial.Porcelain);
            space.Box(builder, right - 0.04f, floor + 1.15f, basin - 0.3f, right - 0.01f, floor + 1.75f, basin + 0.3f, SurfaceMaterial.Chrome);

            var toilet = basin + 1.1f;
            space.Round(builder, right - 0.42f, floor, toilet, 0.18f, 0.42f, SurfaceMaterial.Porcelain, 10);
            space.Box(builder, right - 0.2f, floor + 0.42f, toilet - 0.2f, right - 0.02f, floor + 0.85f, toilet + 0.2f, SurfaceMaterial.Porcelain);
        }

        // ---- The attic ----------------------------------------------------------------------

        private static void Attic(BuildContext context, UnitSpace space, GableRoof roof, float floor, Dormer.Span? dormer)
        {
            var builder = context.Builder;
            var w = space.Width;
            var depth = space.Depth;
            var right = w - Side;
            var plaster = SurfaceMaterial.Interior;
            const float lining = 0.06f;
            float Under(float d) => roof.PlaneHeight(d / depth) - lining;
            var ridgeD = depth * 0.5f;
            var front = FrontFace;
            var back = depth - FrontFace;

            // The slopes, cut round the dormer on the front.
            void Slope(float x0, float x1, float d0, float d1, bool frontSlope)
            {
                if (x1 - x0 < 1e-3f || d1 - d0 < 1e-3f)
                {
                    return;
                }

                var facing = frontSlope ? -Vector3.UnitY + space.Back : -Vector3.UnitY - space.Back;
                builder.AddQuadFacing(space.At(x0, Under(d0), d0), space.At(x1, Under(d0), d0), space.At(x1, Under(d1), d1), space.At(x0, Under(d1), d1), facing, plaster);
            }

            if (dormer.HasValue)
            {
                // The dormer's inside faces sit 3 cm in from its outside; the hole matches them.
                var span = dormer.Value;
                var x0 = (span.A0 * w) + 0.03f;
                var x1 = (span.A1 * w) - 0.03f;
                var d0 = span.FrontB * depth;
                var d1 = span.BackB * depth;
                Slope(Side, right, front, d0, true);
                Slope(Side, x0, d0, d1, true);
                Slope(x1, right, d0, d1, true);
                Slope(Side, right, d1, ridgeD, true);

                // Close the gap between the lining and the slates round the edges of the hole.
                float Slates(float d) => roof.PlaneHeight(d / depth) + roof.Thickness;
                builder.AddQuadFacing(space.At(x0, Under(d0), d0), space.At(x0, Under(d1), d1), space.At(x0, Slates(d1), d1), space.At(x0, Slates(d0), d0), space.Right, plaster);
                builder.AddQuadFacing(space.At(x1, Under(d0), d0), space.At(x1, Under(d1), d1), space.At(x1, Slates(d1), d1), space.At(x1, Slates(d0), d0), -space.Right, plaster);
                space.WallAcross(builder, d1, x0, x1, Under(d1), span.Top, false, plaster);
                space.WallAcross(builder, d0, x0, x1, Under(d0), span.Sill, true, plaster);
            }
            else
            {
                Slope(Side, right, front, ridgeD, true);
            }

            Slope(Side, right, ridgeD, back, false);

            // The gable ends, and a low wall where the slopes meet the floor at front and back.
            foreach (var (x, facingRight) in new[] { (Side, true), (right, false) })
            {
                var facing = facingRight ? space.Right : -space.Right;
                var corners = new[]
                {
                    space.At(x, floor, front), space.At(x, floor, back), space.At(x, Under(back), back),
                    space.At(x, Under(ridgeD), ridgeD), space.At(x, Under(front), front),
                };
                for (var i = 1; i < corners.Length - 1; i++)
                {
                    builder.AddTriangleFacing(corners[0], corners[i], corners[i + 1], facing, plaster);
                }
            }

            space.WallAcross(builder, front, Side, right, floor, Under(front), true, plaster);
            space.WallAcross(builder, back, Side, right, floor, Under(back), false, plaster);

            // A bed under the back slope, a desk in the dormer, boxes in the eaves.
            var bedD = back - 1.1f;
            space.Box(builder, right - 2.2f, floor, bedD - 0.5f, right - 0.2f, floor + 0.3f, bedD + 0.5f, SurfaceMaterial.Timber);
            space.Box(builder, right - 2.15f, floor + 0.3f, bedD - 0.45f, right - 0.25f, floor + 0.45f, bedD + 0.45f, SurfaceMaterial.Linen);
            space.Box(builder, right - 1.8f, floor + 0.45f, bedD - 0.47f, right - 0.23f, floor + 0.5f, bedD + 0.47f, SurfaceMaterial.FabricRust);
            space.Box(builder, right - 2.1f, floor + 0.45f, bedD - 0.35f, right - 1.8f, floor + 0.58f, bedD + 0.35f, SurfaceMaterial.Linen);

            if (dormer.HasValue)
            {
                var deskX = (dormer.Value.A0 + dormer.Value.A1) * 0.5f * w;
                var deskD = dormer.Value.FrontB * depth + 1.1f;
                space.Box(builder, deskX - 0.6f, floor + 0.72f, deskD - 0.3f, deskX + 0.6f, floor + 0.76f, deskD + 0.3f, SurfaceMaterial.Timber);
                foreach (var dx in new[] { -0.55f, 0.55f })
                {
                    space.Box(builder, deskX + dx - 0.03f, floor, deskD - 0.27f, deskX + dx + 0.03f, floor + 0.72f, deskD + 0.27f, SurfaceMaterial.Timber);
                }

                Chair(space, builder, deskX, floor, deskD + 0.6f, 0f, -1f, SurfaceMaterial.PaintSage);
                space.Round(builder, deskX + 0.4f, floor + 0.76f, deskD - 0.1f, 0.05f, 0.3f, SurfaceMaterial.Chrome, 6);
                builder.AddCone(space.At(deskX + 0.4f, floor + 1.0f, deskD - 0.1f), 0.12f, 0.12f, 8, SurfaceMaterial.PaintCream, capBase: true);
            }

            for (var i = 0; i < 4; i++)
            {
                var x = StripA + 0.6f + (i * 0.7f);
                space.Box(builder, x, floor, back - 1.3f, x + 0.5f, floor + 0.3f + (0.08f * (i % 3)), back - 0.8f, SurfaceMaterial.Cardboard);
            }

            space.Box(builder, (Side + right) * 0.5f - 1.0f, floor, ridgeD - 0.7f, (Side + right) * 0.5f + 1.2f, floor + 0.012f, ridgeD + 0.9f, SurfaceMaterial.FabricNavy);
            Light(context, space, (StripB + right) * 0.5f, roof.PlaneHeight(0.5f) - lining, ridgeD, AnchorKind.RoomLight);
        }

        // ---- Stairs and floors --------------------------------------------------------------

        private static (float X0, float D0, float X1, float D1) Hole(StairFlight flight) => (flight.X0, flight.NearD, flight.X1, flight.FarD);

        // A floor with the ceiling of the room below under it, and the sides of a stairwell.
        private static void Slabs(UnitSpace space, MeshBuilder builder, float y, float x0, float d0, float x1, float d1, (float X0, float D0, float X1, float D1)? hole)
        {
            Floor(space, builder, y, x0, d0, x1, d1, hole, SurfaceMaterial.InteriorFloor);
            foreach (var (a0, b0, a1, b1) in Around(x0, d0, x1, d1, hole))
            {
                space.Ceiling(builder, a0, b0, a1, b1, y - Slab, SurfaceMaterial.Interior);
            }

            if (hole.HasValue)
            {
                var h = hole.Value;
                var bottom = y - Slab;
                space.WallAcross(builder, h.D0, h.X0, h.X1, bottom, y, true, SurfaceMaterial.Interior);
                space.WallAcross(builder, h.D1, h.X0, h.X1, bottom, y, false, SurfaceMaterial.Interior);
                space.WallAlong(builder, h.X0, h.D0, h.D1, bottom, y, true, SurfaceMaterial.Interior);
                space.WallAlong(builder, h.X1, h.D0, h.D1, bottom, y, false, SurfaceMaterial.Interior);
            }
        }

        private static void Floor(UnitSpace space, MeshBuilder builder, float y, float x0, float d0, float x1, float d1, (float X0, float D0, float X1, float D1)? hole, SurfaceMaterial material)
        {
            foreach (var (a0, b0, a1, b1) in Around(x0, d0, x1, d1, hole))
            {
                space.Floor(builder, a0, b0, a1, b1, y, material);
            }
        }

        // The rectangle with the hole cut out, as up to four rectangles.
        private static IEnumerable<(float, float, float, float)> Around(float x0, float d0, float x1, float d1, (float X0, float D0, float X1, float D1)? hole)
        {
            if (!hole.HasValue)
            {
                yield return (x0, d0, x1, d1);
                yield break;
            }

            var h = hole.Value;
            yield return (x0, d0, x1, h.D0);
            yield return (x0, h.D1, x1, d1);
            yield return (x0, h.D0, h.X0, h.D1);
            yield return (h.X1, h.D0, x1, h.D1);
        }

        // Banisters on the open side of each upper flight, and round the stairwell edges you
        // could otherwise step off.
        private static void Banisters(UnitSpace space, MeshBuilder builder, StairFlight[] flights, float[] f)
        {
            // Both upper flights are open on the StripA line: the middle flight to the well on its
            // left, the last to the well on its right.
            foreach (var flight in new[] { flights[1], flights[2] })
            {
                const float open = StripA;
                var direction = Math.Sign(flight.EndD - flight.StartD);
                for (var i = 0; i < flight.Steps; i++)
                {
                    var d0 = flight.StartD + (direction * i * flight.Going);
                    var d1 = d0 + (direction * flight.Going);
                    var top = flight.FromY + ((i + 1) * flight.Rise) + 0.9f;
                    space.Box(builder, open - 0.025f, flight.FromY, Math.Min(d0, d1), open + 0.025f, top - 0.05f, Math.Max(d0, d1), SurfaceMaterial.PaintWhite);
                    space.Box(builder, open - 0.04f, top - 0.05f, Math.Min(d0, d1), open + 0.04f, top, Math.Max(d0, d1), SurfaceMaterial.Timber);
                }
            }

            // Attic: round the top of the last flight.
            Rail(space, builder, Side, UpperFoot, StripA, UpperFoot, f[3]);
            Rail(space, builder, StripA, UpperFoot, StripA, Turn, f[3]);
        }

        private static void Rail(UnitSpace space, MeshBuilder builder, float x0, float d0, float x1, float d1, float floor)
        {
            const float half = 0.03f;
            space.Box(builder, Math.Min(x0, x1) - half, floor, Math.Min(d0, d1) - half, Math.Max(x0, x1) + half, floor + 0.9f, Math.Max(d0, d1) + half, SurfaceMaterial.PaintWhite);
            space.Box(builder, Math.Min(x0, x1) - half - 0.015f, floor + 0.9f, Math.Min(d0, d1) - half - 0.015f, Math.Max(x0, x1) + half + 0.015f, floor + 0.95f, Math.Max(d0, d1) + half + 0.015f, SurfaceMaterial.Timber);
        }

        // ---- Windows and lights -------------------------------------------------------------

        // The inside of the front windows, matching the terrace's three columns above the shop.
        private static Hole[] FrontWindows(UnitSpace space, float floor, bool firstFloor)
        {
            var width = space.Width;
            var halfWidth = width < 4.8f ? 0.45f : 0.5f;
            var sill = floor + (firstFloor ? 0.85f : 0.8f);
            var height = firstFloor ? 1.5f : 1.3f;
            var holes = new Hole[3];
            for (var i = 0; i < 3; i++)
            {
                var centre = width * (i + 0.5f) / 3f;
                holes[i] = new Hole(centre - halfWidth, sill, centre + halfWidth, sill + height);
            }

            return holes;
        }

        /// <summary>The back window on each floor, in unit x, as <see cref="TerracedUnitStyle"/> cuts it.</summary>
        public static Hole BackWindow(UnitSpace space, float floor, bool ground)
        {
            var width = space.Width;
            return ground
                ? new Hole((width * 0.7f) - 0.45f, floor + 0.9f, (width * 0.7f) + 0.45f, floor + 2.1f)
                : new Hole((width * 0.5f) - 0.5f, floor + 0.85f, (width * 0.5f) + 0.5f, floor + 2.1f);
        }

        // A pendant lamp hanging from the ceiling, and the light it gives.
        private static void Light(BuildContext context, UnitSpace space, float x, float ceiling, float d, AnchorKind kind)
        {
            var builder = context.Builder;
            space.Box(builder, x - 0.01f, ceiling - 0.45f, d - 0.01f, x + 0.01f, ceiling, d + 0.01f, SurfaceMaterial.PaintBlack);
            builder.AddCone(space.At(x, ceiling - 0.6f, d), 0.2f, 0.16f, 10, SurfaceMaterial.PaintCream);
            builder.AddBlob(space.At(x, ceiling - 0.6f, d), new Vector3(0.05f), SurfaceMaterial.LampGlass);
            context.Anchor(kind, space.At(x, ceiling - 0.05f, d), -Vector3.UnitY, 3f);
        }

        // ---- Furniture ------------------------------------------------------------------------

        private static void Tiles(UnitSpace space, MeshBuilder builder, float y, float x0, float d0, float x1, float d1)
        {
            const float size = 0.5f;
            for (var x = x0; x < x1 - 1e-3f; x += size)
            {
                for (var d = d0; d < d1 - 1e-3f; d += size)
                {
                    var odd = ((int)Math.Round((x - x0) / size) + (int)Math.Round((d - d0) / size)) % 2 == 1;
                    space.Floor(builder, x, d, Math.Min(x1, x + size), Math.Min(d1, d + size), y, odd ? SurfaceMaterial.TileDark : SurfaceMaterial.TileLight);
                }
            }
        }

        private static void GlassBox(UnitSpace space, MeshBuilder builder, float x0, float y0, float d0, float x1, float y1, float d1)
        {
            var glass = SurfaceMaterial.ShopGlass;
            space.WallAcross(builder, d0, x0, x1, y0, y1, false, glass);
            space.WallAcross(builder, d1, x0, x1, y0, y1, true, glass);
            space.WallAlong(builder, x0, d0, d1, y0, y1, false, glass);
            space.WallAlong(builder, x1, d0, d1, y0, y1, true, glass);
            space.Floor(builder, x0, d0, x1, d1, y1, glass);
        }

        private static void Table(UnitSpace space, MeshBuilder builder, float x, float floor, float d, float radius)
        {
            space.Round(builder, x, floor, d, 0.2f, 0.03f, SurfaceMaterial.Iron, 8);
            space.Round(builder, x, floor, d, 0.04f, 0.72f, SurfaceMaterial.Iron, 6);
            space.Round(builder, x, floor + 0.72f, d, radius, 0.04f, SurfaceMaterial.Timber, 12);
        }

        // A chair whose seat faces (faceX, faceD); its back is on the other side.
        private static void Chair(UnitSpace space, MeshBuilder builder, float x, float floor, float d, float faceX, float faceD, SurfaceMaterial material)
        {
            const float half = 0.21f;
            space.Box(builder, x - half, floor + 0.43f, d - half, x + half, floor + 0.47f, d + half, material);
            foreach (var (lx, ld) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                space.Box(builder, x + (lx * (half - 0.04f)) - 0.02f, floor, d + (ld * (half - 0.04f)) - 0.02f, x + (lx * (half - 0.04f)) + 0.02f, floor + 0.43f, d + (ld * (half - 0.04f)) + 0.02f, material);
            }

            var backX = x - (faceX * (half - 0.03f));
            var backD = d - (faceD * (half - 0.03f));
            var acrossX = Math.Abs(faceD) > 0.5f ? half : 0.03f;
            var acrossD = Math.Abs(faceX) > 0.5f ? half : 0.03f;
            space.Box(builder, backX - acrossX, floor + 0.47f, backD - acrossD, backX + acrossX, floor + 0.95f, backD + acrossD, material);
        }

        // A sofa across the room with its back on the far side (d1).
        private static void Sofa(UnitSpace space, MeshBuilder builder, float x0, float x1, float floor, float d0, float d1, SurfaceMaterial fabric)
        {
            space.Box(builder, x0, floor, d0, x1, floor + 0.42f, d1, fabric);
            space.Box(builder, x0, floor + 0.42f, d1 - 0.22f, x1, floor + 0.85f, d1, fabric);
            space.Box(builder, x0, floor + 0.42f, d0, x0 + 0.18f, floor + 0.62f, d1 - 0.22f, fabric);
            space.Box(builder, x1 - 0.18f, floor + 0.42f, d0, x1, floor + 0.62f, d1 - 0.22f, fabric);
            space.Box(builder, x0 + 0.2f, floor + 0.42f, d0 + 0.05f, (x0 + x1) * 0.5f - 0.02f, floor + 0.52f, d1 - 0.24f, SurfaceMaterial.Linen);
            space.Box(builder, (x0 + x1) * 0.5f + 0.02f, floor + 0.42f, d0 + 0.05f, x1 - 0.2f, floor + 0.52f, d1 - 0.24f, SurfaceMaterial.Linen);
        }

        // An armchair turned to face the room's centre, its back to the right-hand wall.
        private static void Armchair(UnitSpace space, MeshBuilder builder, float x, float floor, float d, SurfaceMaterial fabric)
        {
            space.Box(builder, x - 0.4f, floor, d - 0.4f, x + 0.4f, floor + 0.42f, d + 0.4f, fabric);
            space.Box(builder, x + 0.2f, floor + 0.42f, d - 0.4f, x + 0.4f, floor + 0.9f, d + 0.4f, fabric);
            space.Box(builder, x - 0.4f, floor + 0.42f, d - 0.4f, x + 0.2f, floor + 0.6f, d - 0.24f, fabric);
            space.Box(builder, x - 0.4f, floor + 0.42f, d + 0.24f, x + 0.2f, floor + 0.6f, d + 0.4f, fabric);
        }

        // Shelves of books against the left wall of the rooms (at x), from d0 to d1.
        private static void Bookcase(BuildContext context, UnitSpace space, float x, float floor, float d0, float d1, float height)
        {
            var builder = context.Builder;
            space.Box(builder, x, floor, d0, x + 0.32f, floor + 0.03f, d1, SurfaceMaterial.Timber);
            space.Box(builder, x, floor + height - 0.03f, d0, x + 0.32f, floor + height, d1, SurfaceMaterial.Timber);
            space.Box(builder, x, floor, d0, x + 0.32f, floor + height, d0 + 0.03f, SurfaceMaterial.Timber);
            space.Box(builder, x, floor, d1 - 0.03f, x + 0.32f, floor + height, d1, SurfaceMaterial.Timber);
            var spines = new[] { SurfaceMaterial.PaintRed, SurfaceMaterial.PaintNavy, SurfaceMaterial.PaintSage, SurfaceMaterial.PaintCream, SurfaceMaterial.PaintOxblood, SurfaceMaterial.PaintGold };
            for (var shelf = 0; shelf < 4; shelf++)
            {
                var y = floor + 0.03f + (shelf * (height - 0.06f) / 4f);
                if (shelf > 0)
                {
                    space.Box(builder, x, y - 0.02f, d0, x + 0.32f, y, d1, SurfaceMaterial.Timber);
                }

                for (var d = d0 + 0.05f; d < d1 - 0.08f;)
                {
                    var thick = 0.03f + (0.03f * (float)context.Random.NextDouble());
                    var tall = 0.2f + (0.12f * (float)context.Random.NextDouble());
                    space.Box(builder, x + 0.04f, y, d, x + 0.28f, y + tall, d + thick, spines[context.Random.Next(spines.Length)]);
                    d += thick + 0.005f;
                }
            }
        }
    }

    internal static class FlatPoint
    {
        /// <summary>The ground-plane position (x, z) of a point.</summary>
        public static Vector2 ToFlat(this Vector3 point) => new Vector2(point.X, point.Z);
    }
}
