using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;
using Townscape.Generation.Markings;

namespace Townscape.Generation.Buildings.Planning
{
    /// <summary>Turns the layout's terraces and detached buildings into concrete footprints and designs.</summary>
    public static class BuildingPlanner
    {
        public static IReadOnlyList<BuildingPlan> Plan(TownLayout layout)
        {
            var plans = new List<BuildingPlan>();
            foreach (var terrace in layout.Terraces)
            {
                plans.AddRange(PlanTerrace(terrace));
            }

            foreach (var detached in layout.Detached)
            {
                plans.Add(new BuildingPlan(detached.Name, detached.Footprint, detached.Style, detached.Seed));
            }

            return plans;
        }

        /// <summary>
        /// Splits the frontage into units and gives each a wedge-shaped footprint whose side walls
        /// follow the road's normals, so neighbours share walls exactly even where the road curves.
        /// </summary>
        public static IReadOnlyList<BuildingPlan> PlanTerrace(TerraceSpec terrace)
        {
            var random = new Random(terrace.Seed);
            var road = terrace.Road;
            var onLeft = terrace.Side != KerbSide.Right;
            var sign = onLeft ? 1f : -1f;
            var offset = sign * (road.HalfWidth + road.PavementWidth + terrace.Setback);
            var units = terrace.Units;

            // Unit widths: weights with a little jitter, scaled to fill the frontage.
            var weights = new float[units.Count];
            var total = 0f;
            for (var i = 0; i < units.Count; i++)
            {
                weights[i] = units[i].WidthWeight * (0.9f + (0.2f * (float)random.NextDouble()));
                total += weights[i];
            }

            var stations = new float[units.Count + 1];
            stations[0] = terrace.FromAlong;
            for (var i = 0; i < units.Count; i++)
            {
                stations[i + 1] = stations[i] + ((terrace.ToAlong - terrace.FromAlong) * weights[i] / total);
            }

            var fronts = new Vector2[stations.Length];
            var inwards = new Vector2[stations.Length];
            for (var k = 0; k < stations.Length; k++)
            {
                fronts[k] = road.PointAt(stations[k], offset);
                inwards[k] = GeoMath.Left(road.SmoothTangentAt(stations[k])) * sign;
            }

            var footprints = new Footprint[units.Count];
            var designs = new UnitDesign[units.Count];
            for (var i = 0; i < units.Count; i++)
            {
                var depth = terrace.Depth * (0.94f + (0.12f * (float)random.NextDouble()));
                var a = fronts[i];
                var b = fronts[i + 1];
                var aBack = a + (inwards[i] * depth);
                var bBack = b + (inwards[i + 1] * depth);

                // Seen from the street, the left corner is the lower station on the left side of the road.
                footprints[i] = onLeft ? new Footprint(a, b, bBack, aBack) : new Footprint(b, a, aBack, bBack);
                designs[i] = TerraceDesigner.Design(units[i], footprints[i], random);
            }

            // Chimneys: each unit owns the stack on its left party wall; the unit at the far right
            // end also gets one on its right. Stacks rise above whichever neighbour is taller.
            for (var i = 0; i < units.Count; i++)
            {
                var leftNeighbour = onLeft ? i - 1 : i + 1;
                designs[i].ChimneyLeft = true;
                designs[i].LeftNeighbourRidge = leftNeighbour >= 0 && leftNeighbour < units.Count ? designs[leftNeighbour].Ridge : 0f;
                designs[i].ChimneyRight = onLeft ? i == units.Count - 1 : i == 0;
            }

            var plans = new List<BuildingPlan>();
            for (var i = 0; i < units.Count; i++)
            {
                plans.Add(new BuildingPlan(terrace.Name, footprints[i], new TerracedUnitStyle(designs[i]), terrace.Seed + (i * 7919)));
            }

            return plans;
        }
    }
}
