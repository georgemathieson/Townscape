using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Buildings.Styles
{
    /// <summary>The look of a village petrol station: its name, colours, prices and decorations.</summary>
    public sealed class PetrolStationDesign
    {
        /// <summary>On the board across the workshop gable (the pixel font supports A–Z, 0–9 and &amp; ' . - +).</summary>
        public string Name { get; init; } = "FELL VIEW GARAGE";

        public SurfaceMaterial Wall { get; init; } = SurfaceMaterial.RenderWhite;

        public SurfaceMaterial Roof { get; init; } = SurfaceMaterial.Pantile;

        /// <summary>The canopy, its posts, the doors and the sign boards.</summary>
        public SurfaceMaterial Paint { get; init; } = SurfaceMaterial.PaintNavy;

        public SurfaceMaterial Lettering { get; init; } = SurfaceMaterial.PaintWhite;

        /// <summary>Flag colours along the bunting, repeating.</summary>
        public IReadOnlyList<SurfaceMaterial> Bunting { get; init; } = new[]
        {
            SurfaceMaterial.PaintRed, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintBlue,
            SurfaceMaterial.PaintGreen, SurfaceMaterial.PaintOrange, SurfaceMaterial.PaintWhite,
        };

        /// <summary>Bulb colours along the string lights in the shop window, repeating.</summary>
        public IReadOnlyList<SurfaceMaterial> Bulbs { get; init; } = new[]
        {
            SurfaceMaterial.BulbRed, SurfaceMaterial.BulbGreen, SurfaceMaterial.BulbOrange, SurfaceMaterial.BulbYellow, SurfaceMaterial.BulbBlue,
        };

        /// <summary>The price sign by the road, a line each.</summary>
        public IReadOnlyList<string> Prices { get; init; } = new[] { "UNLEADED", "149.9", "DIESEL", "155.9" };
    }

    /// <summary>
    /// A small village petrol station on a whole plot: a whitewashed garage with a pantile roof at
    /// the back (the shop on the left, a gabled workshop on the right), two pumps on an island
    /// parallel to it under a flat canopy, a price sign by the road, string lights in the shop
    /// window and bunting strung about. The plot is surfaced in concrete.
    /// </summary>
    /// <remarks>
    /// Everything is placed in site metres: <c>x</c> from the left of the plot as seen from the road,
    /// <c>d</c> back from the pavement, <c>y</c> up. It needs a plot of at least
    /// <see cref="MinimumWidth"/> by <see cref="MinimumDepth"/> metres.
    /// </remarks>
    public sealed class PetrolStationStyle : IBuildingStyle, IYardFinish
    {
        public const float MinimumWidth = 22f;
        public const float MinimumDepth = 17.5f;

        // The garage: shop on the left, workshop wing on the right, its gable stepped forward.
        private const float BuildingLeft = 1f;
        private const float WingLeft = 8f;
        private const float BuildingRight = 12.5f;
        private const float BuildingFront = 10.5f;
        private const float WingFront = 9.7f;
        private const float BuildingDepth = 6.5f;
        private const float StoreyHeight = 3f;
        private const float MainPitch = 35f;
        private const float WingPitch = 40f;

        // The forecourt: an island with two pumps under a flat canopy.
        private const float CanopyLeft = 10.5f;
        private const float CanopyRight = 19.5f;
        private const float CanopyFront = 2.2f;
        private const float CanopyBack = 7.8f;
        private const float CanopyClearance = 4.25f;
        private const float CanopyTop = 5f;
        private const float IslandLeft = 12.2f;
        private const float IslandRight = 17.8f;
        private const float IslandCentre = 5f;
        private const float IslandHalfWidth = 0.55f;
        private const float IslandHeight = 0.18f;

        // The price sign's left edge, in from the plot's right-hand side: clear of a street lamp at the corner.
        private const float SignInset = 3.9f;
        private const float FlagSpacing = 0.42f;
        private const float BulbSpacing = 0.15f;

        private readonly PetrolStationDesign _design;

        public PetrolStationStyle(PetrolStationDesign design = null)
        {
            _design = design ?? new PetrolStationDesign();
        }

        public PetrolStationDesign Design => _design;

        public SurfaceMaterial Yard => SurfaceMaterial.Concrete;

        /// <summary>Where the pumps stand, in site metres (x along the island, d back from the pavement).</summary>
        public static IReadOnlyList<Vector2> PumpPositions { get; } = new[] { new Vector2(14f, IslandCentre), new Vector2(16f, IslandCentre) };

        public void Build(Footprint footprint, BuildContext context)
        {
            if (footprint.FrontWidth < MinimumWidth - 0.01f || footprint.Depth < MinimumDepth - 0.01f)
            {
                throw new ArgumentException($"A petrol station needs a plot of at least {MinimumWidth} by {MinimumDepth} metres.");
            }

            var site = new Site(footprint);
            var ground = RoadFeature.PavementHeight;
            var floor = BuildingLevels.Floor;
            var eaves = floor + StoreyHeight;
            var mainRidge = eaves + (BuildingDepth * 0.5f * MathF.Tan(MainPitch * MathF.PI / 180f));
            var wingRidge = eaves + ((BuildingRight - WingLeft) * 0.5f * MathF.Tan(WingPitch * MathF.PI / 180f));

            BuildGarage(context, site, floor, eaves, mainRidge, wingRidge);
            BuildForecourt(context, site, ground);
            BuildPriceSign(context, site, ground);
            BuildBunting(context, site, ground, eaves, wingRidge);
        }

        private void BuildGarage(BuildContext context, Site site, float floor, float eaves, float mainRidge, float wingRidge)
        {
            var builder = context.Builder;
            var design = _design;
            var buildingBack = BuildingFront + BuildingDepth;
            var mainRidgeLine = BuildingFront + (BuildingDepth * 0.5f);

            // The shop: a small window, the door and the shop window.
            var shop = WallFrame.FromBase(site.At(BuildingLeft, BuildingFront), site.At(WingLeft, BuildingFront));
            var smallWindow = new Opening(0.5f, floor + 0.9f, 1.6f, floor + 2.2f, 0.16f);
            var door = new Opening(2f, floor, 2.95f, floor + 2.1f, 0.18f);
            var shopWindow = new Opening(3.4f, floor + 0.75f, 6.6f, floor + 2.4f, 0.1f);
            Courses.Plinth(builder, shop);
            WallBuilder.Build(builder, shop, floor, eaves, new[] { smallWindow, door, shopWindow }, design.Wall);
            var windows = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.Casement, SurfaceMaterial.Kerb);
            Glazing.FillWindow(context, shop, smallWindow, windows);
            StringLights(context, shop, smallWindow.X0 + 0.08f, smallWindow.X1 - 0.08f, smallWindow.Y1 - 0.1f, 0.12f, -smallWindow.Depth + 0.04f);
            ShopDoor(context, shop, door);
            Board(context, shop, door.X0 - 0.25f, door.X1 + 0.25f, door.Y1 + 0.08f, 0.28f, "SHOP");
            ShopWindow(context, shop, shopWindow);

            // Gas bottles by the shop wall.
            var bottleWall = shop.Width - 0.6f;
            foreach (var (x, colour) in new[] { (bottleWall - 0.45f, SurfaceMaterial.PaintRed), (bottleWall - 0.05f, SurfaceMaterial.PaintRed), (bottleWall + 0.35f, SurfaceMaterial.PaintBlue) })
            {
                var foot = shop.Point(x, floor, 0.3f);
                builder.AddPrism(foot, 0.16f, 0.62f, 8, colour);
                builder.AddPrism(foot + new Vector3(0f, 0.62f, 0f), 0.07f, 0.1f, 6, SurfaceMaterial.Iron);
            }

            // The workshop wing: open doors, the garage's name across the gable, and its signs.
            var wing = WallFrame.FromBase(site.At(WingLeft, WingFront), site.At(BuildingRight, WingFront));
            var workshop = new Opening(0.55f, floor, wing.Width - 0.55f, floor + 2.7f, 0.2f);
            Courses.Plinth(builder, wing);
            WallBuilder.Build(builder, wing, floor, eaves, new[] { workshop }, design.Wall);
            GableRoof.GableWall(builder, wing, eaves, wingRidge, design.Wall);
            Workshop(context, wing, workshop, floor);
            Board(context, wing, 0.15f, wing.Width - 0.15f, eaves - 0.62f, 0.6f, design.Name);
            GableSign(context, wing, (wing.Width * 0.5f) - 0.7f, eaves + 0.6f, SurfaceMaterial.PaintBlue, SurfaceMaterial.PaintWhite, "MOT");
            GableSign(context, wing, (wing.Width * 0.5f) + 0.7f, eaves + 0.6f, SurfaceMaterial.PaintWhite, design.Paint, "TYRES");

            // The rest of the shell.
            var wingSide = WallFrame.FromBase(site.At(WingLeft, BuildingFront), site.At(WingLeft, WingFront));
            wingSide.Quad(builder, 0f, BuildingLevels.Base, wingSide.Width, eaves, 0f, design.Wall);
            var right = WallFrame.FromBase(site.At(BuildingRight, WingFront), site.At(BuildingRight, buildingBack));
            right.Quad(builder, 0f, BuildingLevels.Base, right.Width, eaves, 0f, design.Wall);
            GableRoof.GableWall(builder, WallFrame.FromBase(site.At(BuildingRight, BuildingFront), site.At(BuildingRight, buildingBack)), eaves, mainRidge, design.Wall);
            var left = WallFrame.FromBase(site.At(BuildingLeft, buildingBack), site.At(BuildingLeft, BuildingFront));
            Courses.Plinth(builder, left);
            left.Quad(builder, 0f, BuildingLevels.Base, left.Width, eaves, 0f, design.Wall);
            GableRoof.GableWall(builder, left, eaves, mainRidge, design.Wall);
            Glazing.AppliedWindow(context, left, (left.Width * 0.5f) - 0.5f, floor + 0.95f, (left.Width * 0.5f) + 0.5f, floor + 2.1f, windows);
            var back = WallFrame.FromBase(site.At(BuildingRight, buildingBack), site.At(BuildingLeft, buildingBack));
            back.Quad(builder, 0f, BuildingLevels.Base, back.Width, eaves, 0f, design.Wall);

            // Roofs: the main ridge runs along the road; the wing's runs back from its gable into the main roof.
            new GableRoof(site.Rect(BuildingLeft, BuildingFront, BuildingRight, buildingBack), eaves, mainRidge, overhang: 0.3f, verge: 0.25f)
                .Build(builder, design.Roof, SurfaceMaterial.PaintWhite);
            var wingRoof = new Footprint(site.At(WingLeft, mainRidgeLine), site.At(WingLeft, WingFront), site.At(BuildingRight, WingFront), site.At(BuildingRight, mainRidgeLine));
            new GableRoof(wingRoof, eaves, wingRidge, overhang: 0.25f, verge: 0.3f).Build(builder, design.Roof, SurfaceMaterial.PaintWhite);
        }

        private void ShopDoor(BuildContext context, WallFrame wall, Opening door)
        {
            var builder = context.Builder;
            var paint = _design.Paint;
            var back = -door.Depth;
            var glassBottom = door.Y0 + 1.0f;
            wall.Quad(builder, door.X0, door.Y0, door.X1, glassBottom, back, paint);
            wall.Block(builder, door.X0 + 0.1f, door.Y0 + 0.12f, door.X1 - 0.1f, glassBottom - 0.1f, back, back + 0.02f, paint);
            wall.Quad(builder, door.X0, glassBottom, door.X1, door.Y1, back, SurfaceMaterial.WindowShop);
            wall.Block(builder, door.X0, glassBottom, door.X1, glassBottom + 0.07f, back, back + 0.03f, paint);
            wall.Block(builder, door.X0, door.Y1 - 0.07f, door.X1, door.Y1, back, back + 0.03f, paint);
            wall.Block(builder, door.X0, glassBottom, door.X0 + 0.07f, door.Y1, back, back + 0.03f, paint);
            wall.Block(builder, door.X1 - 0.07f, glassBottom, door.X1, door.Y1, back, back + 0.03f, paint);
            wall.Block(builder, door.X0 - 0.1f, door.Y0 - 0.15f, door.X1 + 0.1f, door.Y0, -0.05f, 0.3f, SurfaceMaterial.Kerb);
        }

        // The shop window: glass in a white frame, shelves of oil and screenwash, and string lights
        // looped across the top. It glows warm after dark like every shop window in the village.
        private void ShopWindow(BuildContext context, WallFrame wall, Opening window)
        {
            var builder = context.Builder;
            const float glass = -0.08f;
            const float back = -1.1f;
            var up = Vector3.UnitY;
            float x0 = window.X0, x1 = window.X1, y0 = window.Y0, y1 = window.Y1;

            wall.Quad(builder, x0, y0, x1, y1, glass, SurfaceMaterial.ShopGlass);
            const float bar = 0.07f;
            wall.Block(builder, x0, y0, x0 + bar, y1, -0.1f, -0.03f, SurfaceMaterial.PaintWhite);
            wall.Block(builder, x1 - bar, y0, x1, y1, -0.1f, -0.03f, SurfaceMaterial.PaintWhite);
            wall.Block(builder, x0, y1 - bar, x1, y1, -0.1f, -0.03f, SurfaceMaterial.PaintWhite);
            wall.Block(builder, x0, y0, x1, y0 + bar, -0.1f, -0.03f, SurfaceMaterial.PaintWhite);
            wall.Block(builder, window.CentreX - 0.03f, y0, window.CentreX + 0.03f, y1, -0.1f, -0.04f, SurfaceMaterial.PaintWhite);
            wall.Block(builder, x0 - 0.06f, y0 - 0.06f, x1 + 0.06f, y0, -0.05f, 0.08f, SurfaceMaterial.Kerb);

            // The room behind the glass.
            wall.Quad(builder, x0, y0, x1, y1, back, SurfaceMaterial.Interior);
            builder.AddQuadFacing(wall.Point(x0, y0, glass), wall.Point(x1, y0, glass), wall.Point(x1, y0, back), wall.Point(x0, y0, back), up, SurfaceMaterial.InteriorFloor);
            builder.AddQuadFacing(wall.Point(x0, y1, glass), wall.Point(x1, y1, glass), wall.Point(x1, y1, back), wall.Point(x0, y1, back), -up, SurfaceMaterial.Interior);
            builder.AddQuadFacing(wall.Point(x0, y0, glass), wall.Point(x0, y1, glass), wall.Point(x0, y1, back), wall.Point(x0, y0, back), wall.Right, SurfaceMaterial.Interior);
            builder.AddQuadFacing(wall.Point(x1, y0, glass), wall.Point(x1, y1, glass), wall.Point(x1, y1, back), wall.Point(x1, y0, back), -wall.Right, SurfaceMaterial.Interior);

            // Shelves of oil cans and screenwash.
            var stock = new[] { SurfaceMaterial.PaintRed, SurfaceMaterial.PaintGold, SurfaceMaterial.PaintBlue, SurfaceMaterial.PaintGreen, SurfaceMaterial.PaintBlack };
            var item = 0;
            foreach (var shelf in new[] { y0 + 0.02f, y0 + 0.55f, y0 + 1.05f })
            {
                wall.Block(builder, x0 + 0.1f, shelf, x1 - 0.1f, shelf + 0.03f, back + 0.02f, back + 0.45f, SurfaceMaterial.Timber);
                for (var x = x0 + 0.28f; x < x1 - 0.2f; x += context.Range(0.2f, 0.32f))
                {
                    var tall = context.Range(0.16f, 0.3f);
                    var colour = stock[item++ % stock.Length];
                    builder.AddPrism(wall.Point(x, shelf + 0.03f, back + 0.25f), context.Range(0.05f, 0.08f), tall, 6, colour);
                }
            }

            StringLights(context, wall, x0 + 0.1f, window.CentreX, y1 - 0.12f, 0.24f, -0.17f);
            StringLights(context, wall, window.CentreX, x1 - 0.1f, y1 - 0.12f, 0.24f, -0.17f);
            context.Anchor(AnchorKind.ShopWindow, wall.Point(window.CentreX, (y0 + y1) * 0.5f, -0.6f), wall.Out, x1 - x0);
        }

        /// <summary>A sagging string of coloured bulbs between two points just behind the glass.</summary>
        private void StringLights(BuildContext context, WallFrame wall, float x0, float x1, float y, float sag, float z)
        {
            var builder = context.Builder;
            var count = Math.Max(2, (int)MathF.Round((x1 - x0) / BulbSpacing));
            Vector3 At(float t) => wall.Point(GeoMath.Lerp(x0, x1, t), y - (sag * 4f * t * (1f - t)), z);

            Cord(builder, At, 16, 0.012f, wall.Out, SurfaceMaterial.PaintDarkGreen);
            for (var i = 0; i <= count; i++)
            {
                var bulb = _design.Bulbs[i % _design.Bulbs.Count];
                var hang = At(i / (float)count) - new Vector3(0f, 0.09f, 0f);
                builder.AddPrism(hang, 0.034f, 0.08f, 6, bulb, capBottom: true);
            }
        }

        private void Workshop(BuildContext context, WallFrame wall, Opening opening, float floor)
        {
            var builder = context.Builder;
            const float depth = 3f;
            var up = Vector3.UnitY;
            float x0 = opening.X0, x1 = opening.X1, y1 = opening.Y1;

            // A dark room with a concrete floor, and the roller shutter rolled up at the top.
            wall.Quad(builder, x0, floor, x1, y1, -depth, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(wall.Point(x0, floor + 0.005f, -opening.Depth), wall.Point(x1, floor + 0.005f, -opening.Depth), wall.Point(x1, floor + 0.005f, -depth), wall.Point(x0, floor + 0.005f, -depth), up, SurfaceMaterial.Concrete);
            builder.AddQuadFacing(wall.Point(x0, y1, -opening.Depth), wall.Point(x1, y1, -opening.Depth), wall.Point(x1, y1, -depth), wall.Point(x0, y1, -depth), -up, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(wall.Point(x0, floor, -opening.Depth), wall.Point(x0, y1, -opening.Depth), wall.Point(x0, y1, -depth), wall.Point(x0, floor, -depth), wall.Right, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(wall.Point(x1, floor, -opening.Depth), wall.Point(x1, y1, -opening.Depth), wall.Point(x1, y1, -depth), wall.Point(x1, floor, -depth), -wall.Right, SurfaceMaterial.StoneDark);
            wall.Block(builder, x0, y1 - 0.22f, x1, y1, -opening.Depth - 0.2f, -opening.Depth, SurfaceMaterial.PaintWhite);

            // Stacks of tyres just inside.
            foreach (var (x, z, tyres) in new[] { (x0 + 0.5f, -0.9f, 4), (x1 - 0.55f, -1.9f, 3) })
            {
                for (var i = 0; i < tyres; i++)
                {
                    builder.AddPrism(wall.Point(x, floor + (i * 0.21f), z), 0.32f, 0.2f, 8, SurfaceMaterial.PaintBlack, rotation: i * 0.2f);
                }
            }
        }

        /// <summary>A board with lettering, standing proud of a wall.</summary>
        private void Board(BuildContext context, WallFrame wall, float x0, float x1, float y0, float height, string text)
        {
            wall.Block(context.Builder, x0, y0, x1, y0 + height, 0f, 0.07f, _design.Paint);
            PixelFont.Write(context.Builder, wall, text, (x0 + x1) * 0.5f, y0 + (height * 0.5f), 0.045f, x1 - x0 - 0.25f, 0.074f, _design.Lettering);
        }

        /// <summary>A small square sign on the gable (MOT, TYRES).</summary>
        private static void GableSign(BuildContext context, WallFrame wall, float centreX, float y0, SurfaceMaterial panel, SurfaceMaterial lettering, string text)
        {
            const float half = 0.27f;
            wall.Block(context.Builder, centreX - half, y0, centreX + half, y0 + (half * 2f), 0f, 0.04f, panel);
            PixelFont.Write(context.Builder, wall, text, centreX, y0 + half, 0.03f, (half * 2f) - 0.08f, 0.044f, lettering);
        }

        private void BuildForecourt(BuildContext context, Site site, float ground)
        {
            var builder = context.Builder;
            var paint = _design.Paint;
            var up = Vector3.UnitY;
            var islandTop = ground + IslandHeight;
            var soffit = ground + CanopyClearance;
            var top = ground + CanopyTop;

            // The island, with a pump either side of the middle, facing the lanes in front and behind.
            var islandCentre = site.At((IslandLeft + IslandRight) * 0.5f, ground + (IslandHeight * 0.5f), IslandCentre);
            builder.AddBox(islandCentre, site.Right3, up, site.In3, new Vector3((IslandRight - IslandLeft) * 0.5f, IslandHeight * 0.5f, IslandHalfWidth), SurfaceMaterial.Kerb);
            var pumpColours = new[] { SurfaceMaterial.PaintGreen, SurfaceMaterial.PaintBlack };
            for (var i = 0; i < PumpPositions.Count; i++)
            {
                Pump(builder, site.At(PumpPositions[i].X, islandTop, PumpPositions[i].Y), site.Right3, site.In3, pumpColours[i % pumpColours.Length]);
            }

            // Two posts on the island ends hold up the canopy.
            foreach (var x in new[] { IslandLeft + 0.4f, IslandRight - 0.4f })
            {
                builder.AddBox(site.At(x, (islandTop + soffit) * 0.5f, IslandCentre), site.Right3, up, site.In3, new Vector3(0.14f, (soffit - islandTop) * 0.5f, 0.14f), paint);
            }

            // The canopy: a navy fascia with a white stripe, a flat top and a white ceiling with lights.
            var front = WallFrame.FromBase(site.At(CanopyLeft, CanopyFront), site.At(CanopyRight, CanopyFront));
            var rightSide = WallFrame.FromBase(site.At(CanopyRight, CanopyFront), site.At(CanopyRight, CanopyBack));
            var backSide = WallFrame.FromBase(site.At(CanopyRight, CanopyBack), site.At(CanopyLeft, CanopyBack));
            var leftSide = WallFrame.FromBase(site.At(CanopyLeft, CanopyBack), site.At(CanopyLeft, CanopyFront));
            foreach (var side in new[] { front, rightSide, backSide, leftSide })
            {
                side.Quad(builder, 0f, soffit, side.Width, top, 0f, paint);
                side.Quad(builder, 0f, soffit + 0.3f, side.Width, soffit + 0.38f, 0.006f, SurfaceMaterial.PaintWhite);
            }

            PixelFont.Write(builder, front, "FUEL", front.Width * 0.5f, soffit + 0.57f, 0.035f, 2f, 0.008f, SurfaceMaterial.PaintWhite);
            PixelFont.Write(builder, backSide, "FUEL", backSide.Width * 0.5f, soffit + 0.57f, 0.035f, 2f, 0.008f, SurfaceMaterial.PaintWhite);
            builder.AddQuadFacing(site.At(CanopyLeft, top, CanopyFront), site.At(CanopyRight, top, CanopyFront), site.At(CanopyRight, top, CanopyBack), site.At(CanopyLeft, top, CanopyBack), up, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(site.At(CanopyLeft, soffit, CanopyFront), site.At(CanopyRight, soffit, CanopyFront), site.At(CanopyRight, soffit, CanopyBack), site.At(CanopyLeft, soffit, CanopyBack), -up, SurfaceMaterial.PaintWhite);
            foreach (var x in new[] { CanopyLeft + 2f, (CanopyLeft + CanopyRight) * 0.5f, CanopyRight - 2f })
            {
                foreach (var d in new[] { CanopyFront + 1.3f, CanopyBack - 1.3f })
                {
                    var y = soffit - 0.012f;
                    builder.AddQuadFacing(site.At(x - 0.55f, y, d - 0.25f), site.At(x + 0.55f, y, d - 0.25f), site.At(x + 0.55f, y, d + 0.25f), site.At(x - 0.55f, y, d + 0.25f), -up, SurfaceMaterial.CanopyLight);
                }
            }

            foreach (var x in new[] { CanopyLeft + 2.25f, CanopyRight - 2.25f })
            {
                context.Anchor(AnchorKind.CanopyLight, site.At(x, soffit - 0.15f, IslandCentre), -up, CanopyRight - CanopyLeft);
            }
        }

        // A pump: white body, a coloured head, a lit display on each face and a hose at each end.
        private static void Pump(MeshBuilder builder, Vector3 foot, Vector3 along, Vector3 across, SurfaceMaterial colour)
        {
            var up = Vector3.UnitY;
            builder.AddBox(foot + (up * 0.05f), along, up, across, new Vector3(0.42f, 0.05f, 0.27f), SurfaceMaterial.StoneDark);
            var bodyTop = 0.1f + 1.45f;
            builder.AddBox(foot + (up * (0.1f + (1.45f * 0.5f))), along, up, across, new Vector3(0.36f, 1.45f * 0.5f, 0.22f), SurfaceMaterial.PaintWhite);
            builder.AddBox(foot + (up * (bodyTop + 0.15f)), along, up, across, new Vector3(0.38f, 0.15f, 0.24f), colour);
            builder.AddBox(foot + (up * (bodyTop - 0.06f)), along, up, across, new Vector3(0.37f, 0.03f, 0.23f), colour);

            foreach (var side in new[] { -1f, 1f })
            {
                // Display and keypad on each face.
                var face = foot + (across * (side * 0.226f));
                var screen = face + (up * (bodyTop - 0.38f));
                builder.AddQuadFacing(screen - (along * 0.2f) - (up * 0.11f), screen + (along * 0.2f) - (up * 0.11f), screen + (along * 0.2f) + (up * 0.11f), screen - (along * 0.2f) + (up * 0.11f), across * side, SurfaceMaterial.Screen);
                var keys = face + (up * (bodyTop - 0.7f));
                builder.AddQuadFacing(keys - (along * 0.12f) - (up * 0.08f), keys + (along * 0.12f) - (up * 0.08f), keys + (along * 0.12f) + (up * 0.08f), keys - (along * 0.12f) + (up * 0.08f), across * side, SurfaceMaterial.PaintBlack);

                // A nozzle in its holster at each end, its hose hanging in a loop from the holster and
                // running back into the foot of the body.
                var end = foot + (along * (side * 0.37f));
                builder.AddBox(end + (up * 1.05f) + (along * (side * 0.05f)), along, up, across, new Vector3(0.05f, 0.1f, 0.06f), SurfaceMaterial.PaintBlack);
                var centre = end + (up * 0.68f);
                var previous = centre + (up * 0.32f);
                for (var i = 1; i <= 8; i++)
                {
                    var angle = (MathF.PI * 0.5f) - (MathF.PI * i / 8f);
                    var point = centre + (up * (0.32f * MathF.Sin(angle))) + (along * (side * 0.15f * MathF.Cos(angle)));
                    Tube(builder, previous, point, 0.02f, SurfaceMaterial.PaintBlack);
                    previous = point;
                }
            }
        }

        private void BuildPriceSign(BuildContext context, Site site, float ground)
        {
            var builder = context.Builder;
            var paint = _design.Paint;
            var x = site.Width - SignInset;
            var face = WallFrame.FromBase(site.At(x, 0.85f), site.At(x + 1.2f, 0.85f));
            const float bottom = 1f;
            const float height = 1.8f;
            foreach (var leg in new[] { 0.25f, 0.95f })
            {
                face.Block(builder, leg - 0.05f, ground, leg + 0.05f, ground + bottom, -0.1f, -0.02f, paint);
            }

            face.Block(builder, 0f, ground + bottom, face.Width, ground + bottom + height, -0.12f, 0f, paint);
            face.QuadInward(builder, 0f, ground + bottom, face.Width, ground + bottom + height, -0.12f, paint);
            face.Quad(builder, 0.08f, ground + bottom + 0.08f, face.Width - 0.08f, ground + bottom + height - 0.08f, 0.004f, SurfaceMaterial.SignGlass);
            var prices = _design.Prices;
            var row = (height - 0.3f) / Math.Max(1, prices.Count);
            for (var i = 0; i < prices.Count; i++)
            {
                var y = ground + bottom + height - 0.15f - (row * (i + 0.5f));
                PixelFont.Write(builder, face, prices[i], face.Width * 0.5f, y, i % 2 == 0 ? 0.018f : 0.03f, face.Width - 0.28f, 0.01f, paint);
            }

            context.Anchor(AnchorKind.LitSign, face.Point(face.Width * 0.5f, ground + bottom + (height * 0.5f), 0.3f), face.Out, face.Width);
        }

        private void BuildBunting(BuildContext context, Site site, float ground, float eaves, float wingRidge)
        {
            var builder = context.Builder;
            var top = ground + CanopyTop - 0.05f;
            var signTop = site.At(site.Width - SignInset + 0.6f, ground + 2.8f, 0.8f);

            // Small flags along the shop under the eaves, clear of its sign; then from each corner of
            // the canopy: along the road frontage to a pole, back to the workshop gable, down to the
            // price sign, and back to a second pole.
            Bunting(builder, site.At(BuildingLeft + 0.1f, eaves - 0.18f, BuildingFront - 0.12f), site.At(WingLeft - 0.1f, eaves - 0.18f, BuildingFront - 0.12f), 0.1f, flag: 0.22f);
            Bunting(builder, site.At(CanopyLeft, top, CanopyFront), site.At(BuildingLeft + 0.3f, ground + 3.2f, 1f), 0.45f, pole: true, ground: ground);
            Bunting(builder, site.At(CanopyLeft, top, CanopyBack), site.At((WingLeft + BuildingRight) * 0.5f, wingRidge - 0.3f, WingFront - 0.32f), 0.25f);
            Bunting(builder, site.At(CanopyRight, top, CanopyFront), signTop, 0.35f);
            Bunting(builder, site.At(CanopyRight, top, CanopyBack), site.At(CanopyRight + 2.2f, ground + 3.2f, CanopyBack + 2.6f), 0.3f, pole: true, ground: ground);
        }

        /// <summary>A sagging line of triangular flags, optionally ending at a pole planted in the forecourt.</summary>
        private void Bunting(MeshBuilder builder, Vector3 from, Vector3 to, float sag, float flag = 0.3f, bool pole = false, float ground = 0f)
        {
            Vector3 At(float t) => Vector3.Lerp(from, to, t) - new Vector3(0f, sag * 4f * t * (1f - t), 0f);
            var span = to - from;
            var direction = Vector3.Normalize(new Vector3(span.X, 0f, span.Z));
            var side = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitY));
            Cord(builder, At, 20, 0.014f, side, SurfaceMaterial.PaintBlack);

            var length = span.Length();
            var flags = Math.Max(1, (int)(length / (FlagSpacing * flag / 0.3f)));
            for (var i = 0; i < flags; i++)
            {
                var a = At((i + 0.12f) / flags);
                var b = At((i + 0.88f) / flags);
                var tip = ((a + b) * 0.5f) - new Vector3(0f, flag, 0f);
                var colour = _design.Bunting[i % _design.Bunting.Count];
                builder.AddTriangleFacing(a, b, tip, side, colour);
                builder.AddTriangleFacing(a, b, tip, -side, colour);
            }

            if (pole)
            {
                builder.AddPrism(new Vector3(to.X, ground, to.Z), 0.05f, to.Y - ground + 0.1f, 6, SurfaceMaterial.PaintWhite);
            }
        }

        /// <summary>A thin ribbon along a curve, seen from both sides.</summary>
        private static void Cord(MeshBuilder builder, Func<float, Vector3> curve, int segments, float thickness, Vector3 facing, SurfaceMaterial material)
        {
            var down = new Vector3(0f, thickness, 0f);
            for (var i = 0; i < segments; i++)
            {
                var a = curve(i / (float)segments);
                var b = curve((i + 1) / (float)segments);
                builder.AddQuadFacing(a - down, b - down, b, a, facing, material);
                builder.AddQuadFacing(a - down, b - down, b, a, -facing, material);
            }
        }

        /// <summary>A thin square tube from <paramref name="a"/> to <paramref name="b"/>.</summary>
        private static void Tube(MeshBuilder builder, Vector3 a, Vector3 b, float radius, SurfaceMaterial material)
        {
            var span = b - a;
            var length = span.Length();
            if (length < 1e-4f)
            {
                return;
            }

            var along = span / length;
            var reference = MathF.Abs(along.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY;
            var side = Vector3.Normalize(Vector3.Cross(along, reference));
            var up = Vector3.Cross(side, along);
            builder.AddBox((a + b) * 0.5f, along, up, side, new Vector3(length * 0.5f, radius, radius), material);
        }

        /// <summary>The plot in metres: x from its left as seen from the road, d back from the pavement.</summary>
        private readonly struct Site
        {
            private readonly Vector2 _origin;
            private readonly Vector2 _right;
            private readonly Vector2 _in;

            public Site(Footprint footprint)
            {
                _origin = footprint.FrontLeft;
                _right = Vector2.Normalize(footprint.FrontRight - footprint.FrontLeft);
                _in = Vector2.Normalize(footprint.BackLeft - footprint.FrontLeft);
                Width = footprint.FrontWidth;
            }

            public float Width { get; }

            public Vector3 Right3 => new Vector3(_right.X, 0f, _right.Y);

            public Vector3 In3 => new Vector3(_in.X, 0f, _in.Y);

            public Vector2 At(float x, float d) => _origin + (_right * x) + (_in * d);

            public Vector3 At(float x, float y, float d) => GeoMath.At(At(x, d), y);

            public Footprint Rect(float x0, float d0, float x1, float d1) => new Footprint(At(x0, d0), At(x1, d0), At(x1, d1), At(x0, d1));
        }
    }
}
