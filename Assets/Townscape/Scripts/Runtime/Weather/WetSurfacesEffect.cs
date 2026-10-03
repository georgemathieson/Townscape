using System.Linq;
using Townscape.Generation.Geometry;
using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;

namespace Townscape.Runtime.Weather
{
    /// <summary>Darkens and glosses the shared materials as they get wet, so roads shine under the lamps.</summary>
    public sealed class WetSurfacesEffect : IWeatherEffect
    {
        private const float MinChange = 0.003f;

        private readonly MaterialLibrary _materials;
        private readonly SurfaceMaterial[] _surfaces = Wetness.Affected.ToArray();
        private float _applied = -1f;

        public WetSurfacesEffect(MaterialLibrary materials)
        {
            _materials = materials;
        }

        public string Name => "Wet surfaces";

        public void Tick(in WeatherFrame frame)
        {
            if (System.Math.Abs(frame.Wetness - _applied) >= MinChange)
            {
                Apply(frame.Wetness);
            }
        }

        public void Dispose()
        {
            Apply(0f);
        }

        private void Apply(float wetness)
        {
            _applied = wetness;
            foreach (var surface in _surfaces)
            {
                _materials.SetAppearance(surface, Wetness.Apply(surface, SurfacePalette.Get(surface), wetness));
            }
        }
    }
}
