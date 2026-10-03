namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>A rectangular hole in a wall, in wall coordinates.</summary>
    public readonly struct Opening
    {
        public Opening(float x0, float y0, float x1, float y1, float depth)
        {
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
            Depth = depth;
        }

        public float X0 { get; }

        public float Y0 { get; }

        public float X1 { get; }

        public float Y1 { get; }

        /// <summary>How far the reveal runs into the wall.</summary>
        public float Depth { get; }

        public float Width => X1 - X0;

        public float Height => Y1 - Y0;

        public float CentreX => (X0 + X1) * 0.5f;

        public bool Contains(float x, float y) => x > X0 && x < X1 && y > Y0 && y < Y1;
    }
}
