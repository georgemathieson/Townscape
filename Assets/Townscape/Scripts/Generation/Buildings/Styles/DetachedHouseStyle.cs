using System;
using System.Collections.Generic;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Styles
{
    /// <summary>Settings for a free-standing building: a cottage, a detached shop or the old mill.</summary>
    public sealed class HouseDesign
    {
        public int Floors { get; init; } = 2;

        /// <summary>Window columns across the front. With an odd number the door goes in the middle.</summary>
        public int Bays { get; init; } = 3;

        public float FloorHeight { get; init; } = 2.7f;

        public SurfaceMaterial Wall { get; init; } = SurfaceMaterial.RenderWhite;

        public SurfaceMaterial Trim { get; init; } = SurfaceMaterial.PaintBlack;

        public WindowStyle Windows { get; init; } = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.Casement, SurfaceMaterial.Stone, SurfaceMaterial.PaintBlack);

        public SurfaceMaterial DoorPaint { get; init; } = SurfaceMaterial.PaintDarkGreen;

        public float Pitch { get; init; } = 40f;

        /// <summary>A shop on the ground floor (a detached shop), or null for a home.</summary>
        public ShopDefinition Shop { get; init; }

        /// <summary>Text on a board over the door, for named buildings like the mill.</summary>
        public string Sign { get; init; }

        public int Pots { get; init; } = 2;
    }

    /// <summary>
    /// A detached Lake District building: whitewash or stone, symmetrical front, slate roof with
    /// overhanging verges, a chimney at each gable end, and a hood over the door.
    /// </summary>
    public sealed class DetachedHouseStyle : IBuildingStyle
    {
        private const float ShopFloorHeight = 3.45f;

        private readonly HouseDesign _design;

        public DetachedHouseStyle(HouseDesign design)
        {
            _design = design;
        }

        public void Build(Footprint footprint, BuildContext context)
        {
            var builder = context.Builder;
            var design = _design;
            var floor = BuildingLevels.Floor;
            var groundHeight = design.Shop != null ? ShopFloorHeight : design.FloorHeight;
            var eaves = floor + groundHeight + ((design.Floors - 1) * design.FloorHeight);
            var ridge = eaves + (footprint.Depth * 0.5f * MathF.Tan(design.Pitch * MathF.PI / 180f));

            var front = footprint.FrontWall;
            Courses.Plinth(builder, front);
            BuildFront(context, front, floor, groundHeight, eaves);
            if (design.Shop != null)
            {
                Courses.String(builder, front, floor + groundHeight, design.Windows.Sill);
            }

            var gableWindows = new WindowStyle(design.Windows.Frame, GlazingPattern.Casement, design.Windows.Sill, design.Windows.Surround);
            foreach (var side in new[] { footprint.LeftWall, footprint.RightWall })
            {
                side.Quad(builder, 0f, BuildingLevels.Base, side.Width, eaves, 0f, design.Wall);
                GableRoof.GableWall(builder, side, eaves, ridge, design.Wall);
                if (design.Floors > 1)
                {
                    Glazing.AppliedWindow(context, side, (side.Width * 0.5f) - 0.4f, eaves - 1.6f, (side.Width * 0.5f) + 0.4f, eaves - 0.5f, gableWindows);
                }
            }

            var back = footprint.BackWall;
            back.Quad(builder, 0f, BuildingLevels.Base, back.Width, eaves, 0f, design.Wall);
            for (var level = 0; level < design.Floors; level++)
            {
                var y = floor + (level == 0 ? 0f : groundHeight + ((level - 1) * design.FloorHeight));
                Glazing.AppliedWindow(context, back, (back.Width * 0.35f) - 0.45f, y + 0.9f, (back.Width * 0.35f) + 0.45f, y + 2.0f, gableWindows);
            }

            var roof = new GableRoof(footprint, eaves, ridge, overhang: 0.3f, verge: 0.25f);
            roof.Build(builder, SurfaceMaterial.Slate, design.Trim);

            var depthwise = System.Numerics.Vector2.Normalize(footprint.BackLeft - footprint.FrontLeft);
            foreach (var a in new[] { 0.05f, 0.95f })
            {
                Chimney.Stack(context, footprint.At(a, 0.5f), depthwise, ridge - 0.8f, ridge + 0.85f, 0.85f, 0.55f, design.Wall, design.Pots);
            }
        }

        private void BuildFront(BuildContext context, WallFrame front, float floor, float groundHeight, float eaves)
        {
            var builder = context.Builder;
            var design = _design;
            var bays = Math.Max(1, design.Bays);
            var doorBay = bays / 2;
            var upperBase = floor + groundHeight;

            if (design.Shop != null)
            {
                var ground = new GroundFloor(context, front, floor, groundHeight, design.Wall, design.Windows, design.DoorPaint, design.Shop);
                (design.Shop.Frontage ?? TraditionalShopfront.Instance).Build(ground);
            }

            var openings = new List<Opening>();
            Opening door = default;
            for (var bay = 0; bay < bays; bay++)
            {
                var centre = front.Width * (bay + 0.5f) / bays;
                if (design.Shop == null)
                {
                    if (bay == doorBay)
                    {
                        door = new Opening(centre - 0.48f, floor, centre + 0.48f, floor + 2.35f, 0.2f);
                        openings.Add(door);
                    }
                    else
                    {
                        openings.Add(new Opening(centre - 0.5f, floor + 0.85f, centre + 0.5f, floor + 2.2f, 0.16f));
                    }
                }

                for (var level = 1; level < design.Floors; level++)
                {
                    var sill = upperBase + ((level - 1) * design.FloorHeight) + 0.8f;
                    openings.Add(new Opening(centre - 0.45f, sill, centre + 0.45f, sill + 1.25f, 0.16f));
                }
            }

            var wallBottom = design.Shop != null ? upperBase : floor;
            WallBuilder.Build(builder, front, wallBottom, eaves, openings, design.Wall);
            foreach (var opening in openings)
            {
                if (design.Shop == null && opening.Y0 <= floor + 1e-3f)
                {
                    Glazing.FillDoor(context, front, opening, design.DoorPaint, SurfaceMaterial.PaintWhite);
                    DoorHood.Build(builder, front, opening.X0, opening.X1, opening.Y1 + 0.12f);
                    front.Block(builder, opening.X0 - 0.1f, floor - 0.15f, opening.X1 + 0.1f, floor, -0.05f, 0.3f, SurfaceMaterial.Stone);
                }
                else
                {
                    Glazing.FillWindow(context, front, opening, design.Windows);
                }
            }

            if (!string.IsNullOrEmpty(design.Sign) && design.Shop == null)
            {
                var y = upperBase + 0.65f;
                var boardWidth = Math.Min(front.Width - 1f, (PixelFont.PixelWidth(design.Sign) * 0.05f) + 0.5f);
                var centre = front.Width * 0.5f;
                front.Block(builder, centre - (boardWidth * 0.5f), y - 0.5f, centre + (boardWidth * 0.5f), y, 0f, 0.06f, SurfaceMaterial.PaintBlack);
                PixelFont.Write(builder, front, design.Sign, centre, y - 0.25f, 0.045f, boardWidth - 0.3f, 0.064f, SurfaceMaterial.PaintCream);
            }
        }
    }
}
