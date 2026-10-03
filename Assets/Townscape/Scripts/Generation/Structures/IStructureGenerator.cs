using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Structures
{
    /// <summary>
    /// Strategy for one family of structures (bridges now; terraces, cottages and street
    /// furniture later). The town generator runs every registered strategy.
    /// </summary>
    public interface IStructureGenerator
    {
        IEnumerable<MeshData> Generate(TownContext context);
    }
}
