using System;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Buildings.Styles
{
    /// <summary>
    /// A small stone parish church, the village landmark: a west tower with a clock, belfry and
    /// slate spire, a nave with lancet windows and a south porch, and a lower chancel to the east.
    /// The footprint's front is the tower end.
    /// </summary>
    public sealed class ChurchStyle : IBuildingStyle
    {
        private const float TowerSize = 4.6f;
        private const float TowerHeight = 13f;
        private const float SpireHeight = 11f;
        private const float NaveWallHeight = 6f;
        private const float ChancelWallHeight = 5f;
        private const float ChancelLength = 6f;
        private const float ChancelWidth = 6f;

        public void Build(Footprint footprint, BuildContext context)
        {
            var builder = context.Builder;
            var outward = footprint.Outward;
            var right = GeoMath.Left(outward);
            var frontCentre = (footprint.FrontLeft + footprint.FrontRight) * 0.5f;
            var naveWidth = footprint.FrontWidth;
            var naveLength = footprint.Depth - TowerSize - ChancelLength + 0.9f;

            // Tower.
            var tower = Footprint.FromFront(frontCentre, outward, TowerSize, TowerSize);
            BuildTower(context, tower);

            // Nave and chancel are built side-on so their ridges run east-west.
            var naveCentre = frontCentre - (outward * (TowerSize - 0.5f + (naveLength * 0.5f)));
            var nave = Footprint.FromFront(naveCentre + (right * (naveWidth * 0.5f)), right, naveLength, naveWidth);
            BuildBody(context, nave, NaveWallHeight, 50f, lancets: 4);

            var chancelCentre = naveCentre - (outward * ((naveLength * 0.5f) + (ChancelLength * 0.5f) - 0.4f));
            var chancel = Footprint.FromFront(chancelCentre + (right * (ChancelWidth * 0.5f)), right, ChancelLength, ChancelWidth);
            BuildBody(context, chancel, ChancelWallHeight, 50f, lancets: 2);

            // East window in the chancel's gable end (side-on, its right wall faces east).
            var east = chancel.RightWall;
            Lancet(builder, east, (east.Width * 0.5f) - 0.55f, BuildingLevels.Floor + 1.4f, (east.Width * 0.5f) + 0.55f, BuildingLevels.Floor + 3.8f);

            // South porch near the west end, its gable and doorway facing south.
            var porchCentre = naveCentre + (outward * ((naveLength * 0.5f) - 2.2f)) + (right * ((naveWidth * 0.5f) + 1.2f));
            var porch = Footprint.FromFront(porchCentre + (outward * 1.3f), outward, 2.4f, 2.6f);
            BuildPorch(context, porch);
        }

        private static void BuildTower(BuildContext context, Footprint tower)
        {
            var builder = context.Builder;
            var top = BuildingLevels.Floor + TowerHeight;
            foreach (var wall in new[] { tower.FrontWall, tower.RightWall, tower.BackWall, tower.LeftWall })
            {
                wall.Quad(builder, 0f, BuildingLevels.Base, wall.Width, top, 0f, SurfaceMaterial.Stone);

                // String courses marking the stages.
                foreach (var y in new[] { 4.8f, 9.2f })
                {
                    wall.Block(builder, -0.05f, BuildingLevels.Floor + y, wall.Width + 0.05f, BuildingLevels.Floor + y + 0.18f, 0f, 0.1f, SurfaceMaterial.StoneDark);
                }

                // Louvred belfry opening.
                var cx = wall.Width * 0.5f;
                var y0 = BuildingLevels.Floor + 10.1f;
                Lancet(builder, wall, cx - 0.5f, y0, cx + 0.5f, y0 + 1.6f, SurfaceMaterial.PaintBlack);
                for (var k = 0; k < 5; k++)
                {
                    var y = y0 + 0.15f + (k * 0.3f);
                    wall.Block(builder, cx - 0.45f, y, cx + 0.45f, y + 0.06f, 0.012f, 0.06f, SurfaceMaterial.Timber);
                }
            }

            // Parapet and spire.
            var centre = GeoMath.At(tower.Centre, top);
            var along = GeoMath.At(Vector2.Normalize(tower.FrontRight - tower.FrontLeft), 0f);
            var forward = GeoMath.At(tower.Outward, 0f);
            builder.AddBox(centre + new Vector3(0f, 0.2f, 0f), along, Vector3.UnitY, forward, new Vector3((TowerSize * 0.5f) + 0.12f, 0.2f, (TowerSize * 0.5f) + 0.12f), SurfaceMaterial.StoneDark, includeBottom: true);
            builder.AddPyramid(centre + new Vector3(0f, 0.4f, 0f), along, forward, new Vector2((TowerSize * 0.5f) - 0.2f, (TowerSize * 0.5f) - 0.2f), SpireHeight, Vector3.UnitY, SurfaceMaterial.Slate);
            var tip = centre + new Vector3(0f, 0.4f + SpireHeight, 0f);
            builder.AddBox(tip + new Vector3(0f, 0.6f, 0f), along, Vector3.UnitY, forward, new Vector3(0.03f, 0.6f, 0.03f), SurfaceMaterial.PaintGold);
            builder.AddBox(tip + new Vector3(0f, 0.95f, 0f), along, Vector3.UnitY, forward, new Vector3(0.35f, 0.025f, 0.03f), SurfaceMaterial.PaintGold);

            // West door and clock.
            var west = tower.FrontWall;
            var mid = west.Width * 0.5f;
            Lancet(builder, west, mid - 0.75f, BuildingLevels.Base, mid + 0.75f, BuildingLevels.Floor + 2.4f, SurfaceMaterial.StoneDark, 0.006f);
            Lancet(builder, west, mid - 0.6f, BuildingLevels.Floor, mid + 0.6f, BuildingLevels.Floor + 2.3f, SurfaceMaterial.Timber, 0.012f);
            Clock(builder, west, mid, BuildingLevels.Floor + 7.2f);
        }

        private static void BuildBody(BuildContext context, Footprint body, float wallHeight, float pitch, int lancets)
        {
            var builder = context.Builder;
            var eaves = BuildingLevels.Floor + wallHeight;
            var ridge = eaves + (body.Depth * 0.5f * MathF.Tan(pitch * MathF.PI / 180f));
            foreach (var wall in new[] { body.FrontWall, body.BackWall })
            {
                wall.Quad(builder, 0f, BuildingLevels.Base, wall.Width, eaves, 0f, SurfaceMaterial.Stone);
                for (var i = 0; i < lancets; i++)
                {
                    var cx = wall.Width * (i + 0.5f) / lancets;
                    Lancet(builder, wall, cx - 0.42f, BuildingLevels.Floor + 1.6f, cx + 0.42f, eaves - 1.3f);
                }
            }

            foreach (var wall in new[] { body.LeftWall, body.RightWall })
            {
                wall.Quad(builder, 0f, BuildingLevels.Base, wall.Width, eaves, 0f, SurfaceMaterial.Stone);
                GableRoof.GableWall(builder, wall, eaves, ridge, SurfaceMaterial.Stone);
            }

            new GableRoof(body, eaves, ridge, overhang: 0.35f, verge: 0.3f, thickness: 0.18f).Build(builder, SurfaceMaterial.Slate, SurfaceMaterial.StoneDark);
        }

        private static void BuildPorch(BuildContext context, Footprint porch)
        {
            var builder = context.Builder;
            var eaves = BuildingLevels.Floor + 2.6f;
            var ridge = eaves + 1.5f;
            foreach (var wall in new[] { porch.FrontWall, porch.BackWall })
            {
                wall.Quad(builder, 0f, BuildingLevels.Base, wall.Width, eaves, 0f, SurfaceMaterial.Stone);
            }

            foreach (var wall in new[] { porch.LeftWall, porch.RightWall })
            {
                wall.Quad(builder, 0f, BuildingLevels.Base, wall.Width, eaves, 0f, SurfaceMaterial.Stone);
                GableRoof.GableWall(builder, wall, eaves, ridge, SurfaceMaterial.Stone);
            }

            var entrance = porch.RightWall;
            Lancet(builder, entrance, (entrance.Width * 0.5f) - 0.6f, BuildingLevels.Floor, (entrance.Width * 0.5f) + 0.6f, BuildingLevels.Floor + 2.2f, SurfaceMaterial.PaintBlack, 0.012f);

            new GableRoof(porch, eaves, ridge, overhang: 0.2f, verge: 0.2f).Build(builder, SurfaceMaterial.Slate, SurfaceMaterial.StoneDark);
        }

        /// <summary>A pointed-arch window or doorway drawn on the wall, with a stone surround.</summary>
        private static void Lancet(MeshBuilder builder, WallFrame wall, float x0, float y0, float x1, float y1, SurfaceMaterial fill = SurfaceMaterial.WindowGlass, float z = 0.012f)
        {
            var arch = (x1 - x0) * 0.75f;
            var xc = (x0 + x1) * 0.5f;
            const float border = 0.12f;
            wall.Polygon(builder, z * 0.5f, SurfaceMaterial.Kerb, new Vector2(x0 - border, y0), new Vector2(x1 + border, y0), new Vector2(x1 + border, y1), new Vector2(xc, y1 + arch + (border * 1.6f)), new Vector2(x0 - border, y1));
            wall.Polygon(builder, z, fill, new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(xc, y1 + arch), new Vector2(x0, y1));
        }

        private static void Clock(MeshBuilder builder, WallFrame wall, float cx, float cy)
        {
            const int sides = 12;
            var rim = new Vector2[sides];
            var face = new Vector2[sides];
            for (var i = 0; i < sides; i++)
            {
                var angle = MathF.PI * 2f * i / sides;
                rim[i] = new Vector2(cx + (MathF.Cos(angle) * 0.75f), cy + (MathF.Sin(angle) * 0.75f));
                face[i] = new Vector2(cx + (MathF.Cos(angle) * 0.65f), cy + (MathF.Sin(angle) * 0.65f));
            }

            wall.Polygon(builder, 0.01f, SurfaceMaterial.PaintGold, rim);
            wall.Polygon(builder, 0.02f, SurfaceMaterial.PaintWhite, face);

            // Hands at ten to two.
            wall.Polygon(builder, 0.03f, SurfaceMaterial.PaintBlack, new Vector2(cx - 0.02f, cy), new Vector2(cx + 0.02f, cy), new Vector2(cx + 0.28f, cy + 0.42f), new Vector2(cx + 0.24f, cy + 0.44f));
            wall.Polygon(builder, 0.03f, SurfaceMaterial.PaintBlack, new Vector2(cx - 0.02f, cy - 0.02f), new Vector2(cx + 0.02f, cy + 0.02f), new Vector2(cx - 0.32f, cy + 0.2f), new Vector2(cx - 0.34f, cy + 0.17f));
        }
    }
}
