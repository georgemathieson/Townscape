using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Structures
{
    /// <summary>
    /// Strategy for one family of structures (bridges, buildings, and later street furniture).
    /// The town generator runs every registered strategy.
    /// </summary>
    public interface IStructureGenerator
    {
        void Generate(TownContext context, StructureSink sink);
    }

    /// <summary>Collects what structure generators produce: meshes, and anchors for later systems.</summary>
    public sealed class StructureSink
    {
        private readonly List<GeneratedMesh> _meshes = new List<GeneratedMesh>();
        private readonly List<TownAnchor> _anchors = new List<TownAnchor>();

        public IReadOnlyList<GeneratedMesh> Meshes => _meshes;

        /// <summary>Anchors (chimney tops, windows, lamps) for lighting and effects to use later.</summary>
        public ICollection<TownAnchor> Anchors => _anchors;

        public IReadOnlyList<TownAnchor> AnchorList => _anchors;

        public void AddMesh(MeshData mesh, MeshCategory category)
        {
            _meshes.Add(new GeneratedMesh(mesh, category));
        }
    }
}
