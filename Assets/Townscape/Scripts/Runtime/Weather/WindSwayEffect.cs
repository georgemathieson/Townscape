using System.Collections.Generic;
using Townscape.Generation;
using Townscape.Runtime.Rendering;
using Townscape.Simulation.Weather;
using UnityEngine;
using Vector2N = System.Numerics.Vector2;

namespace Townscape.Runtime.Weather
{
    /// <summary>
    /// Bends trees, shrubs, grass and flowers in the wind by moving their vertices each frame.
    /// Only the plants near the camera move; further away the movement would be too small to see.
    /// </summary>
    /// <remarks>
    /// The work is split so it stays cheap: the wind's motion is worked out once for each patch
    /// of ground (plants in a patch move together, as a gust would move them), and each vertex
    /// just scales it by how far up its plant it is.
    /// </remarks>
    public sealed class WindSwayEffect : IWeatherEffect
    {
        private const float Range = 80f;
        private const float PatchSize = 2.5f;

        private readonly List<Chunk> _chunks = new List<Chunk>();

        public WindSwayEffect(IEnumerable<SpawnedMesh> spawned)
        {
            foreach (var mesh in spawned)
            {
                if (mesh.Generated.Category == MeshCategory.Vegetation && mesh.Generated.Mesh.SwayHeights != null)
                {
                    _chunks.Add(new Chunk(mesh));
                }
            }
        }

        public void Tick(in WeatherFrame frame)
        {
            var camera = frame.CameraPosition;
            foreach (var chunk in _chunks)
            {
                if (chunk.Bounds.SqrDistance(camera) < Range * Range)
                {
                    chunk.Sway(frame.Conditions.Wind, frame.Time);
                }
                else
                {
                    chunk.Rest();
                }
            }
        }

        public void Dispose()
        {
            foreach (var chunk in _chunks)
            {
                chunk.Rest();
            }
        }

        private sealed class Chunk
        {
            private readonly Mesh _mesh;
            private readonly Vector3[] _rest;
            private readonly Vector3[] _moved;
            private readonly int[] _vertex;
            private readonly int[] _patch;
            private readonly float[] _bend;
            private readonly float[] _height;
            private readonly Vector2N[] _patchCentre;
            private readonly float[] _patchHeight;
            private readonly Vector2N[] _motion;
            private bool _displaced;

            public Chunk(SpawnedMesh spawned)
            {
                _mesh = spawned.Mesh;
                var data = spawned.Generated.Mesh;
                _rest = _mesh.vertices;
                _moved = (Vector3[])_rest.Clone();

                // Leave room in the bounds for the sway, so plants are never culled mid-swing.
                var bounds = _mesh.bounds;
                bounds.Expand(2f);
                Bounds = bounds;

                var patches = new Dictionary<(int, int), int>();
                var centres = new List<Vector2N>();
                var heights = new List<float>();
                var vertices = new List<int>();
                var patchOfVertex = new List<int>();
                for (var i = 0; i < data.VertexCount; i++)
                {
                    var height = data.SwayHeights[i];
                    if (height < 0.02f)
                    {
                        continue;
                    }

                    var p = data.Positions[i];
                    var key = (Mathf.FloorToInt(p.X / PatchSize), Mathf.FloorToInt(p.Z / PatchSize));
                    if (!patches.TryGetValue(key, out var patch))
                    {
                        patch = centres.Count;
                        patches.Add(key, patch);
                        centres.Add(new Vector2N((key.Item1 + 0.5f) * PatchSize, (key.Item2 + 0.5f) * PatchSize));
                        heights.Add(0f);
                    }

                    heights[patch] = Mathf.Max(heights[patch], height);
                    vertices.Add(i);
                    patchOfVertex.Add(patch);
                }

                _vertex = vertices.ToArray();
                _patch = patchOfVertex.ToArray();
                _patchCentre = centres.ToArray();
                _patchHeight = heights.ToArray();
                _motion = new Vector2N[_patchCentre.Length];
                _bend = new float[_vertex.Length];
                _height = new float[_vertex.Length];
                for (var i = 0; i < _vertex.Length; i++)
                {
                    _height[i] = data.SwayHeights[_vertex[i]];
                    _bend[i] = WindSway.Bend(_height[i]);
                }
            }

            public Bounds Bounds { get; }

            public void Sway(Vector2N wind, float time)
            {
                for (var p = 0; p < _motion.Length; p++)
                {
                    _motion[p] = WindSway.Motion(_patchCentre[p], _patchHeight[p], wind, time);
                }

                for (var i = 0; i < _vertex.Length; i++)
                {
                    var motion = _motion[_patch[i]] * _bend[i];
                    var drop = WindSway.Drop((motion.X * motion.X) + (motion.Y * motion.Y), _height[i]);
                    var rest = _rest[_vertex[i]];
                    _moved[_vertex[i]] = new Vector3(rest.x + motion.X, rest.y - drop, rest.z + motion.Y);
                }

                _mesh.SetVertices(_moved);
                _mesh.bounds = Bounds;
                _displaced = true;
            }

            public void Rest()
            {
                if (!_displaced || _mesh == null)
                {
                    return;
                }

                _mesh.SetVertices(_rest);
                _mesh.bounds = Bounds;
                _displaced = false;
            }
        }
    }
}
