using Townscape.Generation.Geometry;
using Townscape.Runtime.Rendering;
using UnityEngine;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// Rain rings on the puddles, the river and the lake, played as normal-map flipbooks, with
    /// the river's texture sliding downstream so it flows. Puddles fill and drain with wetness.
    /// </summary>
    public sealed class WaterEffect : IWeatherEffect
    {
        private const float FramesPerSecond = 26f;
        private const float FlowSpeed = 0.9f;
        private const float PuddleTile = 1.3f;
        private const float WaterTile = 3.5f;

        private readonly MaterialLibrary _materials;
        private readonly WeatherTextures _textures;
        private readonly SurfaceAppearance _puddle = SurfacePalette.Get(SurfaceMaterial.Puddle);
        private float _flow;

        public WaterEffect(MaterialLibrary materials, WeatherTextures textures)
        {
            _materials = materials;
            _textures = textures;
        }

        public void Tick(in WeatherFrame frame)
        {
            var rain = frame.Conditions.Rain;
            var frameIndex = Mathf.FloorToInt(frame.Time * FramesPerSecond);
            var puddles = _textures.PuddleRipples[Wrap(frameIndex, _textures.PuddleRipples.Length)];
            var water = _textures.WaterRipples[Wrap(frameIndex, _textures.WaterRipples.Length)];

            _materials.SetNormalMap(SurfaceMaterial.Puddle, puddles, 0.15f + (0.85f * rain), PuddleTile);
            _materials.SetNormalMap(SurfaceMaterial.Water, water, 0.25f + (0.75f * rain), WaterTile);
            _materials.SetNormalMap(SurfaceMaterial.RiverWater, water, 0.3f + (0.7f * rain), WaterTile);

            // Texture coordinates run downstream in metres, so this carries the ripples along at the river's speed.
            _flow = Mathf.Repeat(_flow + (frame.DeltaTime * FlowSpeed / WaterTile), 1f);
            _materials.SetTextureOffset(SurfaceMaterial.RiverWater, new Vector2(0f, -_flow));

            // Puddles fade in as the ground gets wet.
            var depth = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.8f, frame.Wetness));
            _materials.SetAppearance(SurfaceMaterial.Puddle, new SurfaceAppearance(_puddle.R, _puddle.G, _puddle.B, _puddle.Smoothness, _puddle.Alpha * depth));
        }

        public void Dispose()
        {
        }

        private static int Wrap(int index, int count) => ((index % count) + count) % count;
    }
}
