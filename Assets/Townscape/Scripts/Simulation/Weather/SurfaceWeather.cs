using System.Collections.Generic;
using System.Linq;
using Townscape.Generation.Geometry;

namespace Townscape.Simulation.Weather
{
    /// <summary>Rain and snow on one surface together: wet first, with any settled snow on top.</summary>
    public static class SurfaceWeather
    {
        /// <summary>Materials that rain or snow change.</summary>
        public static IEnumerable<SurfaceMaterial> Affected => Wetness.Affected.Union(SnowCover.Affected);

        public static SurfaceAppearance Apply(SurfaceMaterial material, SurfaceAppearance dry, float wetness, float snow) =>
            SnowCover.Apply(material, Wetness.Apply(material, dry, wetness), snow);
    }
}
