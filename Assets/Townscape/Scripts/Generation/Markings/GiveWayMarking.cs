using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;

namespace Townscape.Generation.Markings
{
    /// <summary>Double broken white line across a side road where it meets a bigger road.</summary>
    public sealed class GiveWayMarking : IRoadMarking
    {
        private const float Dash = 0.6f;
        private const float Gap = 0.3f;
        private const float LineWidth = 0.15f;
        private const float LineSpacing = 0.45f;

        private readonly float _along;

        /// <param name="along">Arc length along the side road where the first line goes.</param>
        public GiveWayMarking(float along)
        {
            _along = along;
        }

        public void Paint(RoadSpec road, MarkingCanvas canvas)
        {
            PaintBrokenLineAcross(road, canvas, _along, Dash, Gap, LineWidth);
            PaintBrokenLineAcross(road, canvas, _along + LineSpacing, Dash, Gap, LineWidth);
        }

        internal static void PaintBrokenLineAcross(RoadSpec road, MarkingCanvas canvas, float along, float dash, float gap, float width)
        {
            var edge = road.HalfWidth - 0.15f;
            for (var offset = -edge; offset < edge; offset += dash + gap)
            {
                var end = offset + dash > edge ? edge : offset + dash;
                canvas.PaintSegment(
                    MarkingCanvas.PointOn(road, along, offset),
                    MarkingCanvas.PointOn(road, along, end),
                    width,
                    SurfaceMaterial.MarkingWhite);
            }
        }
    }
}
