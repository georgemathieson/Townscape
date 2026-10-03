using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Townscape.Runtime.Lighting
{
    /// <summary>
    /// Keeps the environment reflection in step with the time of day so water and (later) wet
    /// surfaces reflect a storm sky rather than black. The skybox itself is never drawn: the camera
    /// clears to the fog colour, so it only feeds reflections.
    /// </summary>
    public sealed class SkyReflection : IDisposable
    {
        private const int Height = 64;
        private const float MinSecondsBetweenUpdates = 0.25f;
        private const float MinColourChange = 0.01f;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");

        private readonly Texture2D _gradient;
        private readonly Material _material;
        private readonly Color[] _pixels = new Color[Height];
        private Color _lastSky;
        private Color _lastHorizon;
        private float _lastUpdate = float.NegativeInfinity;

        public SkyReflection()
        {
            _gradient = new Texture2D(1, Height, TextureFormat.RGBA32, false)
            {
                name = "Townscape Sky Gradient",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var shader = Shader.Find("Skybox/Panoramic");
            if (shader != null)
            {
                _material = new Material(shader) { name = "Townscape Sky", hideFlags = HideFlags.DontSave };
                _material.SetTexture(MainTexId, _gradient);
                _material.SetColor(TintId, new Color(0.5f, 0.5f, 0.5f, 1f));
                _material.SetFloat(ExposureId, 1f);
            }
        }

        public void Update(in LightingKeyframe key, float time)
        {
            if (_material == null)
            {
                return;
            }

            var changed = Difference(key.SkyColour, _lastSky) > MinColourChange || Difference(key.FogColour, _lastHorizon) > MinColourChange;
            if (!changed || time - _lastUpdate < MinSecondsBetweenUpdates)
            {
                return;
            }

            _lastSky = key.SkyColour;
            _lastHorizon = key.FogColour;
            _lastUpdate = time;

            // Row 0 is straight down, the middle row the horizon, the top row straight up.
            for (var y = 0; y < Height; y++)
            {
                var v = (y + 0.5f) / Height;
                _pixels[y] = v >= 0.5f
                    ? Color.Lerp(key.FogColour, key.SkyColour, Mathf.Pow((v - 0.5f) * 2f, 0.6f))
                    : Color.Lerp(key.FogColour, key.AmbientGround, Mathf.Pow((0.5f - v) * 2f, 0.5f));
            }

            _gradient.SetPixels(_pixels);
            _gradient.Apply(false);

            RenderSettings.skybox = _material;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;
            DynamicGI.UpdateEnvironment();
        }

        public void Dispose()
        {
            if (RenderSettings.skybox == _material)
            {
                RenderSettings.skybox = null;
            }

            DestroySafely(_material);
            DestroySafely(_gradient);
        }

        private static float Difference(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        private static void DestroySafely(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
