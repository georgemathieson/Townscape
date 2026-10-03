using System;
using System.Collections.Generic;
using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;
using UnityEngine;
using UnityEngine.Rendering;
using Vector3N = System.Numerics.Vector3;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// What a strike looks like: a cold flash of light across the whole scene from the strike's
    /// direction, and for some strikes the bolt itself, flickering with the flash.
    /// </summary>
    /// <remarks>
    /// The flash on the sky, fog and ambient light is applied by <c>TimeOfDayLighting</c>; this
    /// adds the directional light and the bolts.
    /// </remarks>
    public sealed class LightningEffect : IWeatherEffect
    {
        private const int Pool = 3;
        private const float CloudBase = 450f;

        // Bolts beyond this are drawn nearer, scaled down so they look exactly the same size, to stay inside the camera's far plane.
        private const float MaxDrawDistance = 950f;

        // Width of the main channel as a share of its distance: a couple of pixels whatever the range.
        private const float WidthPerMetre = 0.0012f;

        private static readonly Color FlashColour = new Color(0.72f, 0.78f, 1f);
        private static readonly Vector4 BoltColour = new Vector4(7f, 7.5f, 10f, 1f);
        private static readonly int ColourId = Shader.PropertyToID("_Color");

        private readonly Light _light;
        private readonly Material _material;
        private readonly Func<Vector2, float> _groundHeight;
        private readonly Bolt[] _bolts = new Bolt[Pool];
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Vector3 _lastDirection = Vector3.down;
        private int _next;

        public LightningEffect(Transform parent, HideFlags hideFlags, Func<Vector2, float> groundHeight)
        {
            _groundHeight = groundHeight;
            _material = EffectMaterials.CreateBolt();

            _light = TownMeshSpawner.CreateChild("Lightning Light", parent, hideFlags).AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.shadows = LightShadows.None;
            _light.color = FlashColour;
            _light.intensity = 0f;
            _light.enabled = false;

            for (var i = 0; i < _bolts.Length; i++)
            {
                _bolts[i] = new Bolt(TownMeshSpawner.CreateChild($"Bolt {i}", parent, hideFlags).transform, _material);
            }
        }

        public string Name => "Lightning";

        public void Tick(in WeatherFrame frame)
        {
            foreach (var strike in frame.NewStrikes)
            {
                var toStrike = new Vector3(strike.Position.X, 0f, strike.Position.Y) - frame.CameraPosition;
                toStrike.y = 0f;
                _lastDirection = (-toStrike.normalized) + (Vector3.down * 0.9f);
                if (strike.HasBolt)
                {
                    Show(strike, frame.CameraPosition);
                }
            }

            _light.enabled = frame.Flash > 0.01f;
            _light.intensity = frame.Flash * 1.6f;
            if (_lastDirection.sqrMagnitude > 1e-4f)
            {
                _light.transform.rotation = Quaternion.LookRotation(_lastDirection.normalized);
            }

            foreach (var bolt in _bolts)
            {
                if (bolt.Strike == null)
                {
                    continue;
                }

                if (frame.Time > bolt.Strike.EndTime)
                {
                    bolt.Hide();
                    continue;
                }

                // The bolt is only visible while its strokes flash.
                var level = bolt.Strike.FlashAt(frame.Time) / Mathf.Max(0.01f, bolt.Strike.Brightness);
                _block.SetVector(ColourId, BoltColour * level);
                bolt.Apply(_block);
            }
        }

        public void Dispose()
        {
            foreach (var bolt in _bolts)
            {
                bolt?.Dispose();
            }

            ObjectUtility.Destroy(_light != null ? _light.gameObject : null);
            ObjectUtility.Destroy(_material);
        }

        private void Show(Strike strike, Vector3 camera)
        {
            var ground = new Vector3(strike.Position.X, _groundHeight(new Vector2(strike.Position.X, strike.Position.Y)), strike.Position.Y);
            var drift = new Vector2((float)Math.Sin(strike.Seed), (float)Math.Cos(strike.Seed * 1.7)) * 90f;
            var cloud = new Vector3(ground.x + drift.x, CloudBase, ground.z + drift.y);
            var segments = LightningBolt.Generate(strike.Seed, ToNumerics(cloud), ToNumerics(ground));

            var distance = Vector3.Distance(camera, ground);
            var scale = Mathf.Min(1f, MaxDrawDistance / Mathf.Max(1f, distance));
            var width = WidthPerMetre * distance * scale;

            var bolt = _bolts[_next];
            _next = (_next + 1) % _bolts.Length;
            bolt.Show(strike, segments, camera, scale, width);
        }

        private static Vector3N ToNumerics(Vector3 v) => new Vector3N(v.x, v.y, v.z);

        // One bolt: a line for the main channel, a faint wide one round it for a glow, and one per branch.
        private sealed class Bolt : IDisposable
        {
            private readonly Transform _root;
            private readonly Material _material;
            private readonly List<LineRenderer> _lines = new List<LineRenderer>();
            private int _used;

            public Bolt(Transform root, Material material)
            {
                _root = root;
                _material = material;
            }

            public Strike Strike { get; private set; }

            public void Show(Strike strike, IReadOnlyList<BoltSegment> segments, Vector3 camera, float scale, float width)
            {
                Strike = strike;
                _used = 0;
                var chain = new List<Vector3>();
                var chainWidth = 1f;
                var isMain = true;
                for (var i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    var start = Place(segment.Start, camera, scale);
                    if (chain.Count > 0 && (start - chain[chain.Count - 1]).sqrMagnitude > 1e-4f)
                    {
                        Draw(chain, width * chainWidth, isMain);
                        chain.Clear();
                    }

                    if (chain.Count == 0)
                    {
                        chain.Add(start);
                        chainWidth = segment.Width;
                        isMain = segment.IsMain;
                    }

                    chain.Add(Place(segment.End, camera, scale));
                }

                Draw(chain, width * chainWidth, isMain);
                for (var i = _used; i < _lines.Count; i++)
                {
                    _lines[i].enabled = false;
                }
            }

            public void Apply(MaterialPropertyBlock block)
            {
                for (var i = 0; i < _used; i++)
                {
                    _lines[i].SetPropertyBlock(block);
                }
            }

            public void Hide()
            {
                Strike = null;
                foreach (var line in _lines)
                {
                    line.enabled = false;
                }
            }

            public void Dispose()
            {
                if (_root != null)
                {
                    ObjectUtility.Destroy(_root.gameObject);
                }
            }

            private void Draw(List<Vector3> points, float width, bool isMain)
            {
                if (points.Count < 2)
                {
                    return;
                }

                Line(points, width, isMain ? 1f : 0.3f, 1f);
                if (isMain)
                {
                    // A wide, faint copy round the main channel reads as glow through the rain.
                    Line(points, width * 4f, 1f, 0.05f);
                }
            }

            private void Line(List<Vector3> points, float width, float tipWidth, float alpha)
            {
                if (_used == _lines.Count)
                {
                    var child = new GameObject("Channel") { hideFlags = _root.gameObject.hideFlags };
                    child.transform.SetParent(_root, false);
                    var created = child.AddComponent<LineRenderer>();
                    created.sharedMaterial = _material;
                    created.useWorldSpace = true;
                    created.alignment = LineAlignment.View;
                    created.textureMode = LineTextureMode.Stretch;
                    created.shadowCastingMode = ShadowCastingMode.Off;
                    created.receiveShadows = false;
                    created.numCornerVertices = 0;
                    created.numCapVertices = 0;
                    _lines.Add(created);
                }

                var line = _lines[_used++];
                line.enabled = true;
                line.positionCount = points.Count;
                line.SetPositions(points.ToArray());
                line.widthMultiplier = width;
                line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, tipWidth);
                line.startColor = new Color(1f, 1f, 1f, alpha);
                line.endColor = new Color(1f, 1f, 1f, alpha * (tipWidth < 1f ? 0.2f : 1f));
            }

            // Brings far bolts in along the line of sight, keeping their apparent size.
            private static Vector3 Place(Vector3N point, Vector3 camera, float scale) =>
                camera + ((new Vector3(point.X, point.Y, point.Z) - camera) * scale);
        }
    }
}
