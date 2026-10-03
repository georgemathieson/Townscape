using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;

namespace Townscape.Generation.Markings
{
    /// <summary>Which kerb of a road, looking along its centreline from the start.</summary>
    public enum KerbSide
    {
        Left,
        Right,
        Both,
    }

    /// <summary>"No waiting at any time": two yellow lines along a kerb.</summary>
    public sealed class DoubleYellowLines : IRoadMarking
    {
        private const float KerbInset = 0.22f;
        private const float LineWidth = 0.09f;
        private const float LineGap = 0.08f;

        private readonly DistanceRange _range;
        private readonly KerbSide _side;

        public DoubleYellowLines(DistanceRange range, KerbSide side = KerbSide.Both)
        {
            _range = range;
            _side = side;
        }

        public void Paint(RoadSpec road, MarkingCanvas canvas)
        {
            var outer = road.HalfWidth - KerbInset - (LineWidth * 0.5f);
            var inner = outer - LineWidth - LineGap;
            foreach (var side in new[] { -1f, 1f })
            {
                if ((side > 0f && _side == KerbSide.Right) || (side < 0f && _side == KerbSide.Left))
                {
                    continue;
                }

                canvas.PaintAlong(road, _range.From, _range.To, side * outer, LineWidth, SurfaceMaterial.MarkingYellow);
                canvas.PaintAlong(road, _range.From, _range.To, side * inner, LineWidth, SurfaceMaterial.MarkingYellow);
            }
        }
    }
}
