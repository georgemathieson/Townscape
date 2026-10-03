using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>
    /// The space behind a shop window. Display coordinates: <c>x</c> from the left of the window,
    /// <c>y</c> up from the window bed, <c>d</c> back from the glass. Displays only ever place
    /// boxes and panels in here, so they cannot poke through the facade.
    /// </summary>
    public sealed class DisplayBox
    {
        private readonly float _left;
        private readonly float _bed;
        private readonly float _glass;

        public DisplayBox(BuildContext context, WallFrame wall, float left, float right, float bed, float top, float glassDepth, float backDepth)
        {
            Context = context;
            Wall = wall;
            _left = left;
            _bed = bed;
            _glass = glassDepth;
            Width = right - left;
            Height = top - bed;
            Depth = backDepth - glassDepth;
        }

        public BuildContext Context { get; }

        public WallFrame Wall { get; }

        public float Width { get; }

        public float Height { get; }

        public float Depth { get; }

        /// <summary>A solid box. Its back is open, so keep boxes clear of the back wall or touching it.</summary>
        public void Box(float x0, float x1, float y0, float y1, float d0, float d1, SurfaceMaterial material)
        {
            Wall.Block(Context.Builder, _left + x0, _bed + y0, _left + x1, _bed + y1, Z(d1), Z(d0), material);
        }

        /// <summary>A flat panel facing the street at depth <paramref name="d"/>.</summary>
        public void Panel(float x0, float y0, float x1, float y1, float d, SurfaceMaterial material)
        {
            Wall.Quad(Context.Builder, _left + x0, _bed + y0, _left + x1, _bed + y1, Z(d), material);
        }

        /// <summary>A small round thing (a cake, a pot, a bucket) standing at (x, y, d).</summary>
        public void Round(float x, float y, float d, float radius, float height, SurfaceMaterial material)
        {
            Context.Builder.AddPrism(Point(x, y, d), radius, height, 7, material);
        }

        /// <summary>Lettering on a panel at depth <paramref name="d"/>.</summary>
        public void Lettering(string text, float centreX, float centreY, float maxWidth, float pixel, float d, SurfaceMaterial material)
        {
            PixelFont.Write(Context.Builder, Wall, text, _left + centreX, _bed + centreY, pixel, maxWidth, Z(d) + 0.004f, material);
        }

        public Vector3 Point(float x, float y, float d) => Wall.Point(_left + x, _bed + y, Z(d));

        public float Range(float min, float max) => Context.Range(min, max);

        private float Z(float d) => -(_glass + d);
    }
}
