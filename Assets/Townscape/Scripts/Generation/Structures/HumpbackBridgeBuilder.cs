using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Structures
{
    /// <summary>
    /// A single-arch stone humpback bridge: a tarmac deck that rises over a shallow segmental arch,
    /// with parapets, dark coping stones, a protruding arch ring and pillars at each end.
    /// </summary>
    /// <remarks>
    /// Works in a local frame: <c>u</c> runs along the deck (0 at the middle), <c>v</c> across it
    /// (positive to the left) and <c>y</c> is world height. The deck meets the road at road level at
    /// both ends, and the arch springs from the river's embankment walls.
    /// </remarks>
    public sealed class HumpbackBridgeBuilder
    {
        private const int DeckSegments = 18;
        private const int ArchSegments = 12;
        private const float ApproachBottom = -0.4f;
        private const float CopingThickness = 0.12f;
        private const float CopingOverhang = 0.06f;
        private const float RingThickness = 0.45f;
        private const float RingProtrusion = 0.06f;
        private const float PillarHalfLength = 0.32f;
        private const float PillarExtraHeight = 0.3f;

        private readonly BridgeSpec _spec;
        private readonly Vector2 _along;
        private readonly Vector2 _across;
        private readonly Vector3 _along3;
        private readonly Vector3 _across3;
        private readonly float _archRadius;
        private readonly float _archCentreY;

        public HumpbackBridgeBuilder(BridgeSpec spec)
        {
            _spec = spec;
            _along = spec.Direction;
            _across = GeoMath.Left(_along);
            _along3 = new Vector3(_along.X, 0f, _along.Y);
            _across3 = new Vector3(_across.X, 0f, _across.Y);

            // Segmental arch through both springing points and the crown.
            var halfSpan = spec.ArchHalfSpan;
            var rise = spec.ArchRise;
            _archRadius = ((halfSpan * halfSpan) + (rise * rise)) / (2f * rise);
            _archCentreY = spec.SpringingHeight + rise - _archRadius;
        }

        /// <summary>Height of the road surface on the bridge at <paramref name="u"/>.</summary>
        public float DeckHeight(float u)
        {
            var c = MathF.Cos(MathF.PI * u / (2f * _spec.HalfLength));
            return _spec.HumpHeight * c * c;
        }

        /// <summary>Height of the underside of the arch at <paramref name="u"/> (only meaningful within the span).</summary>
        public float ArchHeight(float u)
        {
            var clamped = GeoMath.Clamp(u, -_spec.ArchHalfSpan, _spec.ArchHalfSpan);
            return _archCentreY + MathF.Sqrt((_archRadius * _archRadius) - (clamped * clamped));
        }

        public MeshData Build()
        {
            var builder = new MeshBuilder();
            var stations = Stations();
            var outer = _spec.FootprintHalfWidth;

            for (var i = 0; i < stations.Count - 1; i++)
            {
                var u0 = stations[i];
                var u1 = stations[i + 1];
                var d0 = DeckHeight(u0);
                var d1 = DeckHeight(u1);
                var inArch = u0 >= -_spec.ArchHalfSpan - 1e-4f && u1 <= _spec.ArchHalfSpan + 1e-4f;

                AddDeck(builder, u0, u1, d0, d1);
                foreach (var side in new[] { -1f, 1f })
                {
                    AddParapet(builder, side, u0, u1, d0, d1);
                    var bottom0 = inArch ? ArchHeight(u0) : ApproachBottom;
                    var bottom1 = inArch ? ArchHeight(u1) : ApproachBottom;
                    builder.AddQuadFacing(
                        Point(u0, side * outer, bottom0),
                        Point(u1, side * outer, bottom1),
                        Point(u1, side * outer, d1 + _spec.ParapetHeight),
                        Point(u0, side * outer, d0 + _spec.ParapetHeight),
                        _across3 * side,
                        SurfaceMaterial.Stone);
                }

                if (inArch)
                {
                    var a0 = ArchHeight(u0);
                    var a1 = ArchHeight(u1);
                    builder.AddQuadFacing(
                        Point(u0, -outer, a0),
                        Point(u0, outer, a0),
                        Point(u1, outer, a1),
                        Point(u1, -outer, a1),
                        -Vector3.UnitY,
                        SurfaceMaterial.Stone);
                }
            }

            AddAbutments(builder);
            AddArchRings(builder);
            AddPillars(builder);

            return builder.Build(_spec.Name);
        }

        private List<float> Stations()
        {
            var stations = new List<float>();
            for (var i = 0; i <= DeckSegments; i++)
            {
                stations.Add(-_spec.HalfLength + (2f * _spec.HalfLength * i / DeckSegments));
            }

            for (var i = 0; i <= ArchSegments; i++)
            {
                stations.Add(-_spec.ArchHalfSpan + (2f * _spec.ArchHalfSpan * i / ArchSegments));
            }

            stations.Sort();
            var unique = new List<float>();
            foreach (var station in stations)
            {
                if (unique.Count == 0 || station - unique[unique.Count - 1] > 0.05f)
                {
                    unique.Add(station);
                }
            }

            return unique;
        }

        private void AddDeck(MeshBuilder builder, float u0, float u1, float d0, float d1)
        {
            var w = _spec.RoadHalfWidth;
            builder.AddQuadFacing(Point(u0, -w, d0), Point(u0, w, d0), Point(u1, w, d1), Point(u1, -w, d1), Vector3.UnitY, SurfaceMaterial.Road);
        }

        private void AddParapet(MeshBuilder builder, float side, float u0, float u1, float d0, float d1)
        {
            var inner = side * _spec.RoadHalfWidth;
            var top0 = d0 + _spec.ParapetHeight;
            var top1 = d1 + _spec.ParapetHeight;

            // Face towards the road.
            builder.AddQuadFacing(Point(u0, inner, d0), Point(u1, inner, d1), Point(u1, inner, top1), Point(u0, inner, top0), -_across3 * side, SurfaceMaterial.Stone);

            // Coping stones along the top, overhanging slightly on both sides.
            var copingInner = side * (_spec.RoadHalfWidth - CopingOverhang);
            var copingOuter = side * (_spec.FootprintHalfWidth + CopingOverhang);
            var cap0 = top0 + CopingThickness;
            var cap1 = top1 + CopingThickness;
            builder.AddQuadFacing(Point(u0, copingInner, cap0), Point(u0, copingOuter, cap0), Point(u1, copingOuter, cap1), Point(u1, copingInner, cap1), Vector3.UnitY, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(Point(u0, copingInner, top0), Point(u1, copingInner, top1), Point(u1, copingInner, cap1), Point(u0, copingInner, cap0), -_across3 * side, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(Point(u0, copingOuter, top0), Point(u1, copingOuter, top1), Point(u1, copingOuter, cap1), Point(u0, copingOuter, cap0), _across3 * side, SurfaceMaterial.StoneDark);
        }

        private void AddAbutments(MeshBuilder builder)
        {
            var outer = _spec.FootprintHalfWidth;
            foreach (var end in new[] { -1f, 1f })
            {
                var u = end * _spec.ArchHalfSpan;
                builder.AddQuadFacing(
                    Point(u, -outer, _spec.FoundationHeight),
                    Point(u, outer, _spec.FoundationHeight),
                    Point(u, outer, _spec.SpringingHeight),
                    Point(u, -outer, _spec.SpringingHeight),
                    -_along3 * end,
                    SurfaceMaterial.Stone);
            }
        }

        private void AddArchRings(MeshBuilder builder)
        {
            var scale = (_archRadius + RingThickness) / _archRadius;
            var face = _spec.FootprintHalfWidth;
            for (var i = 0; i < ArchSegments; i++)
            {
                var u0 = -_spec.ArchHalfSpan + (2f * _spec.ArchHalfSpan * i / ArchSegments);
                var u1 = -_spec.ArchHalfSpan + (2f * _spec.ArchHalfSpan * (i + 1) / ArchSegments);
                var inner0 = new Vector2(u0, ArchHeight(u0));
                var inner1 = new Vector2(u1, ArchHeight(u1));
                var centre = new Vector2(0f, _archCentreY);
                var outer0 = centre + ((inner0 - centre) * scale);
                var outer1 = centre + ((inner1 - centre) * scale);
                var midNormal = Vector2.Normalize(((inner0 + inner1) * 0.5f) - centre);
                var outward3 = (_along3 * midNormal.X) + (Vector3.UnitY * midNormal.Y);

                foreach (var side in new[] { -1f, 1f })
                {
                    var front = side * (face + RingProtrusion);
                    var back = side * face;

                    builder.AddQuadFacing(
                        Point(inner0.X, front, inner0.Y),
                        Point(inner1.X, front, inner1.Y),
                        Point(outer1.X, front, outer1.Y),
                        Point(outer0.X, front, outer0.Y),
                        _across3 * side,
                        SurfaceMaterial.StoneDark);

                    // Underside and top of the protruding band.
                    builder.AddQuadFacing(
                        Point(inner0.X, back, inner0.Y),
                        Point(inner1.X, back, inner1.Y),
                        Point(inner1.X, front, inner1.Y),
                        Point(inner0.X, front, inner0.Y),
                        -outward3,
                        SurfaceMaterial.StoneDark);
                    builder.AddQuadFacing(
                        Point(outer0.X, back, outer0.Y),
                        Point(outer1.X, back, outer1.Y),
                        Point(outer1.X, front, outer1.Y),
                        Point(outer0.X, front, outer0.Y),
                        outward3,
                        SurfaceMaterial.StoneDark);
                }
            }
        }

        private void AddPillars(MeshBuilder builder)
        {
            var halfAcross = (_spec.ParapetThickness * 0.5f) + 0.08f;
            var centreAcross = _spec.RoadHalfWidth + (_spec.ParapetThickness * 0.5f);
            foreach (var end in new[] { -1f, 1f })
            {
                var u = end * (_spec.HalfLength - 0.3f);
                var top = DeckHeight(u) + _spec.ParapetHeight + PillarExtraHeight;
                var bottom = -0.2f;
                foreach (var side in new[] { -1f, 1f })
                {
                    var centre = Point(u, side * centreAcross, (top + bottom) * 0.5f);
                    builder.AddBox(centre, _along3, Vector3.UnitY, _across3, new Vector3(PillarHalfLength, (top - bottom) * 0.5f, halfAcross), SurfaceMaterial.Stone);
                    builder.AddPyramid(Point(u, side * centreAcross, top), _along3, _across3, new Vector2(PillarHalfLength + 0.04f, halfAcross + 0.04f), 0.28f, Vector3.UnitY, SurfaceMaterial.StoneDark);
                }
            }
        }

        private Vector3 Point(float u, float v, float y)
        {
            var flat = _spec.Centre + (_along * u) + (_across * v);
            return GeoMath.At(flat, y);
        }
    }
}
