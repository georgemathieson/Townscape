using System;
using Townscape.Generation.Structures;

namespace Townscape.Generation.Dressing
{
    /// <summary>Runs the layout's dressing rules in order and emits their meshes, chunked by area.</summary>
    public sealed class DressingGenerator : IStructureGenerator
    {
        public void Generate(TownContext context, StructureSink sink)
        {
            var dressing = new DressingContext(context, sink.Anchors);
            var rules = context.Layout.Dressing;
            for (var i = 0; i < rules.Count; i++)
            {
                rules[i].Apply(dressing, new Random((context.Layout.Seed * 31) + i));
            }

            foreach (var (_, layer, mesh) in dressing.Build())
            {
                sink.AddMesh(mesh, layer == DressingLayer.Furniture ? MeshCategory.Furniture : MeshCategory.Vegetation);
            }
        }
    }
}
