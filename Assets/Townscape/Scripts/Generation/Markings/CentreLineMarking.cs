using System.Collections.Generic;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;

namespace Townscape.Generation.Markings
{
    /// <summary>Broken white centre line, with gaps where other markings take over (e.g. a zebra crossing).</summary>
    public sealed class CentreLineMarking : IRoadMarking
    {
        private readonly float _dash;
        private readonly float _gap;
        private readonly float _width;
        private readonly IReadOnlyList<DistanceRange> _exclusions;

        public CentreLineMarking(float dash = 4f, float gap = 5f, float width = 0.1f, IReadOnlyList<DistanceRange> exclusions = null)
        {
            _dash = dash;
            _gap = gap;
            _width = width;
            _exclusions = exclusions ?? new DistanceRange[0];
        }

        public void Paint(RoadSpec road, MarkingCanvas canvas)
        {
            for (var start = _gap * 0.5f; start < road.Centre.Length; start += _dash + _gap)
            {
                var end = start + _dash;
                if (IsExcluded(start, end))
                {
                    continue;
                }

                canvas.PaintAlong(road, start, end, 0f, _width, SurfaceMaterial.MarkingWhite);
            }
        }

        private bool IsExcluded(float start, float end)
        {
            foreach (var range in _exclusions)
            {
                if (range.Overlaps(start, end))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
