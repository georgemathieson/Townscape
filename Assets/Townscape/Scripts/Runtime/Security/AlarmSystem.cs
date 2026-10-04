using System.Collections.Generic;
using Townscape.Generation;
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
    /// The Copper Kettle's burglar alarm, in play. The sensors watch for the walker (each needs a
    /// clear line of sight, so walls and shut doors hide you) and light their LEDs when they see
    /// you move; opening a door counts too. The keypad on the hall wall sets and unsets it with
    /// the code; the panel beeps through the 30 second exit and entry times, and if the code
    /// doesn't go in, the bell box's sounder wails and its blue strobe flashes.
    /// </summary>
    /// <remarks>
    /// The alarm's state lives here rather than in the store, as a door's open or shut does: it's
    /// something you do inside the town, not a setting.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AlarmSystem : MonoBehaviour
    {
        private const float ChestHeight = 1.2f;
        private const float MovingSpeed = 0.25f;
        private const float LedHoldSeconds = 1.2f;
        private const float MessageSeconds = 1.6f;
        private const int MaxDigits = 8;

        private static readonly Color StrobeBlue = new Color(0.25f, 0.45f, 1f);

        private readonly BurglarAlarm _alarm = new BurglarAlarm();
        private readonly List<(Vector3 Position, Vector3 Facing)> _sensors = new List<(Vector3, Vector3)>();
        private readonly List<Object> _owned = new List<Object>();

        private MaterialLibrary _materials;
        private WalkingController _walking;
        private IReadOnlyList<SwingingDoor> _doors;
        private bool[] _doorWasOpen;
        private AudioSource _bell;
        private AudioSource _beeper;
        private AudioClip _beep;
        private Light _strobe;
        private Vector3 _lastWalker;
        private float _ledUntil;
        private string _typed = string.Empty;
        private string _message;
        private float _messageUntil;
        private float _openedAt;
        private PanelSkin _skin;
        private Texture2D _white;
        private GUIStyle _lcd;
        private GUIStyle _lcdSmall;

        /// <summary>True while the keypad is up on screen.</summary>
        public bool PanelOpen { get; private set; }

        public BurglarAlarm Alarm => _alarm;

        /// <summary>Puts the alarm into the town if a building has its fittings; otherwise returns null.</summary>
        public static AlarmSystem Create(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, WalkingController walking, IReadOnlyList<SwingingDoor> doors)
        {
            TownAnchor? keypad = null;
            TownAnchor? bell = null;
            foreach (var anchor in town.Anchors)
            {
                if (anchor.Kind == AnchorKind.AlarmKeypad)
                {
                    keypad = anchor;
                }
                else if (anchor.Kind == AnchorKind.AlarmBell)
                {
                    bell = anchor;
                }
            }

            if (keypad == null || bell == null)
            {
                return null;
            }

            var system = TownMeshSpawner.CreateChild("Burglar Alarm", parent, hideFlags).AddComponent<AlarmSystem>();
            system.Initialize(town, materials, hideFlags, walking, doors, keypad.Value, bell.Value);
            return system;
        }

        /// <summary>Brings up the keypad and frees the pointer to use it.</summary>
        public void OpenPanel()
        {
            PanelOpen = true;
            _openedAt = Time.unscaledTime;
            _typed = string.Empty;
            if (_walking != null)
            {
                _walking.Paused = true;
            }
        }

        public void ClosePanel()
        {
            PanelOpen = false;
            _typed = string.Empty;
            if (_walking != null)
            {
                _walking.Paused = false;
            }
        }

        private void Initialize(GeneratedTown town, MaterialLibrary materials, HideFlags hideFlags, WalkingController walking, IReadOnlyList<SwingingDoor> doors, TownAnchor keypad, TownAnchor bell)
        {
            _materials = materials;
            _walking = walking;
            _doors = doors ?? new List<SwingingDoor>();
            _doorWasOpen = new bool[_doors.Count];
            foreach (var anchor in town.Anchors)
            {
                if (anchor.Kind == AnchorKind.AlarmSensor)
                {
                    _sensors.Add((ToUnity(anchor.Position), ToUnity(MotionSensor.Facing(anchor.Facing))));
                }
            }

            materials.EnableEmission(SurfaceMaterial.AlarmLed);
            materials.EnableEmission(SurfaceMaterial.AlarmStrobe);

            // Something to look at and press E on, just proud of the panel.
            var panel = TownMeshSpawner.CreateChild("Keypad", transform, hideFlags);
            var outOfWall = ToUnity(keypad.Facing);
            panel.transform.SetPositionAndRotation(ToUnity(keypad.Position), Quaternion.LookRotation(outOfWall, Vector3.up));
            var box = panel.AddComponent<BoxCollider>();
            box.size = new Vector3(keypad.Size + 0.02f, 0.24f, 0.06f);
            panel.AddComponent<AlarmKeypad>().Initialize(this);

            _beep = Clip("Alarm beep", AlarmSounds.Beep());
            _beeper = panel.AddComponent<AudioSource>();
            Spatial(_beeper, 3f, 35f);

            var bellBox = TownMeshSpawner.CreateChild("Bell Box", transform, hideFlags);
            bellBox.transform.position = ToUnity(bell.Position);
            _bell = bellBox.AddComponent<AudioSource>();
            _bell.clip = Clip("Alarm sounder", AlarmSounds.Sounder());
            _bell.loop = true;
            Spatial(_bell, 5f, 160f);

            _strobe = TownMeshSpawner.CreateChild("Strobe", bellBox.transform, hideFlags).AddComponent<Light>();
            _strobe.transform.position = ToUnity(bell.Position + (bell.Facing * 0.4f));
            _strobe.type = LightType.Point;
            _strobe.color = StrobeBlue;
            _strobe.range = 9f;
            _strobe.intensity = 0f;
            _strobe.shadows = LightShadows.None;
            _strobe.enabled = false;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (Sees() || DoorMoved())
            {
                _alarm.Detected();
            }

            if (_alarm.Tick(deltaTime) > 0)
            {
                _beeper.pitch = 1f;
                _beeper.PlayOneShot(_beep, 0.8f);
            }

            if (_alarm.BellRinging && !_bell.isPlaying)
            {
                _bell.Play();
            }
            else if (!_alarm.BellRinging && _bell.isPlaying)
            {
                _bell.Stop();
            }

            // The strobe: two quick flashes a second.
            var phase = Mathf.Repeat(Time.time, 1f);
            var flash = _alarm.StrobeFlashing && (phase < 0.05f || (phase > 0.13f && phase < 0.18f));
            _materials.SetEmission(SurfaceMaterial.AlarmStrobe, flash ? StrobeBlue.linear * 12f : Color.black);
            _strobe.enabled = flash;
            _strobe.intensity = flash ? 8f : 0f;

            _materials.SetEmission(SurfaceMaterial.AlarmLed, Time.time < _ledUntil ? new Color(4f, 0.15f, 0.1f) : Color.black);

            if (PanelOpen && (_walking == null || !_walking.Active))
            {
                ClosePanel();
            }
        }

        private void OnDestroy()
        {
            if (PanelOpen)
            {
                ClosePanel();
            }

            _skin?.Dispose();
            foreach (var owned in _owned)
            {
                ObjectUtility.Destroy(owned);
            }

            _owned.Clear();
        }

        // Whether any sensor sees the walker moving, with nothing solid in between.
        private bool Sees()
        {
            if (_walking == null || !_walking.Active)
            {
                return false;
            }

            var feet = _walking.transform.position;
            var moving = Time.deltaTime > 0f && (feet - _lastWalker).magnitude / Time.deltaTime > MovingSpeed;
            _lastWalker = feet;
            if (!moving)
            {
                return false;
            }

            var chest = feet + (Vector3.up * ChestHeight);
            foreach (var (position, facing) in _sensors)
            {
                var sensor = new System.Numerics.Vector3(position.x, position.y, position.z);
                var target = new System.Numerics.Vector3(chest.x, chest.y, chest.z);
                var look = new System.Numerics.Vector3(facing.x, facing.y, facing.z);
                if (!MotionSensor.Covers(sensor, look, target))
                {
                    continue;
                }

                // The walker is on the Ignore Raycast layer, so only walls and doors get in the way.
                var from = position + (new Vector3(facing.x, 0f, facing.z).normalized * 0.06f);
                if (!Physics.Linecast(from, chest, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    _ledUntil = Time.time + LedHoldSeconds;
                    return true;
                }
            }

            return false;
        }

        // Opening or shutting any door counts as being seen.
        private bool DoorMoved()
        {
            var moved = false;
            for (var i = 0; i < _doors.Count; i++)
            {
                var open = _doors[i] != null && _doors[i].IsOpen;
                moved |= open != _doorWasOpen[i];
                _doorWasOpen[i] = open;
            }

            return moved;
        }

        private void OnGUI()
        {
            if (!PanelOpen)
            {
                return;
            }

            CreateStyles();
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            HandleKeys();
            if (!PanelOpen)
            {
                return;
            }

            const float width = 250f;
            const float height = 360f;
            var area = new Rect((Screen.width / scale * 0.5f) + 60f, (Screen.height / scale * 0.5f) - (height * 0.5f), width, height);
            GUILayout.BeginArea(area, _skin.Panel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Alarm", _skin.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", _skin.Button, GUILayout.Width(32f)))
            {
                ClosePanel();
            }

            GUILayout.EndHorizontal();

            // The screen: the state, then the code as you type it (or what the panel has to say).
            var screen = GUILayoutUtility.GetRect(width - 32f, 62f);
            var previous = GUI.color;
            GUI.color = _alarm.StrobeFlashing ? new Color(0.45f, 0.12f, 0.1f) : new Color(0.16f, 0.27f, 0.25f);
            GUI.DrawTexture(screen, _white);
            GUI.color = previous;
            GUI.Label(new Rect(screen.x + 10f, screen.y + 6f, screen.width - 20f, 26f), Headline(), _lcd);
            var detail = Time.unscaledTime < _messageUntil ? _message : (_typed.Length > 0 ? new string('*', _typed.Length) : Hint());
            GUI.Label(new Rect(screen.x + 10f, screen.y + 34f, screen.width - 20f, 22f), detail, _lcdSmall);

            GUILayout.Space(8f);
            for (var row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                for (var column = 1; column <= 3; column++)
                {
                    Digit((char)('0' + (row * 3) + column));
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", _skin.Button))
            {
                Press();
                _typed = string.Empty;
            }

            Digit('0');
            if (GUILayout.Button("Del", _skin.Button) && _typed.Length > 0)
            {
                Press();
                _typed = _typed.Substring(0, _typed.Length - 1);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set", _skin.Button))
            {
                Submit(set: true);
            }

            if (GUILayout.Button("Unset", _skin.Button))
            {
                Submit(set: false);
            }

            GUILayout.EndHorizontal();
            GUILayout.Label($"The code is {BurglarAlarm.DefaultCode}. Type it, then Set or Unset (Enter does whichever fits). Esc closes.", _skin.Status);
            GUILayout.EndArea();
        }

        private void Digit(char digit)
        {
            if (GUILayout.Button(digit.ToString(), _skin.Button))
            {
                Type(digit);
            }
        }

        private void Type(char digit)
        {
            Press();
            if (_typed.Length < MaxDigits)
            {
                _typed += digit;
            }
        }

        // Enter sets the alarm when it's off and unsets it otherwise.
        private void Submit(bool set)
        {
            var code = _typed;
            _typed = string.Empty;
            var result = set ? _alarm.Set(code) : _alarm.Unset(code);
            switch (result)
            {
                case KeypadResult.WrongCode:
                    Say("WRONG CODE");
                    _beeper.pitch = 0.6f;
                    _beeper.PlayOneShot(_beep, 1f);
                    break;
                case KeypadResult.NothingToDo:
                    Say(set ? "ALREADY SET" : "ALREADY UNSET");
                    break;
                default:
                    Say(set ? "SETTING" : "UNSET");
                    _beeper.pitch = 1f;
                    _beeper.PlayOneShot(_beep, 1f);
                    break;
            }
        }

        private void HandleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown)
            {
                return;
            }

            var key = e.keyCode;
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            {
                Type((char)('0' + (key - KeyCode.Alpha0)));
            }
            else if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
            {
                Type((char)('0' + (key - KeyCode.Keypad0)));
            }
            else if (key == KeyCode.Backspace && _typed.Length > 0)
            {
                _typed = _typed.Substring(0, _typed.Length - 1);
            }
            else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            {
                Submit(set: !_alarm.Armed);
            }
            else if (key == KeyCode.Escape || (key == KeyCode.E && Time.unscaledTime - _openedAt > 0.3f))
            {
                ClosePanel();
            }
            else
            {
                return;
            }

            e.Use();
        }

        private string Headline()
        {
            switch (_alarm.State)
            {
                case AlarmState.Exiting: return $"EXIT   {Mathf.CeilToInt(_alarm.Remaining)}";
                case AlarmState.Set: return "SET";
                case AlarmState.Entry: return $"ENTRY  {Mathf.CeilToInt(_alarm.Remaining)}";
                case AlarmState.Sounding: return "ALARM";
                default: return "UNSET";
            }
        }

        private string Hint()
        {
            switch (_alarm.State)
            {
                case AlarmState.Exiting: return "Leave now";
                case AlarmState.Set: return "Code then Unset";
                case AlarmState.Entry: return "Code then Unset";
                case AlarmState.Sounding: return "Code to silence";
                default: return "Code then Set";
            }
        }

        private void Say(string message)
        {
            _message = message;
            _messageUntil = Time.unscaledTime + MessageSeconds;
        }

        // A key's click.
        private void Press()
        {
            _beeper.pitch = 0.85f;
            _beeper.PlayOneShot(_beep, 0.35f);
        }

        private void CreateStyles()
        {
            if (_skin != null)
            {
                return;
            }

            _skin = new PanelSkin();
            _white = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
            _owned.Add(_white);
            var glow = new Color(0.75f, 1f, 0.85f);
            _lcd = new GUIStyle { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = glow } };
            _lcdSmall = new GUIStyle { fontSize = 15, normal = { textColor = new Color(glow.r, glow.g, glow.b, 0.8f) } };
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
    }
}
