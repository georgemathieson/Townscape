using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    public enum GlazingPattern
    {
        /// <summary>Victorian sash: two sashes, each split into two panes.</summary>
        TwoOverTwo,

        /// <summary>Georgian sash: two sashes of six small panes.</summary>
        SixOverSix,

        /// <summary>Cottage casement: two lights with a transom.</summary>
        Casement,

        /// <summary>A single pane, for small or modern windows.</summary>
        Plain,
    }

    /// <summary>How a window looks: frame colour, glazing bars, and its sill.</summary>
    public sealed class WindowStyle
    {
        public WindowStyle(SurfaceMaterial frame, GlazingPattern pattern, SurfaceMaterial sill = SurfaceMaterial.Stone, SurfaceMaterial? surround = null)
        {
            Frame = frame;
            Pattern = pattern;
            Sill = sill;
            Surround = surround;
        }

        public SurfaceMaterial Frame { get; }

        public GlazingPattern Pattern { get; }

        public SurfaceMaterial Sill { get; }

        /// <summary>Painted band round the window, common on whitewashed Lake District houses.</summary>
        public SurfaceMaterial? Surround { get; }

        public SurfaceMaterial Glass { get; init; } = SurfaceMaterial.WindowGlass;

        /// <summary>
        /// Clear glass you can look through from inside too: the glass has a face on each side
        /// and the frames stand proud of it on both.
        /// </summary>
        public bool SeeThrough => Glass == SurfaceMaterial.ClearGlass;
    }

    /// <summary>Window and door infill for openings, plus "applied" windows for walls without holes.</summary>
    public static class Glazing
    {
        private const float FrameWidth = 0.075f;
        private const float BarWidth = 0.04f;
        private const float BarDepth = 0.04f;

        // How far the frames of a see-through window stand proud of the glass on the inside.
        private const float InsideDepth = 0.04f;

        /// <summary>Glass and frames at the back of a recessed opening, plus a sill and optional surround.</summary>
        public static void FillWindow(BuildContext context, WallFrame wall, Opening opening, WindowStyle style, AnchorKind anchor = AnchorKind.Window)
        {
            var builder = context.Builder;
            var back = -opening.Depth;
            wall.Quad(builder, opening.X0, opening.Y0, opening.X1, opening.Y1, back, GlassFor(context, style));
            if (style.SeeThrough)
            {
                wall.QuadInward(builder, opening.X0, opening.Y0, opening.X1, opening.Y1, back, style.Glass);
            }

            Frames(builder, wall, opening.X0, opening.Y0, opening.X1, opening.Y1, back, style);
            SillAndSurround(builder, wall, opening.X0, opening.Y0, opening.X1, opening.Y1, style);

            var centre = wall.Point(opening.CentreX, (opening.Y0 + opening.Y1) * 0.5f, back);
            context.Anchor(anchor, centre, wall.Out, opening.Width);
        }

        /// <summary>A window drawn onto the face of a wall without cutting a hole, for backs and small walls.</summary>
        public static void AppliedWindow(BuildContext context, WallFrame wall, float x0, float y0, float x1, float y1, WindowStyle style, bool anchor = true)
        {
            var builder = context.Builder;
            wall.Quad(builder, x0, y0, x1, y1, 0.012f, GlassFor(context, style));
            Frames(builder, wall, x0, y0, x1, y1, 0.012f, style);
            SillAndSurround(builder, wall, x0, y0, x1, y1, style);
            if (anchor)
            {
                context.Anchor(AnchorKind.Window, wall.Point((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f), wall.Out, x1 - x0);
            }
        }

        /// <summary>A panelled front door at the back of a recessed opening, with a glazed fanlight above.</summary>
        public static void FillDoor(BuildContext context, WallFrame wall, Opening opening, SurfaceMaterial paint, SurfaceMaterial frame, bool fanlight = true)
        {
            var builder = context.Builder;
            var back = -opening.Depth;
            var doorTop = fanlight ? opening.Y1 - FanlightHeight : opening.Y1;
            wall.Quad(builder, opening.X0, opening.Y0, opening.X1, doorTop, back, paint);

            // Four raised panels.
            var inset = 0.1f;
            var midX = opening.CentreX;
            var midY = opening.Y0 + ((doorTop - opening.Y0) * 0.45f);
            var panelFront = back + 0.02f;
            wall.Block(builder, opening.X0 + inset, opening.Y0 + inset, midX - 0.05f, midY - 0.05f, back, panelFront, paint);
            wall.Block(builder, midX + 0.05f, opening.Y0 + inset, opening.X1 - inset, midY - 0.05f, back, panelFront, paint);
            wall.Block(builder, opening.X0 + inset, midY + 0.05f, midX - 0.05f, doorTop - inset, back, panelFront, paint);
            wall.Block(builder, midX + 0.05f, midY + 0.05f, opening.X1 - inset, doorTop - inset, back, panelFront, paint);

            if (fanlight)
            {
                Fanlight(context, wall, opening, frame);
            }
        }

        /// <summary>The glazed fanlight over a door in <paramref name="opening"/>, which fills its top 0.38 m.</summary>
        public static void Fanlight(BuildContext context, WallFrame wall, Opening opening, SurfaceMaterial frame)
        {
            var builder = context.Builder;
            var back = -opening.Depth;
            var doorTop = opening.Y1 - FanlightHeight;
            var midX = opening.CentreX;
            wall.Quad(builder, opening.X0, doorTop, opening.X1, opening.Y1, back, HomeWindowGroup(context));
            wall.Block(builder, opening.X0, doorTop, opening.X1, doorTop + 0.06f, back, back + BarDepth, frame);
            wall.Block(builder, midX - (BarWidth * 0.5f), doorTop, midX + (BarWidth * 0.5f), opening.Y1, back, back + BarDepth, frame);
            context.Anchor(AnchorKind.Fanlight, wall.Point(opening.CentreX, (doorTop + opening.Y1) * 0.5f, back), wall.Out, opening.Width);
        }

        /// <summary>How much of a door opening its fanlight takes.</summary>
        public const float FanlightHeight = 0.38f;

        /// <summary>
        /// Plain window glass becomes one of the home window groups at random, so each window
        /// lights up at its own time in the evening. Other glass is kept as styled.
        /// </summary>
        private static SurfaceMaterial GlassFor(BuildContext context, WindowStyle style) =>
            style.Glass == SurfaceMaterial.WindowGlass ? HomeWindowGroup(context) : style.Glass;

        public static SurfaceMaterial HomeWindowGroup(BuildContext context) =>
            SurfaceMaterial.Window0 + context.Random.Next(8);

        private static void Frames(MeshBuilder builder, WallFrame wall, float x0, float y0, float x1, float y1, float glassZ, WindowStyle style)
        {
            var front = glassZ + BarDepth;
            var behind = style.SeeThrough ? glassZ - InsideDepth : glassZ;
            var frame = style.Frame;

            // A bar of the frame, standing proud of the glass outside (and inside, for see-through windows).
            void Bar(float bx0, float by0, float bx1, float by1)
            {
                wall.Block(builder, bx0, by0, bx1, by1, behind, front, frame);
                if (style.SeeThrough)
                {
                    wall.QuadInward(builder, bx0, by0, bx1, by1, behind, frame);
                }
            }

            void Across(float y, float width) => Bar(x0, y - (width * 0.5f), x1, y + (width * 0.5f));
            void Upright(float x) => Bar(x - (BarWidth * 0.5f), y0, x + (BarWidth * 0.5f), y1);

            // Outer frame.
            Bar(x0, y0, x1, y0 + FrameWidth);
            Bar(x0, y1 - FrameWidth, x1, y1);
            Bar(x0, y0 + FrameWidth, x0 + FrameWidth, y1 - FrameWidth);
            Bar(x1 - FrameWidth, y0 + FrameWidth, x1, y1 - FrameWidth);

            var midX = (x0 + x1) * 0.5f;
            var midY = (y0 + y1) * 0.5f;
            switch (style.Pattern)
            {
                case GlazingPattern.TwoOverTwo:
                    Across(midY, FrameWidth);
                    Upright(midX);
                    break;
                case GlazingPattern.SixOverSix:
                    Across(midY, FrameWidth);
                    Upright(x0 + ((x1 - x0) / 3f));
                    Upright(x0 + ((x1 - x0) * 2f / 3f));
                    Across(y0 + ((midY - y0) * 0.5f), BarWidth);
                    Across(midY + ((y1 - midY) * 0.5f), BarWidth);
                    break;
                case GlazingPattern.Casement:
                    Upright(midX);
                    Across(y0 + ((y1 - y0) * 0.72f), BarWidth);
                    break;
            }
        }

        private static void SillAndSurround(MeshBuilder builder, WallFrame wall, float x0, float y0, float x1, float y1, WindowStyle style)
        {
            wall.Block(builder, x0 - 0.08f, y0 - 0.09f, x1 + 0.08f, y0, -0.02f, 0.08f, style.Sill);
            if (style.Surround is SurfaceMaterial surround)
            {
                const float band = 0.13f;
                const float proud = 0.012f;
                wall.Quad(builder, x0 - band, y1, x1 + band, y1 + band, proud, surround);
                wall.Quad(builder, x0 - band, y0, x0, y1, proud, surround);
                wall.Quad(builder, x1, y0, x1 + band, y1, proud, surround);
            }
        }
    }
}
