using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;

namespace Townscape.Generation.Markings
{
    /// <summary>
    /// A zebra crossing: white stripes, a broken give-way line each side, and zig-zags along both
    /// kerbs. The flashing orange Belisha beacons belong to street furniture and use <see cref="Along"/>.
    /// </summary>
    public sealed class ZebraCrossingMarking : IRoadMarking
    {
        public const float CrossingWidth = 3f;
        public const float ZigZagReach = GiveWayDistance + 0.6f + (ZigZagCount * ZigZagLength);

        private const float StripeWidth = 0.5f;
        private const float GiveWayDistance = (CrossingWidth * 0.5f) + 1.1f;
        private const int ZigZagCount = 8;
        private const float ZigZagLength = 1.4f;
        private const float ZigZagAmplitude = 0.25f;
        private const float ZigZagLineWidth = 0.1f;

        public ZebraCrossingMarking(float along)
        {
            Along = along;
        }

        /// <summary>Arc length along the road of the middle of the crossing.</summary>
        public float Along { get; }

        /// <summary>The stretch of road the crossing's markings occupy, for other markings to avoid.</summary>
        public DistanceRange Footprint => new DistanceRange(Along - ZigZagReach - 0.5f, Along + ZigZagReach + 0.5f);

        public void Paint(RoadSpec road, MarkingCanvas canvas)
        {
            PaintStripes(road, canvas);

            foreach (var direction in new[] { -1f, 1f })
            {
                GiveWayMarking.PaintBrokenLineAcross(road, canvas, Along + (direction * GiveWayDistance), 0.6f, 0.3f, 0.2f);
                PaintZigZags(road, canvas, direction);
            }
        }

        private void PaintStripes(RoadSpec road, MarkingCanvas canvas)
        {
            var edge = road.HalfWidth - 0.2f;
            for (var offset = -edge; offset + StripeWidth <= edge + 1e-3f; offset += StripeWidth * 2f)
            {
                canvas.PaintAlong(
                    road,
                    Along - (CrossingWidth * 0.5f),
                    Along + (CrossingWidth * 0.5f),
                    offset + (StripeWidth * 0.5f),
                    StripeWidth,
                    SurfaceMaterial.MarkingWhite);
            }
        }

        private void PaintZigZags(RoadSpec road, MarkingCanvas canvas, float direction)
        {
            var start = GiveWayDistance + 0.6f;
            foreach (var side in new[] { -1f, 1f })
            {
                var baseline = side * (road.HalfWidth - 0.45f);
                for (var i = 0; i < ZigZagCount; i++)
                {
                    var s0 = Along + (direction * (start + (i * ZigZagLength)));
                    var s1 = Along + (direction * (start + ((i + 1) * ZigZagLength)));
                    var o0 = baseline + ((i % 2 == 0 ? -1f : 1f) * ZigZagAmplitude);
                    var o1 = baseline + ((i % 2 == 0 ? 1f : -1f) * ZigZagAmplitude);
                    canvas.PaintSegment(
                        MarkingCanvas.PointOn(road, s0, o0),
                        MarkingCanvas.PointOn(road, s1, o1),
                        ZigZagLineWidth,
                        SurfaceMaterial.MarkingWhite);
                }
            }
        }
    }
}
