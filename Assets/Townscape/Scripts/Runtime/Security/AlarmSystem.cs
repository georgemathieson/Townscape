using System.Collections.Generic;
using Townscape.Generation;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Geometry;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.UI;
using Townscape.Runtime.Walking;
using Townscape.Simulation.Audio;
using Townscape.Simulation.Security;
using UnityEngine;

namespace Townscape.Runtime.Security
{
    /// <summary>
    /// One building's burglar alarm, in play (the Copper Kettle has two: the café's and the
    /// flat's). Its zones watch for the walker: each sensor needs a clear line of sight (walls
    /// and shut doors hide you, and only movement counts) and lights its LED when it sees you,
    /// and each door's contact trips when the door moves. The keypads set and unset it; the
    /// control box opens to show the board the zones are wired to, where wires can be cut and
    /// power pulled. The bell box sounds and its strobe flashes when <see cref="BurglarAlarm"/>
    /// says so.
    /// </summary>
    /// <remarks>
    /// The alarm's state lives here rather than in the store, as a door's open or shut does: it's
    /// something you do inside the town, not a setting.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed partial class AlarmSystem : MonoBehaviour, IScreenWindow
    {
        private const float ChestHeight = 1.2f;
        private const float MovingSpeed = 0.25f;
        private const float LedHoldSeconds = 1.2f;

        private static readonly Color StrobeBlue = new Color(0.25f, 0.45f, 1f);
        private static readonly Color LedRed = new Color(4f, 0.15f, 0.1f);
        private static readonly Color LedGreen = new Color(0.2f, 3f, 0.4f);
        private static readonly Color LedAmber = new Color(3.5f, 1.6f, 0.1f);

        private readonly List<Sensor> _sensors = new List<Sensor>();
        private readonly List<DoorContact> _contacts = new List<DoorContact>();
        private readonly List<Keypad> _keypads = new List<Keypad>();
        private readonly List<Object> _owned = new List<Object>();

        private TownAlarm _spec;
        private BurglarAlarm _alarm;
        private WalkingController _walking;
        private AudioSource _sounder;
        private AudioClip _beep;
        private AlarmGlow _strobe;
        private Light _strobeLight;
        private Vector3 _lastWalker;

        public BurglarAlarm Alarm => _alarm;

        /// <summary>Whose alarm it is: "The Copper Kettle", "The flat".</summary>
        public string Name => _spec?.Name;

        /// <summary>The alarm as generation described it: its zones, keypads, control box and bell box.</summary>
        public TownAlarm Spec => _spec;

        /// <summary>Puts every building's alarm into the town.</summary>
        public static IReadOnlyList<AlarmSystem> CreateAll(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, WalkingController walking, IReadOnlyList<SwingingDoor> doors)
        {
            var systems = new List<AlarmSystem>();
            foreach (var spec in town.Alarms)
            {
                var system = TownMeshSpawner.CreateChild($"Alarm: {spec.Name}", parent, hideFlags).AddComponent<AlarmSystem>();
                system.Initialize(spec, materials, hideFlags, walking, doors);
                systems.Add(system);
            }

            return systems;
        }

        private void Initialize(TownAlarm spec, MaterialLibrary materials, HideFlags hideFlags, WalkingController walking, IReadOnlyList<SwingingDoor> doors)
        {
            _spec = spec;
            _walking = walking;
            _alarm = new BurglarAlarm(spec.Zones.Count);
            _beep = Clip("Alarm beep", AlarmSounds.Beep());
            var led = materials.Get(SurfaceMaterial.AlarmLed);

            // The zones: sensors with their LEDs, and the doors' contacts.
            for (var zone = 0; zone < spec.Zones.Count; zone++)
            {
                var z = spec.Zones[zone];
                if (z.Kind == AlarmZoneKind.Motion)
                {
                    var glow = new AlarmGlow($"{z.Name} LED", transform, hideFlags, led, new Color(0.35f, 0.04f, 0.03f), ToUnity(z.Led), ToUnity(z.Facing), Vector3.one * 0.011f, _owned);
                    _sensors.Add(new Sensor(zone, ToUnity(z.Position), ToUnity(MotionSensor.Facing(z.Facing)), glow));
                    continue;
                }

                foreach (var door in doors ?? new List<SwingingDoor>())
                {
                    if (door != null && door.Door != null && door.Door.Name == z.Door)
                    {
                        _contacts.Add(new DoorContact(zone, door));
                    }
                }
            }

            // The keypads: something to look at and press E on, its buzzer and its two lights.
            for (var i = 0; i < spec.Keypads.Count; i++)
            {
                var mount = spec.Keypads[i];
                var panel = mount.Panel;
                var facing = ToUnity(panel.Facing);
                var keypad = TownMeshSpawner.CreateChild($"Keypad {i + 1}", transform, hideFlags);
                keypad.transform.SetPositionAndRotation(ToUnity(panel.Position), Quaternion.LookRotation(facing, Vector3.up));
                Reachable(keypad, panel, AlarmFittings.KeypadDepth);
                var beeper = keypad.AddComponent<AudioSource>();
                Spatial(beeper, 3f, 35f);
                var component = keypad.AddComponent<AlarmKeypad>();
                component.Initialize(this, beeper);
                _keypads.Add(new Keypad(
                    component,
                    new AlarmGlow("Power light", keypad.transform, hideFlags, led, new Color(0.05f, 0.25f, 0.08f), ToUnity(mount.PowerLed), facing, Vector3.one * 0.009f, _owned),
                    new AlarmGlow("Fault light", keypad.transform, hideFlags, led, new Color(0.3f, 0.17f, 0.03f), ToUnity(mount.FaultLed), facing, Vector3.one * 0.009f, _owned)));
            }

            var box = TownMeshSpawner.CreateChild("Control Box", transform, hideFlags);
            box.transform.SetPositionAndRotation(ToUnity(spec.ControlBox.Position), Quaternion.LookRotation(ToUnity(spec.ControlBox.Facing), Vector3.up));
            Reachable(box, spec.ControlBox, AlarmFittings.ControlBoxDepth);
            box.AddComponent<AlarmControlBox>().Initialize(this);

            // The bell box: its sounder, its strobe, and the blue light the strobe throws.
            var strobe = spec.BellStrobe;
            var bell = TownMeshSpawner.CreateChild("Bell Box", transform, hideFlags);
            bell.transform.position = ToUnity(strobe.Position);
            _sounder = bell.AddComponent<AudioSource>();
            _sounder.clip = Clip("Alarm sounder", AlarmSounds.Sounder());
            _sounder.loop = true;
            Spatial(_sounder, 5f, 160f);
            _strobe = new AlarmGlow("Strobe", bell.transform, hideFlags, materials.Get(SurfaceMaterial.AlarmStrobe), new Color(0.16f, 0.32f, 0.78f), ToUnity(strobe.Position), ToUnity(strobe.Facing), new Vector3(strobe.Width, strobe.Height, 0.004f), _owned);
            _strobeLight = TownMeshSpawner.CreateChild("Strobe Light", bell.transform, hideFlags).AddComponent<Light>();
            _strobeLight.transform.position = ToUnity(strobe.Position + (strobe.Facing * 0.4f));
            _strobeLight.type = LightType.Point;
            _strobeLight.color = StrobeBlue;
            _strobeLight.range = 9f;
            _strobeLight.shadows = LightShadows.None;
            _strobeLight.enabled = false;
        }

        private void Update()
        {
            if (_alarm == null)
            {
                return;
            }

            Watch();
            if (_alarm.Tick(Time.deltaTime) > 0)
            {
                // Every keypad in the building beeps the countdown.
                foreach (var keypad in _keypads)
                {
                    keypad.Component.Beeper.pitch = 1f;
                    keypad.Component.Beeper.PlayOneShot(_beep, 0.8f);
                }
            }

            if (_alarm.BellRinging && !_sounder.isPlaying)
            {
                _sounder.Play();
            }
            else if (!_alarm.BellRinging && _sounder.isPlaying)
            {
                _sounder.Stop();
            }

            // The strobe: one bright flash a second.
            var flash = _alarm.StrobeFlashing && Mathf.Repeat(Time.time, 1f) < 0.07f;
            _strobe.Set(flash ? StrobeBlue.linear * 14f : Color.black);
            _strobeLight.enabled = flash;
            _strobeLight.intensity = flash ? 8f : 0f;

            // The keypads' lights: power (blinking while it runs on its battery) and faults.
            var onBattery = _alarm.Powered && !_alarm.MainsConnected;
            var power = _alarm.Powered && (!onBattery || Mathf.Repeat(Time.time, 1f) < 0.5f);
            var fault = _alarm.Faults != AlarmFaults.None;
            foreach (var keypad in _keypads)
            {
                keypad.Power.Set(power ? LedGreen : Color.black);
                keypad.Fault.Set(fault ? LedAmber : Color.black);
            }

            foreach (var sensor in _sensors)
            {
                sensor.Led.Set(Time.time < sensor.LitUntil ? LedRed : Color.black);
            }

            if (WindowOpen && (_walking == null || !_walking.Active))
            {
                CloseWindows();
            }
        }

        private void OnDestroy()
        {
            if (WindowOpen)
            {
                CloseWindows();
            }

            _skin?.Dispose();
            foreach (var owned in _owned)
            {
                ObjectUtility.Destroy(owned);
            }

            _owned.Clear();
        }

        // What the zones see this frame: the doors that moved, and the sensors with a clear view
        // of the walker moving (if their wire's whole and the panel's on, their LED lights).
        private void Watch()
        {
            foreach (var contact in _contacts)
            {
                var open = contact.Door != null && contact.Door.IsOpen;
                if (open != contact.WasOpen)
                {
                    contact.WasOpen = open;
                    _alarm.Detected(contact.Zone);
                }
            }

            if (_walking == null || !_walking.Active)
            {
                return;
            }

            var feet = _walking.transform.position;
            var moving = Time.deltaTime > 0f && (feet - _lastWalker).magnitude / Time.deltaTime > MovingSpeed;
            _lastWalker = feet;
            if (!moving || !_alarm.Powered)
            {
                return;
            }

            var chest = feet + (Vector3.up * ChestHeight);
            var target = new System.Numerics.Vector3(chest.x, chest.y, chest.z);
            foreach (var sensor in _sensors)
            {
                if (!_alarm.ZoneConnected(sensor.Zone))
                {
                    continue;
                }

                var position = new System.Numerics.Vector3(sensor.Position.x, sensor.Position.y, sensor.Position.z);
                var look = new System.Numerics.Vector3(sensor.Facing.x, sensor.Facing.y, sensor.Facing.z);
                if (!MotionSensor.Covers(position, look, target))
                {
                    continue;
                }

                // The walker is on the Ignore Raycast layer, so only walls and doors get in the way.
                var from = sensor.Position + (new Vector3(sensor.Facing.x, 0f, sensor.Facing.z).normalized * 0.06f);
                if (!Physics.Linecast(from, chest, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    sensor.LitUntil = Time.time + LedHoldSeconds;
                    _alarm.Detected(sensor.Zone);
                }
            }
        }

        // Something to look at and press E on: from just off the wall (not through it, so it can't
        // be reached from the other side) to just proud of the face.
        private static void Reachable(GameObject target, WallMount mount, float depth)
        {
            var box = target.AddComponent<BoxCollider>();
            var back = -depth + 0.004f;
            const float front = 0.03f;
            box.center = new Vector3(0f, 0f, (back + front) * 0.5f);
            box.size = new Vector3(mount.Width + 0.02f, mount.Height + 0.02f, front - back);
        }

        private AudioClip Clip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, ProceduralSounds.SampleRate, false);
            clip.SetData(samples, 0);
            _owned.Add(clip);
            return clip;
        }

        private static void Spatial(AudioSource source, float minDistance, float maxDistance)
        {
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
        }

        private static Vector3 ToUnity(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

        private sealed class Sensor
        {
            public Sensor(int zone, Vector3 position, Vector3 facing, AlarmGlow led)
            {
                Zone = zone;
                Position = position;
                Facing = facing;
                Led = led;
            }

            public int Zone { get; }

            public Vector3 Position { get; }

            public Vector3 Facing { get; }

            public AlarmGlow Led { get; }

            public float LitUntil { get; set; }
        }

        private sealed class DoorContact
        {
            public DoorContact(int zone, SwingingDoor door)
            {
                Zone = zone;
                Door = door;
            }

            public int Zone { get; }

            public SwingingDoor Door { get; }

            public bool WasOpen { get; set; }
        }

        private sealed class Keypad
        {
            public Keypad(AlarmKeypad component, AlarmGlow power, AlarmGlow fault)
            {
                Component = component;
                Power = power;
                Fault = fault;
            }

            public AlarmKeypad Component { get; }

            public AlarmGlow Power { get; }

            public AlarmGlow Fault { get; }
        }
    }
}
