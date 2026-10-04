using System;
using System.Collections.Generic;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Styles
{
    /// <summary>The choices that make one terraced unit different from its neighbours.</summary>
    public sealed class UnitDesign
    {
        public float GroundFloorHeight { get; set; } = 3.45f;

        public float UpperFloorHeight { get; set; } = 2.75f;

        public int UpperFloors { get; set; } = 2;

        public SurfaceMaterial Wall { get; set; } = SurfaceMaterial.Stone;

        public SurfaceMaterial Trim { get; set; } = SurfaceMaterial.PaintBlack;

        public WindowStyle Windows { get; set; } = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.TwoOverTwo);

        public SurfaceMaterial DoorPaint { get; set; } = SurfaceMaterial.PaintNavy;

        public IGroundFloorStyle GroundFloor { get; set; }

        public ShopDefinition Shop { get; set; }

        public float Eaves { get; set; }

        public float Ridge { get; set; }

        public bool Dormer { get; set; }

        /// <summary>Chimney stack on the left party wall; <see cref="LeftNeighbourRidge"/> keeps it above the taller roof.</summary>
        public bool ChimneyLeft { get; set; } = true;

        public bool ChimneyRight { get; set; }

        public float LeftNeighbourRidge { get; set; }

        public float RightNeighbourRidge { get; set; }

        public int Pots { get; set; } = 3;

        /// <summary>Flower boxes under the first-floor windows.</summary>
        public bool WindowBoxes { get; set; }
    }

    /// <summary>
    /// One unit of a terrace: a shop or house on the ground floor, homes on the floors above
    /// with sash windows, a slate roof running along the street, and chimneys on the party walls.
    /// A unit whose shop you can go into (<see cref="ShopDefinition.Enterable"/>) is built inside
    /// too, as a café with a flat above, and its windows are clear glass you can see through.
    /// </summary>
    public sealed class TerracedUnitStyle : IBuildingStyle
    {
        private readonly UnitDesign _design;

        public TerracedUnitStyle(UnitDesign design)
        {
            _design = design;
        }

        public UnitDesign Design => _design;

        /// <summary>Whether this unit is built with rooms you can walk into.</summary>
        public bool Enterable => _design.Shop != null && _design.Shop.Enterable;

        public void Build(Footprint footprint, BuildContext context)
        {
            var builder = context.Builder;
            var design = _design;
            var enterable = Enterable && CafeAndFlat.Fits(footprint);
            var windows = enterable ? Clear(design.Windows) : design.Windows;
            var front = footprint.FrontWall;
            var floor = BuildingLevels.Floor;
            var upperBase = floor + design.GroundFloorHeight;

            Courses.Plinth(builder, front);
            var ground = new GroundFloor(context, front, floor, design.GroundFloorHeight, design.Wall, design.Windows, design.DoorPaint, design.Shop);
            design.GroundFloor.Build(ground);

            BuildUpperFront(context, front, upperBase, windows);
            Courses.String(builder, front, upperBase, design.Windows.Sill);
            BuildSidesAndBack(context, footprint, enterable);

            var roof = new GableRoof(footprint, design.Eaves, design.Ridge, overhang: 0.28f, verge: 0f);
            var roofWindows = enterable ? CafeAndFlat.RoofWindows(footprint, design) : null;
            roof.Build(builder, SurfaceMaterial.Slate, design.Trim, roofWindows);
            for (var i = 0; i < (roofWindows?.Length ?? 0); i++)
            {
                RoofWindow.Build(context, roof, roofWindows[i], $"Roof window {i + 1}");
            }

            var along = System.Numerics.Vector2.Normalize(footprint.FrontRight - footprint.FrontLeft);
            if (design.ChimneyLeft)
            {
                Stack(context, footprint.At(0f, 0.5f), along, design.LeftNeighbourRidge);
            }

            if (design.ChimneyRight)
            {
                Stack(context, footprint.At(1f, 0.5f), along, design.RightNeighbourRidge);
            }

            Dormer.Span? dormer = null;
            if (design.Dormer)
            {
                var dormerWindows = new WindowStyle(design.Windows.Frame, GlazingPattern.Casement, SurfaceMaterial.StoneDark);
                var width = Math.Min(1.6f, footprint.FrontWidth * 0.3f);
                Dormer.Build(context, footprint, roof, 0.5f, width, design.Wall, enterable ? Clear(dormerWindows) : dormerWindows, seeThrough: enterable);
                dormer = Dormer.Measure(footprint, roof, 0.5f, width);
            }

            if (enterable)
            {
                CafeAndFlat.Build(context, footprint, design, roof, dormer, roofWindows);
            }
        }

        // The same windows with clear glass, for rooms you can look into and out of.
        private static WindowStyle Clear(WindowStyle style) =>
            new WindowStyle(style.Frame, style.Pattern, style.Sill, style.Surround) { Glass = SurfaceMaterial.ClearGlass };

        private void BuildUpperFront(BuildContext context, WallFrame front, float upperBase, WindowStyle windows)
        {
            var design = _design;
            var width = front.Width;
            var columns = width >= 6.2f ? 3 : (width >= 4.2f ? 2 : 1);
            var halfWidth = width < 4.8f ? 0.45f : 0.5f;
            var openings = new List<Opening>();
            for (var floor = 0; floor < design.UpperFloors; floor++)
            {
                var levelBase = upperBase + (floor * design.UpperFloorHeight);
                var sill = levelBase + (floor == 0 ? 0.85f : 0.8f);
                var height = floor == 0 ? 1.5f : 1.3f;
                for (var i = 0; i < columns; i++)
                {
                    var centre = width * (i + 0.5f) / columns;
                    openings.Add(new Opening(centre - halfWidth, sill, centre + halfWidth, sill + height, 0.15f));
                }
            }

            WallBuilder.Build(context.Builder, front, upperBase, design.Eaves, openings, design.Wall);
            for (var i = 0; i < openings.Count; i++)
            {
                Glazing.FillWindow(context, front, openings[i], windows);
                if (design.WindowBoxes && i < columns)
                {
                    WindowBox.Build(context, front, openings[i].X0, openings[i].X1, openings[i].Y0, design.DoorPaint);
                }
            }
        }

        private void BuildSidesAndBack(BuildContext context, Footprint footprint, bool enterable)
        {
            var builder = context.Builder;
            var design = _design;
            foreach (var side in new[] { footprint.LeftWall, footprint.RightWall })
            {
                side.Quad(builder, 0f, BuildingLevels.Base, side.Width, design.Eaves, 0f, design.Wall);
                GableRoof.GableWall(builder, side, design.Eaves, design.Ridge, design.Wall);
            }

            var back = footprint.BackWall;
            var plain = new WindowStyle(design.Windows.Frame, GlazingPattern.Casement, SurfaceMaterial.StoneDark);
            var floor = BuildingLevels.Floor;
            if (enterable)
            {
                BuildOpenBack(context, footprint, back, Clear(plain));
                return;
            }

            back.Quad(builder, 0f, BuildingLevels.Base, back.Width, design.Eaves, 0f, design.Wall);
            Glazing.AppliedWindow(context, back, (back.Width * 0.3f) - 0.45f, floor + 0.9f, (back.Width * 0.3f) + 0.45f, floor + 2.1f, plain);
            back.Quad(builder, (back.Width * 0.7f) - 0.45f, floor, (back.Width * 0.7f) + 0.45f, floor + 2.1f, 0.012f, design.DoorPaint);
            var y = floor + design.GroundFloorHeight;
            for (var i = 0; i < design.UpperFloors; i++)
            {
                Glazing.AppliedWindow(context, back, (back.Width * 0.5f) - 0.5f, y + 0.85f, (back.Width * 0.5f) + 0.5f, y + 2.1f, plain);
                y += design.UpperFloorHeight;
            }
        }

        // The back of a unit you can go into: real windows (where the interior expects them)
        // instead of ones drawn on the wall, and the back door, which stays shut.
        private void BuildOpenBack(BuildContext context, Footprint footprint, WallFrame back, WindowStyle clear)
        {
            var design = _design;
            var space = new UnitSpace(footprint);
            var floors = CafeAndFlat.Floors(design);
            var openings = new List<Opening>();
            for (var i = 0; i < floors.Length - 1; i++)
            {
                // The back wall's x runs from the right of the unit as seen from the street.
                var hole = CafeAndFlat.BackWindow(space, floors[i], ground: i == 0);
                openings.Add(new Opening(back.Width - hole.To, hole.Bottom, back.Width - hole.From, hole.Top, 0.15f));
            }

            WallBuilder.Build(context.Builder, back, BuildingLevels.Base, design.Eaves, openings, design.Wall);
            foreach (var opening in openings)
            {
                Glazing.FillWindow(context, back, opening, clear);
            }

            var floor = BuildingLevels.Floor;
            back.Quad(context.Builder, (back.Width * 0.7f) - 0.45f, floor, (back.Width * 0.7f) + 0.45f, floor + 2.1f, 0.012f, design.DoorPaint);
        }

        private void Stack(BuildContext context, System.Numerics.Vector2 centre, System.Numerics.Vector2 along, float neighbourRidge)
        {
            var top = Math.Max(_design.Ridge, neighbourRidge) + 0.95f;
            var bottom = Math.Min(_design.Ridge, neighbourRidge > 0f ? neighbourRidge : _design.Ridge) - 0.7f;
            Chimney.Stack(context, centre, along, bottom, top, 1.25f, 0.62f, _design.Wall, _design.Pots);
        }
    }
}
