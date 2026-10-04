using System.Collections.Generic;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.Security;
using Townscape.Runtime.UI;
using Townscape.Runtime.Walking;
using Townscape.Simulation.Network;
using UnityEngine;

namespace Townscape.Runtime.Network
{
    /// <summary>
    /// The fibre broadband in play: the street cabinet by the phone box and the buildings it feeds.
    /// Open the cabinet's doors and press E on the kit to plug each customer's fibre in or pull it
    /// out at the patch tray, and to run speed tests from its little screen. The lights on each
    /// building's ONT and router, and on the switch and router in the cabinet, follow
    /// <see cref="StreetCabinet"/>; a building's burglar alarm reports a comms fault while its
    /// broadband is down.
    /// </summary>
    /// <remarks>
    /// Like the alarms' state, the cabinet's lives here rather than in the store: it's something
    /// you do inside the town, not a setting.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed partial class StreetCabinetSystem : MonoBehaviour, IScreenWindow
    {
        private const float BlinkPerSecond = 2f;

        private static readonly Color LedGreen = new Color(0.2f, 3f, 0.4f);
        private static readonly Color LedAmber = new Color(3.5f, 1.6f, 0.1f);
        private static readonly Color LedRed = new Color(4f, 0.15f, 0.1f);
        private static readonly Color Unlit = new Color(0.12f, 0.12f, 0.12f);

        private readonly List<Customer> _customers = new List<Customer>();
        private readonly List<RackLight> _rackLights = new List<RackLight>();
        private readonly List<Object> _owned = new List<Object>();

        private TownCabinet _spec;
        private StreetCabinet _cabinet;
        private WalkingController _walking;

        public StreetCabinet Cabinet => _cabinet;

        /// <summary>The label on its door: "FTTP 1".</summary>
        public string Name => _spec?.Name;

        /// <summary>Puts the town's street cabinet in play, feeding every building with broadband (null if there's none).</summary>
        public static StreetCabinetSystem Create(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, WalkingController walking, IReadOnlyList<AlarmSystem> alarms)
        {
            if (town.Cabinets.Count == 0 || town.Broadband.Count == 0)
            {
                return null;
            }

            var spec = town.Cabinets[0];
            var system = TownMeshSpawner.CreateChild($"Street Cabinet: {spec.Name}", parent, hideFlags).AddComponent<StreetCabinetSystem>();
            system.Initialize(spec, town.Broadband, materials, hideFlags, walking, alarms);
            return system;
        }

        private void Initialize(TownCabinet spec, IReadOnlyList<TownBroadband> broadband, MaterialLibrary materials, HideFlags hideFlags, WalkingController walking, IReadOnlyList<AlarmSystem> alarms)
        {
            _spec = spec;
            _walking = walking;
            var plans = new List<(string, float, float)>();
            foreach (var customer in broadband)
            {
                plans.Add((customer.Customer, customer.PlanMbps, customer.PlanMbps));
            }

            _cabinet = new StreetCabinet(plans, Random.Range(1, int.MaxValue));
            var led = materials.Get(SurfaceMaterial.AlarmLed);

            // Each building's ONT and router lights, and the alarm that reports through them.
            foreach (var customer in broadband)
            {
                var group = TownMeshSpawner.CreateChild($"Broadband: {customer.Customer}", transform, hideFlags).transform;
                var ontFacing = ToUnity(customer.Ont.Facing);
                var routerFacing = ToUnity(customer.Router.Facing);
                AlarmGlow Light(string name, System.Numerics.Vector3 at, Vector3 facing, Vector3 size) =>
                    new AlarmGlow(name, group, hideFlags, led, Unlit, ToUnity(at), facing, size, _owned);

                var ontSize = Vector3.one * 0.008f;
                var routerSize = new Vector3(0.012f, 0.008f, 0.008f);
                AlarmSystem alarm = null;
                foreach (var candidate in alarms ?? new List<AlarmSystem>())
                {
                    if (candidate != null && candidate.Name == customer.Customer)
                    {
                        alarm = candidate;
                    }
                }

                _customers.Add(new Customer(
                    new[]
                    {
                        Light("ONT power", customer.OntLeds[0], ontFacing, ontSize),
                        Light("ONT fibre", customer.OntLeds[1], ontFacing, ontSize),
                        Light("ONT LOS", customer.OntLeds[2], ontFacing, ontSize),
                        Light("ONT LAN", customer.OntLeds[3], ontFacing, ontSize),
                    },
                    new[]
                    {
                        Light("Router power", customer.RouterLeds[0], routerFacing, routerSize),
                        Light("Router internet", customer.RouterLeds[1], routerFacing, routerSize),
                        Light("Router Wi-Fi", customer.RouterLeds[2], routerFacing, routerSize),
                    },
                    alarm));
            }

            // The lights on the kit in the cabinet: the customers' switch ports, the uplink, the
            // edge router's link to the exchange and the UPS.
            var facing = ToUnity(spec.Facing);
            foreach (var light in spec.Leds)
            {
                if (light.Kind == CabinetLedKind.Port && light.Port > _customers.Count)
                {
                    continue;
                }

                var glow = new AlarmGlow($"{light.Kind} {light.Port}", transform, hideFlags, led, Unlit, ToUnity(light.Position), facing, Vector3.one * 0.006f, _owned);
                _rackLights.Add(new RackLight(light.Kind, light.Port, glow));
            }

            // The kit itself, behind the doors, to press E on.
            var rack = TownMeshSpawner.CreateChild("Rack", transform, hideFlags);
            rack.transform.SetPositionAndRotation(ToUnity(spec.Rack.Position), Quaternion.LookRotation(facing, Vector3.up));
            var box = rack.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0f, -0.15f);
            box.size = new Vector3(spec.Rack.Width, spec.Rack.Height, 0.3f);
            rack.AddComponent<StreetCabinetRack>().Initialize(this);
        }

        private void Update()
        {
            if (_cabinet == null)
            {
                return;
            }

            _cabinet.Tick(Time.deltaTime);
            var blink = Mathf.Repeat(Time.time * BlinkPerSecond, 1f) < 0.5f;
            for (var line = 0; line < _customers.Count; line++)
            {
                var customer = _customers[line];
                var ont = _cabinet.Ont(line);
                customer.Ont[0].Set(Shown(ont.Power, blink));
                customer.Ont[1].Set(Shown(ont.Fibre, blink));
                customer.Ont[2].Set(Shown(ont.Los, blink));
                customer.Ont[3].Set(Shown(ont.Lan, blink));
                var router = _cabinet.Router(line);
                customer.Router[0].Set(Shown(router.Power, blink));
                customer.Router[1].Set(Shown(router.Internet, blink));
                customer.Router[2].Set(Shown(router.Wifi, blink));
                if (customer.Alarm != null)
                {
                    customer.Alarm.Alarm.ConnectInternet(_cabinet.InternetUp(line));
                }
            }

            foreach (var light in _rackLights)
            {
                light.Glow.Set(RackColour(light, blink));
            }

            UpdateTest();
            if (WindowOpen && (_walking == null || !_walking.Active))
            {
                CloseWindow();
            }
        }

        private void OnDestroy()
        {
            if (WindowOpen)
            {
                CloseWindow();
            }

            _skin?.Dispose();
            foreach (var owned in _owned)
            {
                ObjectUtility.Destroy(owned);
            }

            _owned.Clear();
        }

        // A switch port flickers green with traffic while its line is up, and blinks amber when
        // it's counting errors; the uplink flickers with everyone's traffic; the edge router's
        // link to the exchange goes amber when it's overloaded.
        private Color RackColour(RackLight light, bool blink)
        {
            switch (light.Kind)
            {
                case CabinetLedKind.Port:
                    var line = light.Port - 1;
                    var fibre = _cabinet.Lines[line];
                    if (!fibre.HasLight)
                    {
                        return Color.black;
                    }

                    if (_cabinet.FaultOn(line) == LineFault.FailingPort)
                    {
                        return blink ? LedAmber : Color.black;
                    }

                    // Lit steady while the customer's box gets in sync, then flickering with traffic.
                    return fibre.Up && Traffic(light.Port) ? LedGreen * 0.15f : LedGreen;
                case CabinetLedKind.Uplink:
                    return Traffic(0) ? LedGreen * 0.3f : LedGreen;
                case CabinetLedKind.Wan:
                    return _cabinet.Fault == LineFault.CongestedUplink ? LedAmber : LedGreen;
                default:
                    return LedGreen;
            }
        }

        // An irregular flicker, different for each light.
        private static bool Traffic(int seed) => Mathf.PerlinNoise(Time.time * 9f, seed * 7.3f) < 0.35f;

        private static Color Shown(Led led, bool blink)
        {
            switch (led)
            {
                case Led.Green: return LedGreen;
                case Led.GreenBlink: return blink ? LedGreen : Color.black;
                case Led.Amber: return LedAmber;
                case Led.AmberBlink: return blink ? LedAmber : Color.black;
                case Led.Red: return LedRed;
                case Led.RedBlink: return blink ? LedRed : Color.black;
                default: return Color.black;
            }
        }

        private static Vector3 ToUnity(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

        private sealed class Customer
        {
            public Customer(AlarmGlow[] ont, AlarmGlow[] router, AlarmSystem alarm)
            {
                Ont = ont;
                Router = router;
                Alarm = alarm;
            }

            /// <summary>Power, fibre, LOS and LAN.</summary>
            public AlarmGlow[] Ont { get; }

            /// <summary>Power, internet and Wi-Fi.</summary>
            public AlarmGlow[] Router { get; }

            public AlarmSystem Alarm { get; }
        }

        private sealed class RackLight
        {
            public RackLight(CabinetLedKind kind, int port, AlarmGlow glow)
            {
                Kind = kind;
                Port = port;
                Glow = glow;
            }

            public CabinetLedKind Kind { get; }

            public int Port { get; }

            public AlarmGlow Glow { get; }
        }
    }
}
