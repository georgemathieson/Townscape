using System;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Interiors
{
    /// <summary>
    /// A straight flight of stairs running front to back (or back to front) in a strip of the
    /// building, from one floor to the next. Each step is built solid down to the floor below, so
    /// nothing can fall through and the space under the stairs is closed in.
    /// </summary>
    public sealed class StairFlight
    {
        /// <param name="startD">Depth of the bottom step's front edge.</param>
        /// <param name="endD">Depth where the flight reaches the floor above.</param>
        public StairFlight(float x0, float x1, float startD, float endD, float fromY, float toY, int steps)
        {
            X0 = x0;
            X1 = x1;
            StartD = startD;
            EndD = endD;
            FromY = fromY;
            ToY = toY;
            Steps = steps;
        }

        public float X0 { get; }

        public float X1 { get; }

        public float StartD { get; }

        public float EndD { get; }

        public float FromY { get; }

        public float ToY { get; }

        public int Steps { get; }

        /// <summary>The height of each step.</summary>
        public float Rise => (ToY - FromY) / Steps;

        /// <summary>The depth of each tread.</summary>
        public float Going => Math.Abs(EndD - StartD) / Steps;

        public float NearD => Math.Min(StartD, EndD);

        public float FarD => Math.Max(StartD, EndD);

        /// <summary>Where the stairs are under your feet at depth <paramref name="d"/>: the floor of the flight.</summary>
        public float HeightAt(float d)
        {
            var run = (d - StartD) * Math.Sign(EndD - StartD);
            if (run < 0f)
            {
                return FromY;
            }

            var step = Math.Min(Steps, (int)Math.Floor(run / Going) + 1);
            return FromY + (step * Rise);
        }

        public void Build(UnitSpace space, MeshBuilder builder, SurfaceMaterial tread, SurfaceMaterial side)
        {
            var direction = Math.Sign(EndD - StartD);
            for (var i = 0; i < Steps; i++)
            {
                var d0 = StartD + (direction * i * Going);
                var d1 = d0 + (direction * Going);
                var top = FromY + ((i + 1) * Rise);
                space.Box(builder, X0, FromY, Math.Min(d0, d1), X1, top - 0.04f, Math.Max(d0, d1), side);
                space.Box(builder, X0, top - 0.04f, Math.Min(d0, d1) - 0.02f, X1, top, Math.Max(d0, d1) + 0.02f, tread);
            }
        }
    }
}
