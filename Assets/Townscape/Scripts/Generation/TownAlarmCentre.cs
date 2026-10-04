using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation
{
    /// <summary>
    /// An alarm receiving centre: an office of operators' desks where every alarm in the village
    /// reports. Each desk's monitors are a console to look at the alarms on, and a wall of
    /// screens shows every site at a glance.
    /// </summary>
    public sealed class TownAlarmCentre
    {
        public TownAlarmCentre(string name, IReadOnlyList<WallMount> consoles, IReadOnlyList<WallMount> videoWall, Vector3 room, string guardPost = null)
        {
            Name = name;
            GuardPost = guardPost;
            Consoles = consoles;
            VideoWall = videoWall;
            Room = room;
        }

        public string Name { get; }

        /// <summary>The front of each desk's monitors: look at one and press E.</summary>
        public IReadOnlyList<WallMount> Consoles { get; }

        /// <summary>The screens on the wall the desks face, one per site.</summary>
        public IReadOnlyList<WallMount> VideoWall { get; }

        /// <summary>The middle of the room, at head height: where its chime sounds.</summary>
        public Vector3 Room { get; }

        /// <summary>Where the key-holding guard waits to be sent out: the name of a <see cref="RouteStop"/>.</summary>
        public string GuardPost { get; }
    }
}
