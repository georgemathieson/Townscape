using System.Numerics;

namespace Townscape.Generation.Maths
{
    /// <summary>Axis-aligned rectangle on the ground plane, used to skip far-away features quickly.</summary>
    public readonly struct Rect2
    {
        public Rect2(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public Vector2 Min { get; }

        public Vector2 Max { get; }

        public bool Contains(Vector2 p) => p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y;

        public Rect2 Expand(float amount) => new Rect2(Min - new Vector2(amount), Max + new Vector2(amount));
    }
}
