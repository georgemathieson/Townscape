using System.Collections.Generic;
using Townscape.Generation.Buildings;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Dressing;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Markings;
using Townscape.Generation.Routes;
using Townscape.Generation.Structures;
using Townscape.Generation.Terrain;

namespace Townscape.Generation
{
    public enum MeshCategory
    {
        Ground,
        Fells,
        Water,
        Markings,
        Structure,
        Building,
        Furniture,
        Vegetation,
        Puddles,

        /// <summary>Light fittings hung from ceilings indoors, which a walker can pass under or brush past.</summary>
        Fittings,
    }

    public static class MeshCategories
    {
        /// <summary>
        /// Whether a walker bumps into meshes of this kind. Water, road markings and puddles lie
        /// flat on something solid; plants are left out because their leaves would snag, and their
        /// trunks are marked separately with <see cref="AnchorKind.TreeTrunk"/> anchors. Hanging
        /// light fittings are left out so they never catch your head on the stairs.
        /// </summary>
        public static bool IsSolid(MeshCategory category) =>
            category == MeshCategory.Ground || category == MeshCategory.Fells || category == MeshCategory.Structure ||
            category == MeshCategory.Building || category == MeshCategory.Furniture;
    }

    public sealed class GeneratedMesh
    {
        public GeneratedMesh(MeshData mesh, MeshCategory category)
        {
            Mesh = mesh;
            Category = category;
        }

        public MeshData Mesh { get; }

        public MeshCategory Category { get; }
    }

    /// <summary>Everything generation produced, plus the ground model for runtime queries.</summary>
    public sealed class GeneratedTown
    {
        public GeneratedTown(TownContext context, IReadOnlyList<GeneratedMesh> meshes, IReadOnlyList<TownAnchor> anchors, GroundMeshStats groundStats, IReadOnlyList<TownDoor> doors = null, IReadOnlyList<TownAlarm> alarms = null, IReadOnlyList<TownBroadband> broadband = null, IReadOnlyList<TownCabinet> cabinets = null, IReadOnlyList<TownAlarmCentre> alarmCentres = null)
        {
            AlarmCentres = alarmCentres ?? System.Array.Empty<TownAlarmCentre>();
            Alarms = alarms ?? System.Array.Empty<TownAlarm>();
            Broadband = broadband ?? System.Array.Empty<TownBroadband>();
            Cabinets = cabinets ?? System.Array.Empty<TownCabinet>();
            Context = context;
            Meshes = meshes;
            Anchors = anchors;
            GroundStats = groundStats;
            Doors = doors ?? System.Array.Empty<TownDoor>();
        }

        public TownContext Context { get; }

        public IReadOnlyList<GeneratedMesh> Meshes { get; }

        /// <summary>Chimney tops, windows and lamps left by the generators for lighting and effects.</summary>
        public IReadOnlyList<TownAnchor> Anchors { get; }

        public GroundMeshStats GroundStats { get; }

        /// <summary>Doors that open, built apart from their buildings so they can swing.</summary>
        public IReadOnlyList<TownDoor> Doors { get; }

        /// <summary>Burglar alarms fitted to buildings.</summary>
        public IReadOnlyList<TownAlarm> Alarms { get; }

        /// <summary>Where each customer's fibre broadband comes into their building.</summary>
        public IReadOnlyList<TownBroadband> Broadband { get; }

        /// <summary>Street cabinets the fibre runs back to.</summary>
        public IReadOnlyList<TownCabinet> Cabinets { get; }

        /// <summary>Alarm receiving centres, where the alarms report.</summary>
        public IReadOnlyList<TownAlarmCentre> AlarmCentres { get; }

        /// <summary>Alarmed buildings as the people who come to them see them: outside, the keypad, the rooms.</summary>
        public IReadOnlyList<TownSite> Sites { get; init; } = System.Array.Empty<TownSite>();

        /// <summary>The ways people walk about the town, and through the buildings they go into.</summary>
        public RouteNetwork Walking { get; init; } = new RouteNetwork();

        /// <summary>The ways cars drive about the town.</summary>
        public RouteNetwork Driving { get; init; } = new RouteNetwork();
    }

    /// <summary>
    /// Turns a <see cref="TownLayout"/> into meshes. Pure C# with no Unity dependency, so it runs
    /// the same in the editor, in a player build, in unit tests and in the offline preview tool.
    /// </summary>
    public sealed class TownGenerator
    {
        private readonly IReadOnlyList<IStructureGenerator> _structures;

        public TownGenerator(IReadOnlyList<IStructureGenerator> structures = null)
        {
            _structures = structures ?? new IStructureGenerator[] { new BridgeGenerator(), new BuildingGenerator(), new DressingGenerator() };
        }

        /// <summary>Apron of flagstones left round each building.</summary>
        public const float PlotMargin = 0.6f;

        public static TownContext CreateContext(TownLayout layout, GenerationSettings settings)
        {
            var terrain = new BaseTerrain(layout, settings);
            var buildings = BuildingPlanner.Plan(layout);
            var ground = new GroundModel(terrain, CreateFeatures(layout, buildings));
            return new TownContext(layout, settings, terrain, ground, buildings);
        }

        public static IReadOnlyList<IGroundFeature> CreateFeatures(TownLayout layout, IReadOnlyList<BuildingPlan> buildings)
        {
            var features = new List<IGroundFeature>();
            features.Add(new RiverFeature(layout.River, layout.WaterLevel));
            foreach (var bridge in layout.Bridges)
            {
                features.Add(new FootprintFeature(bridge.Centre, bridge.Direction, bridge.HalfLength, bridge.FootprintHalfWidth));
            }

            foreach (var road in layout.Roads)
            {
                features.Add(new RoadFeature(road));
            }

            foreach (var path in layout.Paths)
            {
                features.Add(new PathFeature(path));
            }

            foreach (var building in buildings)
            {
                var finish = building.Style is IYardFinish yard ? yard.Yard : SurfaceMaterial.Pavement;
                features.Add(new PlotFeature(building.Footprint.Corners, PlotMargin, finish));
            }

            return features;
        }

        public GeneratedTown Generate(TownLayout layout, GenerationSettings settings = null)
        {
            settings ??= GenerationSettings.Default;
            var context = CreateContext(layout, settings);
            var meshes = new List<GeneratedMesh>();

            var groundGenerator = new GroundMeshGenerator(context.Ground, settings);
            foreach (var mesh in groundGenerator.Generate())
            {
                meshes.Add(new GeneratedMesh(mesh, MeshCategory.Ground));
            }

            foreach (var mesh in new FellsGenerator(context.Terrain, layout, settings).Generate())
            {
                meshes.Add(new GeneratedMesh(mesh, MeshCategory.Fells));
            }

            meshes.Add(new GeneratedMesh(WaterPlaneGenerator.Generate(settings.WaterHalfExtent, layout.WaterLevel - WaterPlaneGenerator.RiverClearance), MeshCategory.Water));
            meshes.Add(new GeneratedMesh(RiverSurfaceGenerator.Generate(layout.River, layout.WaterLevel), MeshCategory.Water));

            var markings = new MeshBuilder();
            var canvas = new MarkingCanvas(markings, context.Ground);
            foreach (var road in layout.Roads)
            {
                foreach (var marking in road.Markings)
                {
                    marking.Paint(road, canvas);
                }
            }

            if (!markings.IsEmpty)
            {
                meshes.Add(new GeneratedMesh(markings.Build("Road Markings"), MeshCategory.Markings));
            }

            var sink = new StructureSink();
            foreach (var structure in _structures)
            {
                structure.Generate(context, sink);
            }

            meshes.AddRange(sink.Meshes);
            return new GeneratedTown(context, meshes, sink.AnchorList, groundGenerator.Stats, sink.DoorList, sink.AlarmList, sink.BroadbandList, sink.CabinetList, sink.AlarmCentreList)
            {
                Sites = sink.SiteList,
                Walking = RouteNetworkBuilder.Walking(context, sink.SiteList),
                Driving = RouteNetworkBuilder.Driving(context),
            };
        }
    }
}
