using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Styles
{
    /// <summary>
    /// The old mill by the beck, now Mill Works: three tall storeys of stone with five bays of
    /// small-paned windows in black frames, a slate roof with a chimney at each gable, and a
    /// glazed front door under a hood with the name on a board above it. Every window is real,
    /// with clear glass, and the door opens: inside are the co-working floors and the alarm
    /// receiving centre (<see cref="MillWorks"/>).
    /// </summary>
    public sealed class MillWorksStyle : IBuildingStyle
    {
        private const float Pitch = 35f;
        private const SurfaceMaterial Wall = SurfaceMaterial.Stone;
        private const SurfaceMaterial DoorPaint = SurfaceMaterial.PaintDarkGreen;

        private static readonly WindowStyle Windows = new WindowStyle(SurfaceMaterial.PaintBlack, GlazingPattern.SixOverSix, SurfaceMaterial.Kerb) { Glass = SurfaceMaterial.ClearGlass };

        public void Build(Footprint footprint, BuildContext context)
        {
            var builder = context.Builder;
            var space = new UnitSpace(footprint);
            var floor = BuildingLevels.Floor;
            var eaves = MillWorks.Eaves;
            var ridge = eaves + (footprint.Depth * 0.5f * MathF.Tan(Pitch * MathF.PI / 180f));

            // Each wall with its windows cut right through, found on the wall straight out from
            // where the rooms inside put them.
            var front = footprint.FrontWall;
            var door = MillWorks.Door(space);
            var doorway = new Opening(door.From, door.Bottom, door.To, door.Top, MillWorks.Shell);
            var frontWindows = MillWorks.FrontWindows(space).Select(h => Cut(space, front, h, d: 0f)).ToList();
            Courses.Plinth(builder, front);
            Walls(context, front, frontWindows.Append(doorway).ToList(), frontWindows, eaves);
            Walls(context, footprint.BackWall, MillWorks.BackWindows(space).Select(h => Cut(space, footprint.BackWall, h, d: space.Depth)).ToList(), null, eaves);
            Walls(context, footprint.LeftWall, MillWorks.LeftWindows(space).Select(h => CutAlong(space, footprint.LeftWall, h, x: 0f)).ToList(), null, eaves);
            Walls(context, footprint.RightWall, MillWorks.RightWindows(space).Select(h => CutAlong(space, footprint.RightWall, h, x: space.Width)).ToList(), null, eaves);
            foreach (var side in new[] { footprint.LeftWall, footprint.RightWall })
            {
                GableRoof.GableWall(builder, side, eaves, ridge, Wall);
            }

            FrontDoor(context, front, doorway);
            Sign(builder, front, doorway, floor);

            var roof = new GableRoof(footprint, eaves, ridge, overhang: 0.3f, verge: 0.25f);
            roof.Build(builder, SurfaceMaterial.Slate, SurfaceMaterial.Timber);
            var depthwise = Vector2.Normalize(footprint.BackLeft - footprint.FrontLeft);
            foreach (var a in new[] { 0.05f, 0.95f })
            {
                Chimney.Stack(context, footprint.At(a, 0.5f), depthwise, ridge - 0.8f, ridge + 0.85f, 0.85f, 0.55f, Wall, 3);
            }

            MillWorks.Build(context, footprint);
        }

        // A hole in a wall running across the building (the front or back, at depth d), on that wall.
        private static Opening Cut(UnitSpace space, WallFrame wall, Hole hole, float d)
        {
            var a = On(space, wall, hole.From, d);
            var b = On(space, wall, hole.To, d);
            return new Opening(Math.Min(a, b), hole.Bottom, Math.Max(a, b), hole.Top, MillWorks.Shell);
        }

        // A hole in a wall running back (a gable, at x), given back (d) and up, on that wall.
        private static Opening CutAlong(UnitSpace space, WallFrame wall, Hole hole, float x)
        {
            var a = On(space, wall, x, hole.From);
            var b = On(space, wall, x, hole.To);
            return new Opening(Math.Min(a, b), hole.Bottom, Math.Max(a, b), hole.Top, MillWorks.Shell);
        }

        private static float On(UnitSpace space, WallFrame wall, float x, float d) => Vector3.Dot(space.At(x, 0f, d) - wall.Origin, wall.Right);

        // A stone wall with its openings, and clear glass and black frames in each window.
        private static void Walls(BuildContext context, WallFrame wall, List<Opening> openings, List<Opening> windows, float eaves)
        {
            WallBuilder.Build(context.Builder, wall, BuildingLevels.Base, eaves, openings, Wall);
            foreach (var window in windows ?? openings)
            {
                Glazing.FillWindow(context, wall, window, Windows);
            }
        }

        // A glazed door in a dark green frame that opens into reception, with a stone step and a
        // slate hood over it.
        private static void FrontDoor(BuildContext context, WallFrame wall, Opening doorway)
        {
            var builder = context.Builder;
            var floor = BuildingLevels.Floor;
            var back = -doorway.Depth;
            var left = doorway.X0;
            var right = doorway.X1;
            var top = doorway.Y1;
            var hinge = wall.Point(left, floor, back - 0.02f);
            context.Door(MillWorks.DoorName, hinge, wall.Right, -wall.Out, doorway.Width, top - floor, door =>
            {
                var b = door.Builder;
                wall.Quad(b, left, floor + 0.3f, right, top - 0.1f, back, SurfaceMaterial.ClearGlass);
                wall.QuadInward(b, left, floor + 0.3f, right, top - 0.1f, back - 0.04f, SurfaceMaterial.ClearGlass);
                wall.Block(b, left, floor, right, floor + 0.3f, back - 0.04f, back, DoorPaint);
                wall.QuadInward(b, left, floor, right, floor + 0.3f, back - 0.04f, DoorPaint);
                wall.Block(b, left, top - 0.1f, right, top, back - 0.04f, back, DoorPaint);
                wall.QuadInward(b, left, top - 0.1f, right, top, back - 0.04f, DoorPaint);
                foreach (var (x0, x1) in new[] { (left, left + 0.08f), (right - 0.08f, right) })
                {
                    wall.Block(b, x0, floor + 0.3f, x1, top - 0.1f, back - 0.04f, back, DoorPaint);
                    wall.QuadInward(b, x0, floor + 0.3f, x1, top - 0.1f, back - 0.04f, DoorPaint);
                }

                wall.Block(b, right - 0.2f, floor + 0.9f, right - 0.16f, floor + 1.4f, back, back + 0.05f, SurfaceMaterial.Chrome);
                wall.Block(b, right - 0.2f, floor + 0.9f, right - 0.16f, floor + 1.4f, back - 0.09f, back - 0.04f, SurfaceMaterial.Chrome);
            });

            DoorHood.Build(builder, wall, left, right, top + 0.12f);
            wall.Block(builder, left - 0.1f, floor - 0.15f, right + 0.1f, floor, -0.05f, 0.3f, SurfaceMaterial.Stone);
        }

        // The name on a board over the door, between it and the window above.
        private static void Sign(MeshBuilder builder, WallFrame wall, Opening doorway, float floor)
        {
            const string text = "MILL WORKS";
            var y = floor + MillWorks.FloorHeight + 0.6f;
            var width = (PixelFont.PixelWidth(text) * 0.05f) + 0.5f;
            var centre = doorway.CentreX;
            wall.Block(builder, centre - (width * 0.5f), y - 0.5f, centre + (width * 0.5f), y, 0f, 0.06f, SurfaceMaterial.PaintBlack);
            PixelFont.Write(builder, wall, text, centre, y - 0.25f, 0.045f, width - 0.3f, 0.064f, SurfaceMaterial.PaintCream);
        }
    }
}
