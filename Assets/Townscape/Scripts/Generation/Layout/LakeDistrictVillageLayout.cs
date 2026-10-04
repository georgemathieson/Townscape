using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Dressing;
using Townscape.Generation.Dressing.Props;
using Townscape.Generation.Dressing.Rules;
using Townscape.Generation.Geometry;
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

        /// <summary>The label on the fibre cabinet beside the phone box.</summary>
        public const string FibreCabinetName = "FTTP 1";

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
                new PathSpec("Church Path", new Polyline(new Vector2(69.5f, 70f), new Vector2(75.2f, 70f)), 1.6f),
                new PathSpec("Green Walk", Polyline.Smooth(new[] { new Vector2(-62.5f, 38f), new Vector2(-30f, 40.5f), new Vector2(-9f, 36f) }), 1.8f),
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
                lake: new LakeSpec(new Vector2(150f, -470f), new Vector2(260f, 150f), depth: 4f))
            {
                Terraces = Terraces(roads[0]),
                Detached = Detached(roads[0], roads[1], roads[2], roads[3]),
                Dressing = Dressing(roads[0], roads[1], roads[2], roads[3], river),
            };
        }

        /// <summary>Street furniture, walls and greenery, applied in order after the buildings.</summary>
        private static IReadOnlyList<IDressingRule> Dressing(RoadSpec high, RoadSpec fellRoad, RoadSpec millLane, RoadSpec churchLane, RiverSpec river)
        {
            var greenCentre = new Vector2(-30f, 46f);
            Vector2 OnHighStreet(float x, float offset) => high.PointAt(high.AlongNearest(new Vector2(x, 0f)), offset);
            Vector2 TowardsRoad(float x, float offset) => OnHighStreet(x, 0f) - OnHighStreet(x, offset);

            var walls = new List<Polyline>();
            walls.AddRange(DryStoneWallsRule.AlongRoad(fellRoad, 8f, fellRoad.Centre.Length, 1.3f));
            walls.AddRange(DryStoneWallsRule.AlongRoad(churchLane, 8f, churchLane.Centre.Length, 1.3f));
            walls.AddRange(DryStoneWallsRule.AlongRoad(millLane, 8f, millLane.Centre.Length, 1.3f));
            walls.Add(new Polyline(new Vector2(76f, 61f), new Vector2(99f, 61f), new Vector2(99f, 83f), new Vector2(76.5f, 83f)));
            walls.Add(new Polyline(new Vector2(58f, 34f), new Vector2(78f, 35f), new Vector2(98f, 36f)));
            walls.Add(new Polyline(new Vector2(-98f, 20f), new Vector2(-82f, 22f), new Vector2(-66f, 22f)));
            walls.Add(new Polyline(new Vector2(-98f, -30f), new Vector2(-60f, -32f), new Vector2(-48f, -42f)));
            walls.Add(new Polyline(new Vector2(20f, -30f), new Vector2(60f, -28f), new Vector2(98f, -26f)));

            var props = new List<PlacedProp>
            {
                new PlacedProp(new PhoneBox(), new Vector2(-12.5f, 9f), new Vector2(0f, -1f)),
                new PlacedProp(new FibreCabinet(FibreCabinetName), new Vector2(-11.05f, 8.95f), new Vector2(0f, -1f)),
                new PlacedProp(new Bench(), new Vector2(-8.6f, 11f), new Vector2(1f, 0f)),
                new PlacedProp(new PillarBox(), OnHighStreet(44.5f, -(high.HalfWidth + 0.45f)), TowardsRoad(44.5f, -(high.HalfWidth + 0.45f))),
                new PlacedProp(new BusStop(), OnHighStreet(-78f, -(high.HalfWidth + 0.35f)), TowardsRoad(-78f, -(high.HalfWidth + 0.35f))),
                new PlacedProp(new Bench(), OnHighStreet(-76f, -(high.HalfWidth + 1.65f)), TowardsRoad(-76f, -(high.HalfWidth + 1.65f))),

                // Riverside benches looking over the water.
                new PlacedProp(new Bench(), new Vector2(7.6f, 35f), new Vector2(-1f, 0f)),
                new PlacedProp(new Bench(), new Vector2(3f, 75f), new Vector2(-1f, 0f)),
                new PlacedProp(new Bench(), new Vector2(-7.3f, -40f), new Vector2(1f, 0f)),
                new PlacedProp(new Bench(), new Vector2(-0.3f, -85f), new Vector2(1f, 0f)),

                // The village green and the church.
                new PlacedProp(new Memorial(), greenCentre, new Vector2(0f, -1f)),
                new PlacedProp(new Bench(), new Vector2(-35f, 42.6f), new Vector2(0.5f, 1f)),
                new PlacedProp(new Bench(), new Vector2(-25f, 42.6f), new Vector2(-0.5f, 1f)),
                new PlacedProp(new Tree(TreeKind.Broadleaf, 1.2f), new Vector2(-41f, 53f), Vector2.UnitY, DressingLayer.Vegetation),
                new PlacedProp(new Tree(TreeKind.Broadleaf, 1.1f), new Vector2(-19f, 54f), Vector2.UnitY, DressingLayer.Vegetation),
                new PlacedProp(new Tree(TreeKind.Birch), new Vector2(-16f, 31f), Vector2.UnitY, DressingLayer.Vegetation),
                new PlacedProp(new Bench(), new Vector2(72.5f, 67.8f), new Vector2(0f, 1f)),
            };

            return new IDressingRule[]
            {
                new StreetLampsRule(high, 17f, bothSides: true, baskets: true),
                new StreetLampsRule(fellRoad, 24f, bothSides: false, baskets: false),
                new StreetLampsRule(churchLane, 24f, bothSides: false, baskets: false),
                new StreetLampsRule(millLane, 24f, bothSides: false, baskets: false),
                new BelishaBeaconsRule(high),
                new RiverRailingsRule(river),
                new PlacedPropsRule(props),
                new FlowerBedRule(greenCentre, 1.8f, 2.6f),
                new ChurchyardRule(new Vector2(76f, 61f), new Vector2(99f, 83f), new Vector2(1f, 0f)),
                new DryStoneWallsRule(walls),
                new TreesRule(new[] { new OpenSpace(new Vector2(-30f, 44f), 9f), new OpenSpace(new Vector2(87f, 72f), 12f) }),
                new GroundCoverRule(),
                new PuddlesRule(),
            };
        }

        /// <summary>
        /// The high street terraces, west to east. Shops cluster near the bridge; the far ends are
        /// terraced houses. Gaps are left for the lanes and the riverside paths.
        /// </summary>
        private static IReadOnlyList<TerraceSpec> Terraces(RoadSpec high)
        {
            float At(float x) => high.AlongNearest(new Vector2(x, 0f));
            TerraceUnit Shop(Buildings.Shops.ShopDefinition shop, float weight = 1f) => new TerraceUnit(shop, weight);
            TerraceUnit[] Houses(int count)
            {
                var houses = new TerraceUnit[count];
                for (var i = 0; i < count; i++)
                {
                    houses[i] = TerraceUnit.House();
                }

                return houses;
            }

            return new[]
            {
                // North side.
                new TerraceSpec("Terrace NW (west)", high, KerbSide.Left, At(-95f), At(-63.5f), Houses(5), 101),
                new TerraceSpec("Terrace NW (bridge)", high, KerbSide.Left, At(-56.5f), At(-17f), new[]
                {
                    TerraceUnit.House(), TerraceUnit.House(), Shop(VillageShops.Butcher), Shop(VillageShops.Ironmonger), Shop(VillageShops.Chemist), Shop(VillageShops.FellGallery),
                }, 102),
                new TerraceSpec("Terrace NE (bridge)", high, KerbSide.Left, At(13f), At(46.5f), new[]
                {
                    Shop(VillageShops.Packhorse, 1.25f), Shop(VillageShops.FellsideCoffee), Shop(VillageShops.LanternBooks), Shop(VillageShops.HartleysNews), Shop(VillageShops.PixelAndByte),
                }, 103),
                new TerraceSpec("Terrace NE (east)", high, KerbSide.Left, At(56f), At(95f), Houses(6), 104),

                // South side.
                new TerraceSpec("Terrace SW (west)", high, KerbSide.Right, At(-95f), At(-34f), Houses(9), 105),
                new TerraceSpec("Terrace SW (bridge)", high, KerbSide.Right, At(-26.5f), At(-13f), new[]
                {
                    Shop(VillageShops.Barber), Shop(VillageShops.SkeinAndFell),
                }, 106),
                new TerraceSpec("Terrace SE (bridge)", high, KerbSide.Right, At(13f), At(40f), new[]
                {
                    Shop(VillageShops.CoopersBakery), Shop(VillageShops.MrsDodds), Shop(VillageShops.CopperKettle), Shop(VillageShops.LakesideChippy),
                }, 107),
                new TerraceSpec("Terrace SE (middle)", high, KerbSide.Right, At(42.5f), At(70f), new[]
                {
                    Shop(VillageShops.PostOffice), Shop(VillageShops.FellAndCrag), Shop(VillageShops.Bluebell), TerraceUnit.House(), TerraceUnit.House(),
                }, 108),
            };
        }

        /// <summary>
        /// Cottages along the lanes, a detached bookshop, the church, the old mill, and the petrol
        /// station on the way out of the village at the east end of the high street.
        /// </summary>
        private static IReadOnlyList<DetachedBuildingSpec> Detached(RoadSpec high, RoadSpec fellRoad, RoadSpec millLane, RoadSpec churchLane)
        {
            var whitewash = new HouseDesign();
            var stone = new HouseDesign
            {
                Wall = SurfaceMaterial.Stone,
                Windows = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.Casement, SurfaceMaterial.Kerb),
                DoorPaint = SurfaceMaterial.PaintOxblood,
                Trim = SurfaceMaterial.Timber,
            };
            var greenStone = new HouseDesign
            {
                Wall = SurfaceMaterial.StoneGreen,
                Windows = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.TwoOverTwo, SurfaceMaterial.Kerb),
                DoorPaint = SurfaceMaterial.PaintNavy,
                Trim = SurfaceMaterial.PaintWhite,
            };
            var cream = new HouseDesign
            {
                Wall = SurfaceMaterial.RenderCream,
                Windows = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.Casement, SurfaceMaterial.StoneDark, SurfaceMaterial.PaintWhite),
                DoorPaint = SurfaceMaterial.PaintSage,
            };

            DetachedBuildingSpec Cottage(string name, RoadSpec road, float along, KerbSide side, HouseDesign design, int seed) =>
                DetachedBuildingSpec.FacingRoad(name, road, along, side, 4f, 7.5f, 6.5f, new DetachedHouseStyle(design), seed);

            return new[]
            {
                Cottage("Fell Road cottage 1", fellRoad, 32f, KerbSide.Right, whitewash, 201),
                Cottage("Fell Road cottage 2", fellRoad, 50f, KerbSide.Right, stone, 202),
                Cottage("Fell Road cottage 3", fellRoad, 68f, KerbSide.Right, cream, 203),
                Cottage("Fell Road cottage 4", fellRoad, 40f, KerbSide.Left, greenStone, 204),
                Cottage("Fell Road cottage 5", fellRoad, 62f, KerbSide.Left, whitewash, 205),
                Cottage("Church Lane cottage 1", churchLane, 46f, KerbSide.Left, stone, 206),
                Cottage("Church Lane cottage 2", churchLane, 36f, KerbSide.Right, whitewash, 207),
                Cottage("Church Lane cottage 3", churchLane, 58f, KerbSide.Right, greenStone, 208),
                Cottage("Mill Lane cottage 1", millLane, 30f, KerbSide.Right, cream, 209),
                Cottage("Mill Lane cottage 2", millLane, 24f, KerbSide.Left, whitewash, 210),
                Cottage("Mill Lane cottage 3", millLane, 46f, KerbSide.Right, stone, 211),

                DetachedBuildingSpec.FacingRoad("The Inkwell", churchLane, 28f, KerbSide.Left, 2.5f, 7f, 6.5f, new DetachedHouseStyle(new HouseDesign
                {
                    Wall = SurfaceMaterial.StoneGreen,
                    Bays = 2,
                    Shop = VillageShops.Inkwell,
                    Windows = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.SixOverSix, SurfaceMaterial.Kerb),
                }), 212),

                DetachedBuildingSpec.FacingRoad("Fell View Garage", high, high.AlongNearest(new Vector2(83f, 0f)), KerbSide.Right, 0.3f, 23f, 17.5f, new PetrolStationStyle(), 215),

                DetachedBuildingSpec.Facing("St Bega's Church", new Vector2(75f, 70f), new Vector2(-1f, 0f), 8f, 22f, new ChurchStyle(), 213),

                DetachedBuildingSpec.Facing("The Old Mill", new Vector2(-19f, -68f), new Vector2(0f, 1f), 12f, 8f, new DetachedHouseStyle(new HouseDesign
                {
                    Floors = 3,
                    Bays = 5,
                    Wall = SurfaceMaterial.Stone,
                    Windows = new WindowStyle(SurfaceMaterial.PaintWhite, GlazingPattern.SixOverSix, SurfaceMaterial.Kerb),
                    DoorPaint = SurfaceMaterial.PaintDarkGreen,
                    Trim = SurfaceMaterial.Timber,
                    Pitch = 35f,
                    Sign = "THE OLD MILL",
                    Pots = 3,
                }), 214),
            };
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
