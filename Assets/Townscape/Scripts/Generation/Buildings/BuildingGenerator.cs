using System.Collections.Generic;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;
using Townscape.Generation.Structures;

namespace Townscape.Generation.Buildings
{
    /// <summary>
    /// Builds every planned building. Buildings in the same group (a terrace) share a mesh, split
    /// into parts if a mesh would outgrow 16-bit indices, and a second mesh for hanging light
    /// fittings, which walkers pass through.
    /// </summary>
    public sealed class BuildingGenerator : IStructureGenerator
    {
        private const int MaxVerticesPerMesh = 50000;

        public void Generate(TownContext context, StructureSink sink)
        {
            var order = new List<string>();
            var groups = new Dictionary<string, List<Planning.BuildingPlan>>();
            foreach (var plan in context.Buildings)
            {
                if (!groups.TryGetValue(plan.Group, out var list))
                {
                    list = new List<Planning.BuildingPlan>();
                    groups.Add(plan.Group, list);
                    order.Add(plan.Group);
                }

                list.Add(plan);
            }

            foreach (var group in order)
            {
                var builder = new MeshBuilder();
                var fittings = new MeshBuilder();
                var part = 0;
                foreach (var plan in groups[group])
                {
                    if (builder.VertexCount > MaxVerticesPerMesh)
                    {
                        sink.AddMesh(builder.Build($"{group} ({++part})"), MeshCategory.Building);
                        builder = new MeshBuilder();
                    }

                    plan.Style.Build(plan.Footprint, new BuildContext(builder, sink.Anchors, plan.Seed, sink.Doors, fittings, sink.Alarms, sink.Broadband, sink.AlarmCentres, sink.Sites));
                }

                if (!builder.IsEmpty)
                {
                    sink.AddMesh(builder.Build(part == 0 ? group : $"{group} ({part + 1})"), MeshCategory.Building);
                }

                if (!fittings.IsEmpty)
                {
                    sink.AddMesh(fittings.Build($"{group} fittings"), MeshCategory.Fittings);
                }
            }
        }
    }
}
