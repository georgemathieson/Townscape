using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation
{
    /// <summary>
    /// Where a customer's fibre broadband comes into a building: the optical network terminal
    /// (ONT) the fibre ends at, and the router beside it, each on the wall with a row of lights.
    /// </summary>
    public sealed class TownBroadband
    {
        public TownBroadband(string customer, float planMbps, WallMount ont, IReadOnlyList<Vector3> ontLeds, WallMount router, IReadOnlyList<Vector3> routerLeds)
        {
            Customer = customer;
            PlanMbps = planMbps;
            Ont = ont;
            OntLeds = ontLeds;
            Router = router;
            RouterLeds = routerLeds;
        }

        /// <summary>Who it's for: the same name as the building's burglar alarm, whose panel reports through it.</summary>
        public string Customer { get; }

        /// <summary>The speed they pay for, the same both ways, in megabits a second.</summary>
        public float PlanMbps { get; }

        public WallMount Ont { get; }

        /// <summary>The ONT's lights, top to bottom: power, fibre, loss of signal (LOS) and LAN.</summary>
        public IReadOnlyList<Vector3> OntLeds { get; }

        public WallMount Router { get; }

        /// <summary>The router's lights, along its bottom edge from the ONT's side: power, internet and Wi-Fi.</summary>
        public IReadOnlyList<Vector3> RouterLeds { get; }
    }

    public enum CabinetLedKind
    {
        /// <summary>A customer's port on the fibre switch: lit while the line is up.</summary>
        Port,

        /// <summary>The switch's uplink to the edge router.</summary>
        Uplink,

        /// <summary>The edge router's link back to the exchange.</summary>
        Wan,

        /// <summary>The UPS, green while it's running on mains.</summary>
        Ups,
    }

    /// <summary>A light on the kit in a street cabinet.</summary>
    public readonly struct CabinetLed
    {
        public CabinetLed(CabinetLedKind kind, int port, Vector3 position)
        {
            Kind = kind;
            Port = port;
            Position = position;
        }

        public CabinetLedKind Kind { get; }

        /// <summary>For a <see cref="CabinetLedKind.Port"/>, which port it is (from 1).</summary>
        public int Port { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// A green street cabinet the buildings' fibre runs back to. Behind its doors is a rack of kit
    /// (a power strip, a UPS, a fibre switch, an edge router and a patch tray), with a little
    /// computer screen beside it.
    /// </summary>
    public sealed class TownCabinet
    {
        public TownCabinet(string name, WallMount rack, Vector3 facing, IReadOnlyList<CabinetLed> leds, WallMount screen)
        {
            Name = name;
            Rack = rack;
            Facing = facing;
            Leds = leds;
            Screen = screen;
        }

        /// <summary>The label on its door: "FTTP 1".</summary>
        public string Name { get; }

        /// <summary>The front of the rack and screen, as far forward as the kit comes.</summary>
        public WallMount Rack { get; }

        /// <summary>The way its doors face.</summary>
        public Vector3 Facing { get; }

        public IReadOnlyList<CabinetLed> Leds { get; }

        public WallMount Screen { get; }
    }
}
