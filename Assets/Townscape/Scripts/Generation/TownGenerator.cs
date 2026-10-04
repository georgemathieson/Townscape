using System.Collections.Generic;
using Townscape.Generation.Buildings;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Dressing;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Markings;
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
        public GeneratedTown(TownContext context, IReadOnlyList<GeneratedMesh> meshes, IReadOnlyList<TownAnchor> anchors, GroundMeshStats groundStats)
        {
            Context = context;
            Meshes = meshes;
            Anchors = anchors;
            GroundStats = groundStats;
        }

        public TownContext Context { get; }

        public IReadOnlyList<GeneratedMesh> Meshes { get; }

        /// <summary>Chimney tops, windows and lamps left by the generators for lighting and effects.</summary>
        public IReadOnlyList<TownAnchor> Anchors { get; }

        public GroundMeshStats GroundStats { get; }
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
            return new GeneratedTown(context, meshes, sink.AnchorList, groundGenerator.Stats);
        }
    }
}
