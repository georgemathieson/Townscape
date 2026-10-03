using System;
using System.Text;
using Townscape.Runtime.Lighting;
using Townscape.Runtime.Weather;
using Townscape.Simulation.Diagnostics;
using Unity.Profiling;
using UnityEngine;

namespace Townscape.Runtime.UI
{
    public enum PerformanceView
    {
        Hidden,
        FrameRate,
        Detailed,
    }

    /// <summary>
    /// A frame rate readout in the top-right corner, kept apart from the control panel so it stays
    /// on screen when the panel is hidden (handy for screenshots). F cycles it: off, the frame rate,
    /// and a breakdown of where the time goes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PerformanceOverlay : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;
        private const float Width = 330f;

        private readonly FrameTimes _frames = new FrameTimes();
        private readonly SmoothedTiming _gpu = new SmoothedTiming();
        private readonly FrameTiming[] _frameTiming = new FrameTiming[1];
        private StormSystem _storm;
        private TownLights _lights;
        private Func<float> _buildMilliseconds;
        private PanelSkin _skin;
        private GUIStyle _text;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPassCalls;
        private ProfilerRecorder _triangles;
        private string _shown = string.Empty;
        private float _refreshedAt = float.NegativeInfinity;

        public PerformanceView View { get; set; } = PerformanceView.FrameRate;

        public void Initialize(StormSystem storm, TownLights lights, Func<float> buildMilliseconds)
        {
            _storm = storm;
            _lights = lights;
            _buildMilliseconds = buildMilliseconds;
        }

        /// <summary>Off, then the frame rate, then the breakdown, then off again.</summary>
        public void Cycle()
        {
            View = (PerformanceView)(((int)View + 1) % 3);
            _refreshedAt = float.NegativeInfinity;
        }

        private void OnEnable()
        {
            // Render counters; not every platform or build reports them, so each is only shown if valid.
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        private void OnDisable()
        {
            _batches.Dispose();
            _setPassCalls.Dispose();
            _triangles.Dispose();
        }

        private void OnDestroy()
        {
            _skin?.Dispose();
        }

        private void Update()
        {
            _frames.Add(Time.unscaledDeltaTime);

            // GPU time is only available where the platform reports frame timings.
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _frameTiming) > 0 && _frameTiming[0].gpuFrameTime > 0.0)
            {
                _gpu.Add((float)_frameTiming[0].gpuFrameTime);
            }

            if (View != PerformanceView.Hidden && Time.unscaledTime - _refreshedAt >= RefreshSeconds)
            {
                _shown = Describe();
                _refreshedAt = Time.unscaledTime;
            }
        }

        private void OnGUI()
        {
            if (View == PerformanceView.Hidden || string.IsNullOrEmpty(_shown))
            {
                return;
            }

            _skin ??= new PanelSkin();
            _text ??= new GUIStyle(_skin.Keys) { fontSize = 13, wordWrap = true, margin = new RectOffset(0, 0, 0, 0) };

            // The same scaling as the control panel.
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = View == PerformanceView.Detailed ? Width : 230f;
            GUILayout.BeginArea(new Rect((Screen.width / scale) - width - 16f, 16f, width, (Screen.height / scale) - 32f));
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.Label(_shown, _text);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private string Describe()
        {
            var fps = _frames.FramesPerSecond;
            var colour = fps >= 55f ? "#9fd89a" : fps >= 30f ? "#f0c060" : "#f07a6a";
            var text = new StringBuilder();
            text.Append($"<color={colour}><b>{fps:0} fps</b></color>  ·  {_frames.AverageMilliseconds:0.0} ms  ·  worst {_frames.WorstMilliseconds:0.0} ms");
            if (View != PerformanceView.Detailed)
            {
                return text.ToString();
            }

            if (_gpu.Milliseconds > 0f)
            {
                text.Append($"\nGPU {_gpu.Milliseconds:0.0} ms");
            }

            var built = _buildMilliseconds?.Invoke() ?? 0f;
            text.Append($"\nTown built in {built / 1000f:0.00} s  ·  {Screen.width}×{Screen.height}");

            if (_storm != null)
            {
                var total = 0f;
                var parts = new StringBuilder();
                foreach (var (name, milliseconds) in _storm.Timings)
                {
                    total += milliseconds;
                    parts.Append($"\n   {name} {milliseconds:0.00} ms");
                }

                text.Append($"\n<b>Storm</b> {total:0.00} ms{parts}");
            }

            if (_lights != null)
            {
                text.Append($"\n<b>Lights</b> {_lights.UpdateMilliseconds:0.00} ms  ·  {_lights.LightCount} lights");
            }

            var counters = new StringBuilder();
            Counter(counters, _batches, "batches", 1f, "0");
            Counter(counters, _setPassCalls, "set-pass calls", 1f, "0");
            Counter(counters, _triangles, "M triangles", 1e-6f, "0.0");
            if (counters.Length > 0)
            {
                text.Append($"\n{counters}");
            }

            text.Append("\n<color=#8a8f98>F  cycles this readout</color>");
            return text.ToString();
        }

        private static void Counter(StringBuilder text, ProfilerRecorder recorder, string label, float scale, string format)
        {
            if (!recorder.Valid || recorder.LastValue <= 0)
            {
                return;
            }

            text.Append(text.Length > 0 ? "  ·  " : string.Empty).Append((recorder.LastValue * scale).ToString(format)).Append(' ').Append(label);
        }
    }
}
