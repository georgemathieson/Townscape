using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Maths;
using Townscape.Generation.Markings;

namespace Townscape.Generation.Layout
{
    /// <summary>
    /// The shipped town: a small Lake District village where a high street crosses the river on a
    /// stone humpback bridge. Lanes lead up to the church, out to the fells and down to the old
    /// mill, with riverside footpaths along both banks and a lake out in the valley to the south.
    /// </summary>
    /// <remarks>
    /// Coordinates are metres; x is east, z is north, and the bridge sits at the origin.
    /// </remarks>
    public sealed class LakeDistrictVillageLayout : ITownLayoutSource
    {
        public const float WaterLevel = -0.9f;
        public const float GrassLevel = 0.1f;

        public TownLayout Create()
        {
            // Straight and due south for 32 m either side of the bridge, so the arch sits square.
            var river = new RiverSpec(
                Polyline.Smooth(new[]
                {
                    new Vector2(-70f, 420f),
                    new Vector2(-48f, 260f),
                    new Vector2(-20f, 140f),
                    new Vector2(-6f, 70f),
                    new Vector2(0f, 32f),
                    new Vector2(0f, 16f),
                    new Vector2(0f, -16f),
                    new Vector2(0f, -32f),
                    new Vector2(8f, -80f),
                    new Vector2(30f, -140f),
                    new Vector2(70f, -230f),
                    new Vector2(110f, -320f),
                }),
                waterHalfWidth: 5f,
                bankWidth: 4f,
                wallHalfWidth: 5.6f,
                walledCentre: Vector2.Zero,
                walledRadius: 50f,
                bedDepth: 0.9f);

            var bridge = new BridgeSpec("Packhorse Bridge", Vector2.Zero, Vector2.UnitX)
            {
                ArchHalfSpan = river.WallHalfWidth,
                SpringingHeight = WaterLevel + 0.25f,
            };

            var roads = new List<RoadSpec>
            {
                HighStreet(),
                Lane("Fell Road", 5f, new Vector2(-60f, -2f), new Vector2(-61f, 20f), new Vector2(-66f, 45f), new Vector2(-78f, 70f), new Vector2(-92f, 92f), new Vector2(-110f, 112f)),
                Lane("Mill Lane", 4.2f, new Vector2(-30f, 0f), new Vector2(-31f, -20f), new Vector2(-27f, -42f), new Vector2(-17f, -60f)),
                Lane("Church Lane", 4.2f, new Vector2(50f, 1.6f), new Vector2(51f, 20f), new Vector2(56f, 42f), new Vector2(66f, 62f), new Vector2(72f, 88f)),
            };

            var paths = new List<PathSpec>
            {
                new PathSpec("Riverside Walk (north)", Polyline.Smooth(new[] { new Vector2(10.5f, 6.5f), new Vector2(10f, 30f), new Vector2(7f, 60f), new Vector2(3f, 90f), new Vector2(-2f, 112f) }), 2f),
                new PathSpec("Riverside Walk (south)", Polyline.Smooth(new[] { new Vector2(-10.5f, -6.5f), new Vector2(-10f, -30f), new Vector2(-8.5f, -55f), new Vector2(-4.5f, -78f), new Vector2(2f, -100f), new Vector2(8f, -112f) }), 2f),
                new PathSpec("Mill Footpath", new Polyline(new Vector2(-17f, -60f), new Vector2(-7.3f, -62f)), 1.6f),
            };

            return new TownLayout(
                "Lake District village",
                seed: 1847,
                waterLevel: WaterLevel,
                grassLevel: GrassLevel,
                roads: roads,
                river: river,
                paths: paths,
                bridges: new[] { bridge },
                lake: new LakeSpec(new Vector2(150f, -470f), new Vector2(260f, 150f), depth: 4f));
        }

        private static RoadSpec HighStreet()
        {
            // Straight between x = -20 and 20 so the bridge lines up.
            var centre = Polyline.Smooth(new[]
            {
                new Vector2(-110f, -4.5f),
                new Vector2(-70f, -3f),
                new Vector2(-40f, 0f),
                new Vector2(-20f, 0f),
                new Vector2(20f, 0f),
                new Vector2(40f, 0f),
                new Vector2(65f, 4f),
                new Vector2(110f, 14f),
            });

            float At(float x, float z) => centre.Closest(new Vector2(x, z)).Along;

            var zebra = new ZebraCrossingMarking(At(28f, 0f));
            var markings = new List<IRoadMarking>
            {
                new CentreLineMarking(exclusions: new[] { zebra.Footprint }),
                zebra,

                // North kerb by the bridge, the south kerb either side of Mill Lane, and east of Church Lane.
                new DoubleYellowLines(new DistanceRange(At(-48f, 0f), At(-15f, 0f)), KerbSide.Left),
                new DoubleYellowLines(new DistanceRange(At(-48f, 0f), At(-33f, 0f)), KerbSide.Right),
                new DoubleYellowLines(new DistanceRange(At(-27f, 0f), At(-15f, 0f)), KerbSide.Right),
                new DoubleYellowLines(new DistanceRange(At(54f, 2f), At(75f, 6f)), KerbSide.Both),
            };

            return new RoadSpec("High Street", RoadKind.HighStreet, centre, carriagewayWidth: 7f, pavementWidth: 2f, markings);
        }

        private static RoadSpec Lane(string name, float width, params Vector2[] points)
        {
            // Lanes start on the high street's centreline, so the give-way line sits where the lane
            // leaves the main carriageway.
            var markings = new IRoadMarking[] { new GiveWayMarking(3.8f) };
            return new RoadSpec(name, RoadKind.Lane, Polyline.Smooth(points), width, pavementWidth: 0f, markings);
        }
    }
}
