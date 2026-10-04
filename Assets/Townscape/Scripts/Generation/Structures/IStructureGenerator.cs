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
        private readonly List<TownDoor> _doors = new List<TownDoor>();
        private readonly List<TownAlarm> _alarms = new List<TownAlarm>();
        private readonly List<TownBroadband> _broadband = new List<TownBroadband>();
        private readonly List<TownCabinet> _cabinets = new List<TownCabinet>();

        public IReadOnlyList<GeneratedMesh> Meshes => _meshes;

        /// <summary>Anchors (chimney tops, windows, lamps) for lighting and effects to use later.</summary>
        public ICollection<TownAnchor> Anchors => _anchors;

        public IReadOnlyList<TownAnchor> AnchorList => _anchors;

        /// <summary>Doors that open, each with its own leaf mesh.</summary>
        public ICollection<TownDoor> Doors => _doors;

        public IReadOnlyList<TownDoor> DoorList => _doors;

        /// <summary>Burglar alarms, each with its zones, keypads, control box and bell box.</summary>
        public ICollection<TownAlarm> Alarms => _alarms;

        public IReadOnlyList<TownAlarm> AlarmList => _alarms;

        /// <summary>Where each customer's fibre broadband comes into their building.</summary>
        public ICollection<TownBroadband> Broadband => _broadband;

        public IReadOnlyList<TownBroadband> BroadbandList => _broadband;

        /// <summary>Street cabinets the fibre runs back to.</summary>
        public ICollection<TownCabinet> Cabinets => _cabinets;

        public IReadOnlyList<TownCabinet> CabinetList => _cabinets;

        public void AddMesh(MeshData mesh, MeshCategory category)
        {
            _meshes.Add(new GeneratedMesh(mesh, category));
        }
    }
}
