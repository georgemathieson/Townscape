using System.Linq;
using Townscape.Generation.Geometry;
using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// Changes the shared materials as the weather settles on them: darker and glossier as they get
    /// wet, so roads shine under the lamps, and white as snow lies on roofs, grass and pavements.
    /// </summary>
    public sealed class WeatheredSurfacesEffect : IWeatherEffect
    {
        private const float MinChange = 0.003f;

        private readonly MaterialLibrary _materials;
        private readonly SurfaceMaterial[] _surfaces = SurfaceWeather.Affected.ToArray();
        private float _wetness = -1f;
        private float _snow = -1f;

        public WeatheredSurfacesEffect(MaterialLibrary materials)
        {
            _materials = materials;
        }

        public string Name => "Wet and snowy surfaces";

        public void Tick(in WeatherFrame frame)
        {
            if (System.Math.Abs(frame.Wetness - _wetness) >= MinChange || System.Math.Abs(frame.SnowCover - _snow) >= MinChange)
            {
                Apply(frame.Wetness, frame.SnowCover);
            }
        }

        public void Dispose()
        {
            Apply(0f, 0f);
        }

        private void Apply(float wetness, float snow)
        {
            _wetness = wetness;
            _snow = snow;
            foreach (var surface in _surfaces)
            {
                _materials.SetAppearance(surface, SurfaceWeather.Apply(surface, SurfacePalette.Get(surface), wetness, snow));
            }
        }
    }
}
