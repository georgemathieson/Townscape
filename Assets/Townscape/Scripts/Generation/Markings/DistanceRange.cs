namespace Townscape.Generation.Markings
{
    /// <summary>A stretch of road measured as arc length from the start of its centreline.</summary>
    public readonly struct DistanceRange
    {
        public DistanceRange(float from, float to)
        {
            From = from;
            To = to;
        }

        public float From { get; }

        public float To { get; }

        public bool Overlaps(float from, float to) => from < To && to > From;
    }
}
