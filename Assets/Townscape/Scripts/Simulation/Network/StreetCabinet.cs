using System;
using System.Collections.Generic;

namespace Townscape.Simulation.Network
{
    /// <summary>What can go wrong with the fibre, one thing at a time, for whoever opens the cabinet to find.</summary>
    public enum LineFault
    {
        None,

        /// <summary>Dust on the line's connector in the patch tray: weaker light, slower speeds and a little loss.</summary>
        DirtyConnector,

        /// <summary>
        /// The line's fibre is bent too tightly in the tray: so little light gets through that the
        /// customer's box keeps losing it, with heavy loss and jitter in between.
        /// </summary>
        BentFibre,

        /// <summary>The line's port on the fibre switch is failing: the light's fine, but frames arrive corrupted.</summary>
        FailingPort,

        /// <summary>The cabinet's link back to the exchange is overloaded: every line is slow and pings are long.</summary>
        CongestedUplink,
    }

    /// <summary>One of the little lights on the customer's boxes.</summary>
    public enum Led
    {
        Off,
        Green,
        GreenBlink,
        Amber,
        AmberBlink,
        Red,
        RedBlink,
    }

    /// <summary>The lights on a customer's optical network terminal, where the fibre comes into the building.</summary>
    public readonly struct OntLights
    {
        public OntLights(Led power, Led fibre, Led los, Led lan)
        {
            Power = power;
            Fibre = fibre;
            Los = los;
            Lan = lan;
        }

        public Led Power { get; }

        /// <summary>Steady once it's in sync with the cabinet, blinking while it gets there.</summary>
        public Led Fibre { get; }

        /// <summary>Loss of signal: blinks red when no light is coming down the fibre.</summary>
        public Led Los { get; }

        /// <summary>The cable to the router.</summary>
        public Led Lan { get; }
    }

    /// <summary>The lights on a customer's router.</summary>
    public readonly struct RouterLights
    {
        public RouterLights(Led power, Led internet, Led wifi)
        {
            Power = power;
            Internet = internet;
            Wifi = wifi;
        }

        public Led Power { get; }

        /// <summary>Green when it's online, blinking amber while it connects, blinking red when it can't.</summary>
        public Led Internet { get; }

        public Led Wifi { get; }
    }

    /// <summary>What a speed test from the cabinet's screen found.</summary>
    public readonly struct SpeedTestResult
    {
        public SpeedTestResult(bool connected, float rxDbm, float downMbps, float upMbps, float pingMs, float jitterMs, float lossPercent, long portErrors)
        {
            Connected = connected;
            RxDbm = rxDbm;
            DownMbps = downMbps;
            UpMbps = upMbps;
            PingMs = pingMs;
            JitterMs = jitterMs;
            LossPercent = lossPercent;
            PortErrors = portErrors;
        }

        /// <summary>False when the line isn't up (unpatched, or still getting in sync): there's nothing to test.</summary>
        public bool Connected { get; }

        /// <summary>How much light the customer's box is getting, in dBm.</summary>
        public float RxDbm { get; }

        public float DownMbps { get; }

        public float UpMbps { get; }

        public float PingMs { get; }

        public float JitterMs { get; }

        public float LossPercent { get; }

        /// <summary>Corrupted frames (CRC errors) counted on the line's switch port so far.</summary>
        public long PortErrors { get; }
    }

    /// <summary>One customer's fibre: from a port on the cabinet's switch, through the patch tray, to their building.</summary>
    public sealed class FibreLine
    {
        internal FibreLine(string customer, int port, float planDownMbps, float planUpMbps)
        {
            Customer = customer;
            Port = port;
            PlanDownMbps = planDownMbps;
            PlanUpMbps = planUpMbps;
        }

        /// <summary>Who it's for (the same name as the building's burglar alarm).</summary>
        public string Customer { get; }

        /// <summary>Its port on the switch, from 1.</summary>
        public int Port { get; }

        public float PlanDownMbps { get; }

        public float PlanUpMbps { get; }

        /// <summary>Plugged in at the patch tray.</summary>
        public bool Patched { get; internal set; } = true;

        /// <summary>Seconds left of getting in sync with the cabinet after the light comes back.</summary>
        public float Ranging { get; internal set; }

        /// <summary>Seconds left with no light at all (a badly bent fibre keeps going dark).</summary>
        public float Dark { get; internal set; }

        /// <summary>CRC errors counted on its switch port.</summary>
        public long PortErrors { get; internal set; }

        internal float ErrorCarry { get; set; }

        internal float NextDrop { get; set; }

        public bool HasLight => Patched && Dark <= 0f;

        public bool Up => HasLight && Ranging <= 0f;
    }

    /// <summary>
    /// A green street cabinet carrying fibre broadband to the buildings round it. A power strip
    /// and a UPS keep a fibre switch and an edge router running; each customer's fibre comes off
    /// a switch port through a patch tray, where it can be unplugged and plugged back in. When it
    /// comes back the customer's box takes a few seconds to get in sync before they're online.
    /// Now and then something goes wrong (one thing at a time): the cabinet's screen runs speed
    /// tests to find out what, and naming the fault rightly gets it fixed.
    /// </summary>
    public sealed class StreetCabinet
    {
        /// <summary>How long a customer's box takes to get back in sync once it has light.</summary>
        public const float RangingSeconds = 8f;

        /// <summary>How long, on average, before the next fault turns up.</summary>
        public const float MeanSecondsBetweenFaults = 240f;

        /// <summary>A quiet spell after start-up, and after each fault is fixed, before the next one can turn up.</summary>
        public const float QuietSeconds = 60f;

        /// <summary>The light a healthy line's box gets, in dBm.</summary>
        public const float NormalRxDbm = -9.5f;

        /// <summary>Below this the customer's box can't hold sync.</summary>
        public const float SensitivityDbm = -23f;

        /// <summary>What the light meter reads with no light at all.</summary>
        public const float NoLightDbm = -40f;

        /// <summary>A badly bent fibre goes dark for this long each time it drops.</summary>
        public const float DarkSeconds = 3f;

        private readonly List<FibreLine> _lines = new List<FibreLine>();
        private readonly Random _random;
        private float _quiet = QuietSeconds;

        public StreetCabinet(IEnumerable<(string Customer, float DownMbps, float UpMbps)> customers, int seed = 1)
        {
            _random = new Random(seed);
            foreach (var (customer, down, up) in customers)
            {
                _lines.Add(new FibreLine(customer, _lines.Count + 1, down, up));
            }
        }

        public IReadOnlyList<FibreLine> Lines => _lines;

        /// <summary>What's wrong at the moment, if anything.</summary>
        public LineFault Fault { get; private set; }

        /// <summary>The line it's on, or -1 when it's the whole cabinet (or there's none).</summary>
        public int FaultLine { get; private set; } = -1;

        /// <summary>Faults named rightly (and so fixed).</summary>
        public int Diagnosed { get; private set; }

        /// <summary>Wrong guesses.</summary>
        public int Misdiagnosed { get; private set; }

        /// <summary>The line for <paramref name="customer"/>, or -1.</summary>
        public int IndexOf(string customer) => _lines.FindIndex(line => line.Customer == customer);

        /// <summary>Whether the customer on <paramref name="line"/> is online.</summary>
        public bool InternetUp(int line) => _lines[line].Up;

        /// <summary>The fault affecting <paramref name="line"/> (a congested uplink affects them all).</summary>
        public LineFault FaultOn(int line)
        {
            if (Fault == LineFault.CongestedUplink || (Fault != LineFault.None && FaultLine == line))
            {
                return Fault;
            }

            return LineFault.None;
        }

        /// <summary>Plugs a line in at the patch tray, or pulls it out.</summary>
        public void Patch(int line, bool patched)
        {
            var fibre = _lines[line];
            if (fibre.Patched == patched)
            {
                return;
            }

            fibre.Patched = patched;
            fibre.Dark = 0f;
            fibre.Ranging = patched ? RangingSeconds : 0f;
            fibre.NextDrop = DropInterval();
        }

        /// <summary>Breaks something now (for tests, and for trying the screen out).</summary>
        public void Inject(LineFault fault, int line = 0)
        {
            Fault = fault;
            FaultLine = fault == LineFault.None || fault == LineFault.CongestedUplink ? -1 : line;
            if (fault == LineFault.BentFibre)
            {
                _lines[line].NextDrop = DropInterval();
            }
        }

        /// <summary>
        /// Names what's wrong with <paramref name="line"/>. A right answer gets a fault fixed and
        /// returns true; so does saying there's nothing wrong with a healthy line.
        /// </summary>
        public bool Diagnose(int line, LineFault guess)
        {
            var actual = FaultOn(line);
            if (guess != actual)
            {
                Misdiagnosed++;
                return false;
            }

            if (actual != LineFault.None)
            {
                Diagnosed++;
                Fault = LineFault.None;
                FaultLine = -1;
                _quiet = QuietSeconds;
            }

            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            for (var i = 0; i < _lines.Count; i++)
            {
                TickLine(i, _lines[i], deltaTime);
            }

            if (Fault != LineFault.None)
            {
                return;
            }

            _quiet -= deltaTime;
            if (_quiet <= 0f && _random.NextDouble() < deltaTime / MeanSecondsBetweenFaults)
            {
                BreakSomething();
            }
        }

        /// <summary>
        /// Tests <paramref name="line"/>'s speed from the cabinet: how much light it gets, how fast
        /// it goes each way, how long a ping takes and how much that varies, how many packets go
        /// missing and how many errors its switch port has counted.
        /// </summary>
        public SpeedTestResult RunTest(int line)
        {
            var fibre = _lines[line];
            var fault = FaultOn(line);
            var rx = RxOf(fibre, fault);
            if (!fibre.Up)
            {
                return new SpeedTestResult(false, rx, 0f, 0f, 0f, 0f, 100f, fibre.PortErrors);
            }

            float down, up, ping, jitter, loss;
            switch (fault)
            {
                case LineFault.DirtyConnector:
                    (down, up, ping, jitter, loss) = (0.55f, 0.6f, 5f, 1.5f, Between(1f, 3f));
                    break;
                case LineFault.BentFibre:
                    (down, up, ping, jitter, loss) = (0.12f, 0.15f, 9f, 25f, Between(12f, 25f));
                    break;
                case LineFault.FailingPort:
                    (down, up, ping, jitter, loss) = (0.35f, 0.35f, 5f, 3f, Between(4f, 8f));
                    break;
                case LineFault.CongestedUplink:
                    (down, up, ping, jitter, loss) = (0.25f, 0.7f, Between(100f, 140f), 40f, 0.5f);
                    break;
                default:
                    (down, up, ping, jitter, loss) = (0.94f, 0.94f, 4f, 0.5f, 0f);
                    break;
            }

            return new SpeedTestResult(
                true,
                rx,
                fibre.PlanDownMbps * down * Noise(0.03f),
                fibre.PlanUpMbps * up * Noise(0.03f),
                ping * Noise(0.15f),
                jitter * Noise(0.3f),
                loss,
                fibre.PortErrors);
        }

        /// <summary>The lights on the customer's optical network terminal.</summary>
        public OntLights Ont(int line)
        {
            var fibre = _lines[line];
            var fibreLed = !fibre.HasLight ? Led.Off : (fibre.Ranging > 0f ? Led.GreenBlink : Led.Green);
            return new OntLights(Led.Green, fibreLed, fibre.HasLight ? Led.Off : Led.RedBlink, Led.Green);
        }

        /// <summary>The lights on the customer's router.</summary>
        public RouterLights Router(int line)
        {
            var fibre = _lines[line];
            var internet = !fibre.HasLight ? Led.RedBlink : (fibre.Ranging > 0f ? Led.AmberBlink : Led.Green);
            return new RouterLights(Led.Green, internet, Led.Green);
        }

        private void TickLine(int index, FibreLine fibre, float deltaTime)
        {
            if (!fibre.Patched)
            {
                return;
            }

            if (fibre.Dark > 0f)
            {
                fibre.Dark -= deltaTime;
                if (fibre.Dark <= 0f)
                {
                    fibre.Dark = 0f;
                    fibre.Ranging = RangingSeconds;
                }

                return;
            }

            if (fibre.Ranging > 0f)
            {
                fibre.Ranging = Math.Max(0f, fibre.Ranging - deltaTime);
            }

            var fault = FaultOn(index);
            var errorsPerSecond = fault == LineFault.FailingPort ? 40f : fault == LineFault.BentFibre ? 6f : fault == LineFault.DirtyConnector ? 0.5f : 0f;
            fibre.ErrorCarry += errorsPerSecond * deltaTime;
            var whole = (long)fibre.ErrorCarry;
            fibre.PortErrors += whole;
            fibre.ErrorCarry -= whole;

            // A badly bent fibre keeps losing the light altogether.
            if (fault == LineFault.BentFibre && fibre.Ranging <= 0f)
            {
                fibre.NextDrop -= deltaTime;
                if (fibre.NextDrop <= 0f)
                {
                    fibre.Dark = DarkSeconds;
                    fibre.NextDrop = DropInterval();
                }
            }
        }

        private void BreakSomething()
        {
            var up = new List<int>();
            for (var i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Up)
                {
                    up.Add(i);
                }
            }

            if (up.Count == 0)
            {
                return;
            }

            var fault = (LineFault)(1 + _random.Next(4));
            Inject(fault, up[_random.Next(up.Count)]);
        }

        private float RxOf(FibreLine fibre, LineFault fault)
        {
            if (!fibre.HasLight)
            {
                return NoLightDbm;
            }

            var loss = fault == LineFault.DirtyConnector ? 7f : fault == LineFault.BentFibre ? 12.5f : 0f;
            return NormalRxDbm - loss + Between(-0.4f, 0.4f);
        }

        private float DropInterval() => Between(15f, 25f);

        private float Between(float min, float max) => min + ((float)_random.NextDouble() * (max - min));

        private float Noise(float spread) => 1f + Between(-spread, spread);
    }
}
