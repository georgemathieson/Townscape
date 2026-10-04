using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation
{
    public enum AlarmZoneKind
    {
        /// <summary>A contact on a door (or a window that opens): it trips when the door moves.</summary>
        Door,

        /// <summary>A motion sensor high in a room.</summary>
        Motion,
    }

    /// <summary>One of an alarm's zones, wired back to its control box: a door's contact or a motion sensor.</summary>
    public sealed class AlarmZone
    {
        public AlarmZone(string name, AlarmZoneKind kind, Vector3 position, Vector3 facing, Vector3 led, string door = null)
        {
            Name = name;
            Kind = kind;
            Position = position;
            Facing = facing;
            Led = led;
            Door = door;
        }

        /// <summary>What the control box's label says: "Café door", "Kitchen sensor".</summary>
        public string Name { get; }

        public AlarmZoneKind Kind { get; }

        /// <summary>A sensor's lens, or where a door's contact is.</summary>
        public Vector3 Position { get; }

        /// <summary>The level way a sensor looks into its room.</summary>
        public Vector3 Facing { get; }

        /// <summary>A sensor's LED, which lights when it sees you move.</summary>
        public Vector3 Led { get; }

        /// <summary>For a door's contact, the <see cref="TownDoor.Name"/> of the door it's on.</summary>
        public string Door { get; }
    }

    /// <summary>Something fixed flat to a wall: the middle of its face, the way it faces, and its size.</summary>
    public readonly struct WallMount
    {
        public WallMount(Vector3 position, Vector3 facing, float width, float height)
        {
            Position = position;
            Facing = facing;
            Width = width;
            Height = height;
        }

        public Vector3 Position { get; }

        public Vector3 Facing { get; }

        public float Width { get; }

        public float Height { get; }
    }

    /// <summary>An alarm keypad on a wall, with its power and fault lights.</summary>
    public sealed class AlarmKeypadMount
    {
        public AlarmKeypadMount(WallMount panel, Vector3 powerLed, Vector3 faultLed)
        {
            Panel = panel;
            PowerLed = powerLed;
            FaultLed = faultLed;
        }

        public WallMount Panel { get; }

        public Vector3 PowerLed { get; }

        public Vector3 FaultLed { get; }
    }

    /// <summary>A burglar alarm: its zones, keypads, control box and bell box.</summary>
    public sealed class TownAlarm
    {
        public TownAlarm(string name, IReadOnlyList<AlarmZone> zones, IReadOnlyList<AlarmKeypadMount> keypads, WallMount controlBox, WallMount bellStrobe)
        {
            Name = name;
            Zones = zones;
            Keypads = keypads;
            ControlBox = controlBox;
            BellStrobe = bellStrobe;
        }

        /// <summary>Whose alarm it is: "The Copper Kettle", "The flat".</summary>
        public string Name { get; }

        public IReadOnlyList<AlarmZone> Zones { get; }

        public IReadOnlyList<AlarmKeypadMount> Keypads { get; }

        /// <summary>The white box the zones are wired back to, with the panel's board inside.</summary>
        public WallMount ControlBox { get; }

        /// <summary>The front of the bell box's strobe lens.</summary>
        public WallMount BellStrobe { get; }
    }
}
