using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation
{
    /// <summary>
    /// A point on a way through the town: where to walk to, what it's called if it's somewhere
    /// people go on purpose ("The Copper Kettle: till"), and the door (if any) to go through on
    /// the way to it from the stop before.
    /// </summary>
    public readonly struct RouteStop
    {
        public RouteStop(Vector3 position, string name = null, string door = null)
        {
            Position = position;
            Name = name;
            Door = door;
        }

        /// <summary>On the floor, where the feet go.</summary>
        public Vector3 Position { get; }

        public string Name { get; }

        /// <summary>The <see cref="TownDoor.Name"/> of the door between the stop before and this one.</summary>
        public string Door { get; }
    }

    /// <summary>
    /// A way through a building, stop by stop: in from the street, through its doors, up its
    /// stairs and round its rooms. Each stop leads on to the next; routes that share a stop
    /// (by name or by place) join up there.
    /// </summary>
    public sealed class TownRoute
    {
        public TownRoute(IReadOnlyList<RouteStop> stops, bool joinsStreet = false)
        {
            Stops = stops;
            JoinsStreet = joinsStreet;
        }

        public IReadOnlyList<RouteStop> Stops { get; }

        /// <summary>The first stop is outside, and joins on to the pavement nearest it.</summary>
        public bool JoinsStreet { get; }
    }

    /// <summary>
    /// A building with an alarm, as the people who come to it see it: where to stand outside,
    /// the keypad, the rooms to look round, and what a burglar would be after. Every place is the
    /// name of a <see cref="RouteStop"/> on one of its routes.
    /// </summary>
    public sealed class TownSite
    {
        public TownSite(string name, IReadOnlyList<TownRoute> routes, string outside, string keypad, IReadOnlyList<string> rooms, IReadOnlyList<string> loot)
        {
            Name = name;
            Routes = routes;
            Outside = outside;
            Keypad = keypad;
            Rooms = rooms;
            Loot = loot;
        }

        /// <summary>The alarm's name: "The Copper Kettle", "The flat".</summary>
        public string Name { get; }

        public IReadOnlyList<TownRoute> Routes { get; }

        /// <summary>On the pavement in front of its door.</summary>
        public string Outside { get; }

        /// <summary>Standing at the alarm's keypad.</summary>
        public string Keypad { get; }

        /// <summary>The middle of each room the alarm covers, in the order you'd look round them.</summary>
        public IReadOnlyList<string> Rooms { get; }

        /// <summary>Where a burglar heads for: the till, the telly.</summary>
        public IReadOnlyList<string> Loot { get; }
    }
}
