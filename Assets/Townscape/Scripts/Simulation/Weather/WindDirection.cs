using System;
using System.Numerics;

namespace Townscape.Simulation.Weather
{
    /// <summary>Compass points for people: winds are named after where they come from.</summary>
    public static class WindDirection
    {
        private static readonly string[] Points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        /// <summary>Where a wind blowing towards <paramref name="towards"/> (x east, y north) comes from, for example "SW".</summary>
        public static string From(Vector2 towards)
        {
            if (towards.LengthSquared() < 1e-6f)
            {
                return "calm";
            }

            var from = -towards;
            var bearing = MathF.Atan2(from.X, from.Y) * 180f / MathF.PI;
            var index = (int)MathF.Round((((bearing % 360f) + 360f) % 360f) / 45f) % Points.Length;
            return Points[index];
        }
    }
}
