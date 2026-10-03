namespace Townscape.Generation.Structures
{
    /// <summary>Builds every bridge in the layout as a humpback bridge.</summary>
    public sealed class BridgeGenerator : IStructureGenerator
    {
        public void Generate(TownContext context, StructureSink sink)
        {
            foreach (var bridge in context.Layout.Bridges)
            {
                sink.AddMesh(new HumpbackBridgeBuilder(bridge).Build(), MeshCategory.Structure);
            }
        }
    }
}
