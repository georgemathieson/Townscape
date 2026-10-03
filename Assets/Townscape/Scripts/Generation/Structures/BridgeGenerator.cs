using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Structures
{
    /// <summary>Builds every bridge in the layout as a humpback bridge.</summary>
    public sealed class BridgeGenerator : IStructureGenerator
    {
        public IEnumerable<MeshData> Generate(TownContext context)
        {
            foreach (var bridge in context.Layout.Bridges)
            {
                yield return new HumpbackBridgeBuilder(bridge).Build();
            }
        }
    }
}
