using System.Collections.Generic;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>A terraced house: a front door to one side with a hood over it, and one or two windows.</summary>
    public sealed class HouseFrontage : IGroundFloorStyle
    {
        private readonly bool _doorOnLeft;

        public HouseFrontage(bool doorOnLeft)
        {
            _doorOnLeft = doorOnLeft;
        }

        public void Build(GroundFloor floor)
        {
            var wall = floor.Wall;
            var builder = floor.Builder;
            var f = floor.Floor;
            var width = wall.Width;

            var doorX = _doorOnLeft ? 0.5f : width - 1.45f;
            var door = new Opening(doorX, f, doorX + 0.95f, f + 2.4f, 0.2f);
            var openings = new List<Opening> { door };

            var windowLeft = _doorOnLeft ? door.X1 + 0.5f : 0.5f;
            var windowRight = _doorOnLeft ? width - 0.5f : door.X0 - 0.5f;
            var count = windowRight - windowLeft > 2.6f ? 2 : 1;
            var span = (windowRight - windowLeft) / count;
            for (var i = 0; i < count; i++)
            {
                var centre = windowLeft + (span * (i + 0.5f));
                openings.Add(new Opening(centre - 0.55f, f + 0.85f, centre + 0.55f, f + 2.35f, 0.14f));
            }

            WallBuilder.Build(builder, wall, f, floor.Top, openings, floor.WallMaterial);
            Glazing.FillDoor(floor.Context, wall, door, floor.DoorPaint, SurfaceMaterial.PaintWhite);
            for (var i = 1; i < openings.Count; i++)
            {
                Glazing.FillWindow(floor.Context, wall, openings[i], floor.Windows);
            }

            DoorHood.Build(builder, wall, door.X0, door.X1, door.Y1 + 0.12f);
            wall.Block(builder, door.X0 - 0.1f, f - 0.15f, door.X1 + 0.1f, f, -0.05f, 0.3f, SurfaceMaterial.Stone);
        }
    }

    /// <summary>A small pitched slate canopy on brackets over a front door.</summary>
    public static class DoorHood
    {
        public static void Build(MeshBuilder builder, WallFrame wall, float x0, float x1, float y)
        {
            const float reach = 0.55f;
            var left = x0 - 0.2f;
            var right = x1 + 0.2f;
            var up = System.Numerics.Vector3.UnitY;
            builder.AddQuadFacing(wall.Point(left, y + 0.35f, 0f), wall.Point(right, y + 0.35f, 0f), wall.Point(right, y, reach), wall.Point(left, y, reach), up + wall.Out, SurfaceMaterial.Slate);
            builder.AddQuadFacing(wall.Point(left, y + 0.29f, 0f), wall.Point(right, y + 0.29f, 0f), wall.Point(right, y - 0.06f, reach), wall.Point(left, y - 0.06f, reach), -up, SurfaceMaterial.PaintWhite);
            builder.AddQuadFacing(wall.Point(left, y - 0.06f, reach), wall.Point(right, y - 0.06f, reach), wall.Point(right, y, reach), wall.Point(left, y, reach), wall.Out, SurfaceMaterial.PaintWhite);
            foreach (var x in new[] { left + 0.06f, right - 0.06f })
            {
                wall.Block(builder, x - 0.04f, y - 0.35f, x + 0.04f, y + 0.25f, 0f, 0.12f, SurfaceMaterial.PaintWhite);
            }
        }
    }
}
