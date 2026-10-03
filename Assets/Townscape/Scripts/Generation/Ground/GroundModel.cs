using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Terrain;

namespace Townscape.Generation.Ground
{
    /// <summary>
    /// Answers "what is the ground like here?" for any point: which region wins, how high it is,
    /// and where the nearest region edge lies. Shared by the mesher, the markings and the camera.
    /// </summary>
    public sealed class GroundModel
    {
        private readonly IReadOnlyList<IGroundFeature> _features;
        private readonly ISurfaceRegion _openGround;

        public GroundModel(BaseTerrain terrain, IReadOnlyList<IGroundFeature> features)
        {
            Terrain = terrain;
            _features = features;
            _openGround = new OpenGroundRegion(terrain);
        }

        public BaseTerrain Terrain { get; }

        public ISurfaceRegion Classify(Vector2 p)
        {
            ISurfaceRegion best = _openGround;
            var bestPriority = int.MinValue;
            foreach (var feature in _features)
            {
                if (feature.TryClaim(p, out var claim) && claim.Priority > bestPriority)
                {
                    best = claim.Region;
                    bestPriority = claim.Priority;
                }
            }

            return best;
        }

        /// <summary>Distance to the nearest feature edge; zero inside a feature.</summary>
        public float DistanceToFeatures(Vector2 p)
        {
            var best = float.MaxValue;
            foreach (var feature in _features)
            {
                if (!feature.Bounds.Expand(10f).Contains(p))
                {
                    continue;
                }

                best = MathF.Min(best, MathF.Max(0f, feature.DistanceToEdge(p)));
            }

            return best;
        }

        public GroundPoint PointAt(Vector2 p) => new GroundPoint(p, Terrain.HeightAt(p, DistanceToFeatures(p)));

        /// <summary>Surface height of whatever region covers <paramref name="p"/>.</summary>
        public float HeightAt(Vector2 p) => Classify(p).HeightAt(PointAt(p));

        public bool TryNearestContour(Vector2 p, float maxDistance, out ContourHit hit)
        {
            hit = default;
            var found = false;
            for (var i = 0; i < _features.Count; i++)
            {
                if (!_features[i].TryNearestContour(p, maxDistance, out var candidate))
                {
                    continue;
                }

                if (!found || candidate.Distance < hit.Distance)
                {
                    // Make contour ids unique across features.
                    hit = new ContourHit(candidate.Point, candidate.Distance, (i * 16) + candidate.ContourId);
                    found = true;
                }
            }

            return found;
        }
    }
}
