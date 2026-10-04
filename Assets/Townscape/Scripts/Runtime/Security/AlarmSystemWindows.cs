using System;
using Townscape.Generation;
using Townscape.Runtime.UI;
using Townscape.Simulation.Security;
using UnityEngine;

namespace Townscape.Runtime.Security
{
    /// <summary>
    /// What an alarm puts on screen: its keypad (a little screen, number keys, Set, Unset and Eng)
    /// and, with the control box's lid off, the inside of the box: the board with its
    /// microcontroller and lights, a wire to each zone, the mains supply, the standby battery, the
    /// network cable, and the bell box's wires through their fuse. Both pause the walker and free
    /// the mouse while they're up.
    /// </summary>
    public sealed partial class AlarmSystem
    {
        private const int MaxDigits = 8;
        private const float MessageSeconds = 1.6f;

        private static readonly Color[] ZoneWires =
        {
            new Color(0.95f, 0.95f, 0.92f), new Color(0.95f, 0.8f, 0.15f), new Color(0.3f, 0.6f, 0.95f), new Color(0.6f, 0.35f, 0.85f),
            new Color(0.95f, 0.5f, 0.15f), new Color(0.4f, 0.8f, 0.4f), new Color(0.9f, 0.45f, 0.6f), new Color(0.6f, 0.6f, 0.6f),
        };

        private PanelSkin _skin;
        private Texture2D _white;
        private GUIStyle _lcd;
        private GUIStyle _lcdSmall;
        private GUIStyle _tiny;
        private GUIStyle _tinyLight;
        private GUIStyle _small;
        private GUIStyle _smallButton;
        private bool _keypadOpen;
        private bool _boxOpen;
        private bool _confirmTamper;
        private AudioSource _beeper;
        private string _typed = string.Empty;
        private string _message;
        private float _messageUntil;
        private float _openedAt;

        /// <summary>True while its keypad or control box is up on screen.</summary>
        public bool WindowOpen => _keypadOpen || _boxOpen;

        /// <summary>Brings up the keypad; its beeps come from the keypad you used.</summary>
        public void OpenKeypad(AlarmKeypad at)
        {
            _beeper = at.Beeper;
            _keypadOpen = true;
            Opened();
        }

        /// <summary>
        /// Takes the control box's lid off. With power on and no engineer code put in, it warns you
        /// first that the tamper switch will set the alarm off.
        /// </summary>
        public void OpenControlBox()
        {
            _boxOpen = true;
            _confirmTamper = _alarm.Powered && !_alarm.EngineerMode;
            if (!_confirmTamper)
            {
                _alarm.OpenLid();
            }

            Opened();
        }

        private void Opened()
        {
            _openedAt = Time.unscaledTime;
            _typed = string.Empty;
            if (_walking != null)
            {
                _walking.Paused = true;
            }
        }

        private void CloseWindows()
        {
            if (_boxOpen && _alarm.LidOpen)
            {
                _alarm.CloseLid();
            }

            _keypadOpen = false;
            _boxOpen = false;
            _confirmTamper = false;
            _typed = string.Empty;
            if (_walking != null)
            {
                _walking.Paused = false;
            }
        }

        private void OnGUI()
        {
            if (!WindowOpen)
            {
                return;
            }

            CreateStyles();
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            HandleKeys();
            if (!WindowOpen)
            {
                return;
            }

            var width = Screen.width / scale;
            var height = Screen.height / scale;
            if (_keypadOpen)
            {
                DrawKeypad(new Rect((width * 0.5f) + 60f, (height * 0.5f) - 185f, 260f, 370f));
            }
            else if (_confirmTamper)
            {
                DrawTamperWarning(new Rect((width * 0.5f) - 200f, (height * 0.5f) - 110f, 400f, 220f));
            }
            else
            {
                DrawBox(new Rect((width * 0.5f) - 370f, (height * 0.5f) - 300f, 740f, 600f));
            }
        }

        // ---- The keypad ---------------------------------------------------------------------

        private void DrawKeypad(Rect area)
        {
            GUILayout.BeginArea(area, _skin.Panel);
            Title($"Alarm: {_spec.Name}");

            // The screen: blank with no power; otherwise the state, then the code as you type it,
            // what the panel has to say, or its faults.
            var screen = GUILayoutUtility.GetRect(area.width - 32f, 62f);
            var powered = _alarm.Powered;
            Fill(screen, !powered ? new Color(0.06f, 0.07f, 0.07f) : _alarm.StrobeFlashing || _alarm.Tampered ? new Color(0.45f, 0.12f, 0.1f) : new Color(0.16f, 0.27f, 0.25f));
            if (powered)
            {
                var faults = _alarm.Faults;
                var detail = Time.unscaledTime < _messageUntil ? _message
                    : _typed.Length > 0 ? new string('*', _typed.Length)
                    : faults != AlarmFaults.None ? FaultText(faults)
                    : Hint();
                GUI.Label(new Rect(screen.x + 10f, screen.y + 6f, screen.width - 20f, 26f), Headline(), _lcd);
                GUI.Label(new Rect(screen.x + 10f, screen.y + 34f, screen.width - 20f, 22f), detail, _lcdSmall);
            }

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
                Submit(Action.Set);
            }

            if (GUILayout.Button("Unset", _skin.Button))
            {
                Submit(Action.Unset);
            }

            if (GUILayout.Button("Eng", _skin.Button))
            {
                Submit(Action.Engineer);
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(
                $"The code is {BurglarAlarm.DefaultCode}; the engineer's is {BurglarAlarm.DefaultEngineerCode}. Type one, then Set, Unset or Eng. Esc closes.",
                _skin.Status);
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

        private enum Action
        {
            Set,
            Unset,
            Engineer,
        }

        private void Submit(Action action)
        {
            var code = _typed;
            _typed = string.Empty;
            var wasEngineer = _alarm.EngineerMode;
            var result = action == Action.Set ? _alarm.Set(code) : action == Action.Unset ? _alarm.Unset(code) : _alarm.Engineer(code);
            switch (result)
            {
                case KeypadResult.NoPower:
                    return;
                case KeypadResult.WrongCode:
                    Say("WRONG CODE");
                    Beep(0.6f);
                    return;
                case KeypadResult.NothingToDo:
                    Say(action == Action.Set ? "ALREADY SET" : action == Action.Unset ? "ALREADY UNSET" : "UNSET IT FIRST");
                    return;
                case KeypadResult.CloseTheLid:
                    Say("CLOSE THE LID");
                    Beep(0.6f);
                    return;
                case KeypadResult.EngineerMode:
                    Say("ENGINEER MODE");
                    Beep(0.6f);
                    return;
                default:
                    Say(action == Action.Set ? "SETTING" : action == Action.Unset ? "UNSET" : wasEngineer ? "ENGINEER OUT" : "ENGINEER IN");
                    Beep(1f);
                    return;
            }
        }

        private string Headline()
        {
            if (_alarm.EngineerMode)
            {
                return "ENGINEER";
            }

            switch (_alarm.State)
            {
                case AlarmState.Exiting: return $"EXIT   {Mathf.CeilToInt(_alarm.Remaining)}";
                case AlarmState.Set: return "SET";
                case AlarmState.Entry: return $"ENTRY  {Mathf.CeilToInt(_alarm.Remaining)}";
                case AlarmState.Sounding: return _alarm.Tampered ? "TAMPER" : "ALARM";
                default: return "UNSET";
            }
        }

        private string Hint()
        {
            if (_alarm.EngineerMode)
            {
                return _alarm.LidOpen ? "Lid off" : "Eng code to leave";
            }

            switch (_alarm.State)
            {
                case AlarmState.Exiting: return "Leave now";
                case AlarmState.Set: return "Code then Unset";
                case AlarmState.Entry: return "Code then Unset";
                case AlarmState.Sounding: return "Code to silence";
                default: return "Code then Set";
            }
        }

        private static string FaultText(AlarmFaults faults)
        {
            var text = "FAULT:";
            text += (faults & AlarmFaults.Mains) != 0 ? " MAINS" : string.Empty;
            text += (faults & AlarmFaults.Battery) != 0 ? " BATT" : string.Empty;
            text += (faults & AlarmFaults.Comms) != 0 ? " COMMS" : string.Empty;
            text += (faults & AlarmFaults.Bell) != 0 ? " BELL" : string.Empty;
            return text;
        }

        private void Say(string message)
        {
            _message = message;
            _messageUntil = Time.unscaledTime + MessageSeconds;
        }

        // A key's click, and the panel's longer beeps: only with power.
        private void Press() => Beep(0.85f, 0.35f);

        private void Beep(float pitch, float volume = 1f)
        {
            if (_beeper == null || !_alarm.Powered)
            {
                return;
            }

            _beeper.pitch = pitch;
            _beeper.PlayOneShot(_beep, volume);
        }

        private void HandleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown)
            {
                return;
            }

            var key = e.keyCode;
            if (key == KeyCode.Escape || (key == KeyCode.E && Time.unscaledTime - _openedAt > 0.3f))
            {
                CloseWindows();
            }
            else if (!_keypadOpen)
            {
                return;
            }
            else if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
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
                Submit(_alarm.Armed ? Action.Unset : Action.Set);
            }
            else
            {
                return;
            }

            e.Use();
        }

        // ---- The control box ----------------------------------------------------------------

        private void DrawTamperWarning(Rect area)
        {
            GUILayout.BeginArea(area, _skin.Panel);
            Title($"Alarm control box: {_spec.Name}");
            GUILayout.Label(
                "The lid has a tamper switch. Unless the engineer code has been put into a keypad (and Eng pressed), taking it off sets the alarm off, set or not.",
                _skin.Status);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take the lid off anyway", _skin.Button))
            {
                _confirmTamper = false;
                _alarm.OpenLid();
            }

            if (GUILayout.Button("Leave it", _skin.Button))
            {
                CloseWindows();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawBox(Rect area)
        {
            GUILayout.BeginArea(area, _skin.Panel);
            Title($"Alarm control box: {_spec.Name}");
            GUILayout.EndArea();

            DrawBoard(new Rect(area.x + 16f, area.y + 50f, 420f, area.height - 66f));

            GUILayout.BeginArea(new Rect(area.x + 452f, area.y + 46f, area.width - 468f, area.height - 60f));
            Status();
            GUILayout.Label("ZONES", _skin.Heading);
            for (var zone = 0; zone < _spec.Zones.Count; zone++)
            {
                var connected = _alarm.ZoneConnected(zone);
                if (Row($"Z{zone + 1}  {_spec.Zones[zone].Name}", connected ? "Cut" : "Rejoin"))
                {
                    _alarm.ConnectZone(zone, !connected);
                }
            }

            GUILayout.Label("POWER AND NETWORK", _skin.Heading);
            if (Row(_alarm.MainsConnected ? "Mains: on" : "Mains: off", _alarm.MainsConnected ? "Switch off" : "Switch on"))
            {
                _alarm.ConnectMains(!_alarm.MainsConnected);
            }

            if (Row(_alarm.BatteryConnected ? "Battery: connected" : "Battery: off", _alarm.BatteryConnected ? "Disconnect" : "Connect"))
            {
                _alarm.ConnectBattery(!_alarm.BatteryConnected);
            }

            if (Row(_alarm.EthernetConnected ? "Network: plugged in" : "Network: unplugged", _alarm.EthernetConnected ? "Unplug" : "Plug in"))
            {
                _alarm.ConnectEthernet(!_alarm.EthernetConnected);
            }

            GUILayout.Label("BELL BOX", _skin.Heading);
            if (Row(_alarm.BellReversed ? "Power: + and - swapped" : "Power: wired right", _alarm.BellReversed ? "Wire it right" : "Swap + and -"))
            {
                _alarm.ReverseBell(!_alarm.BellReversed);
            }

            GUI.enabled = _alarm.FuseBlown;
            if (Row(_alarm.FuseBlown ? "Fuse F1: blown" : "Fuse F1: good", "New fuse"))
            {
                _alarm.FitNewFuse();
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Put the lid back on", _skin.Button))
            {
                CloseWindows();
            }

            GUILayout.EndArea();
        }

        // What the panel's doing, in a few words.
        private void Status()
        {
            string power;
            if (_alarm.MainsConnected)
            {
                power = "Running on the mains.";
            }
            else if (_alarm.Powered)
            {
                var left = TimeSpan.FromSeconds(_alarm.BatteryLeft);
                power = $"Running on its battery: {left.Minutes}:{left.Seconds:00} left.";
            }
            else
            {
                power = "Dead: no mains and no battery.";
            }

            var state = _alarm.EngineerMode ? "Engineer mode: the lid can come off safely."
                : _alarm.Tampered ? "Tamper! The alarm's going off."
                : string.Empty;
            var bell = _alarm.SelfActivating > 0f ? $" The bell box is sounding on its own battery ({Mathf.CeilToInt(_alarm.SelfActivating)} s)." : string.Empty;
            GUILayout.Label($"{power} {state}{bell}", _small);
        }

        private bool Row(string label, string button)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _small, GUILayout.Width(170f));
            var pressed = GUILayout.Button(button, _smallButton, GUILayout.Width(90f));
            GUILayout.EndHorizontal();
            return pressed;
        }

        // The inside of the box, drawn: the board, its chip, lights and terminals, and the wires.
        private void DrawBoard(Rect r)
        {
            var o = new Vector2(r.x, r.y);
            Rect At(float x, float y, float w, float h) => new Rect(o.x + x, o.y + y, w, h);
            void Box(float x, float y, float w, float h, Color colour) => Fill(At(x, y, w, h), colour);
            void Text(float x, float y, string text, GUIStyle style = null) => GUI.Label(At(x, y, 160f, 16f), text, style ?? _tiny);
            void Wire(Color colour, float thick, params Vector2[] points)
            {
                for (var i = 1; i < points.Length; i++)
                {
                    var a = points[i - 1];
                    var b = points[i];
                    var x0 = Mathf.Min(a.x, b.x) - (thick * 0.5f);
                    var y0 = Mathf.Min(a.y, b.y) - (thick * 0.5f);
                    Box(x0, y0, Mathf.Abs(b.x - a.x) + thick, Mathf.Abs(b.y - a.y) + thick, colour);
                }
            }

            var red = new Color(0.85f, 0.15f, 0.12f);
            var black = new Color(0.08f, 0.08f, 0.08f);
            var yellow = new Color(0.95f, 0.82f, 0.15f);
            var copper = new Color(0.8f, 0.55f, 0.3f);
            var powered = _alarm.Powered;

            // The box's insides and the board.
            Fill(r, new Color(0.86f, 0.86f, 0.83f));
            Box(150f, 0f, 120f, 10f, new Color(0.55f, 0.55f, 0.55f));
            Text(160f, 10f, "cables in");
            const float px = 70f;
            const float py = 70f;
            const float pw = 290f;
            const float ph = 200f;
            Box(px, py, pw, ph, new Color(0.1f, 0.36f, 0.2f));
            for (var t = 0; t < 6; t++)
            {
                Box(px + 30f + (t * 40f), py + 30f, 2f, 140f, new Color(0.18f, 0.48f, 0.3f));
            }

            // The microcontroller, with its legs, and the board's lights.
            Box(180f, 140f, 70f, 70f, black);
            for (var leg = 0; leg < 7; leg++)
            {
                Box(184f + (leg * 9f), 134f, 4f, 6f, new Color(0.75f, 0.75f, 0.75f));
                Box(184f + (leg * 9f), 210f, 4f, 6f, new Color(0.75f, 0.75f, 0.75f));
            }

            Text(198f, 166f, "MCU", _small);
            var blink = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
            Light(92f, 236f, "PWR", powered ? new Color(0.2f, 0.95f, 0.3f) : Color.gray * 0.4f);
            Light(132f, 236f, "FLT", powered && _alarm.Faults != AlarmFaults.None ? new Color(1f, 0.65f, 0.1f) : Color.gray * 0.4f);
            Light(172f, 236f, "NET", powered && _alarm.EthernetConnected && blink ? new Color(0.3f, 0.6f, 1f) : Color.gray * 0.4f);
            Box(318f, 86f, 26f, 14f, black);
            Text(310f, 100f, _alarm.LidOpen ? "tamper: open" : "tamper", _tinyLight);

            // A terminal and wire for each zone, in from the top; a cut wire hangs short.
            var zones = _spec.Zones.Count;
            var step = 270f / Mathf.Max(1, zones);
            for (var zone = 0; zone < zones; zone++)
            {
                var x = px + 10f + (step * zone) + (step * 0.5f);
                var connected = _alarm.ZoneConnected(zone);
                var colour = ZoneWires[zone % ZoneWires.Length];
                Box(x - 9f, py + 4f, 18f, 14f, new Color(0.2f, 0.45f, 0.3f));
                Box(x - 6f, py + 8f, 4f, 4f, new Color(0.8f, 0.8f, 0.8f));
                Box(x + 2f, py + 8f, 4f, 4f, new Color(0.8f, 0.8f, 0.8f));
                Text(x - 8f, py + 20f, $"Z{zone + 1}", _tinyLight);
                Wire(colour, 3f, new Vector2(x, 10f), new Vector2(x, connected ? py + 8f : py - 26f));
                if (!connected)
                {
                    Box(x - 2.5f, py - 28f, 5f, 3f, copper);
                }
            }

            // The bell box's wires, in from the left, through fuse F1: + red, - black, trigger yellow.
            Box(px, 150f, 18f, 48f, new Color(0.2f, 0.45f, 0.3f));
            Text(px + 20f, 151f, "+", _tinyLight);
            Text(px + 20f, 165f, "-", _tinyLight);
            Text(px + 20f, 179f, "TR", _tinyLight);
            Text(4f, 132f, "to the bell box");
            var plus = new Vector2(px + 4f, 158f);
            var minus = new Vector2(px + 4f, 172f);
            if (_alarm.BellReversed)
            {
                Wire(red, 3f, new Vector2(0f, 158f), new Vector2(40f, 158f), new Vector2(40f, 172f), plus + new Vector2(0f, 14f));
                Wire(black, 3f, new Vector2(0f, 172f), new Vector2(30f, 172f), new Vector2(30f, 158f), plus);
            }
            else
            {
                Wire(red, 3f, new Vector2(0f, 158f), plus);
                Wire(black, 3f, new Vector2(0f, 172f), minus);
            }

            Wire(yellow, 3f, new Vector2(0f, 186f), new Vector2(px + 4f, 186f));
            Box(100f, 214f, 34f, 12f, _alarm.FuseBlown ? new Color(0.2f, 0.18f, 0.16f) : new Color(0.85f, 0.88f, 0.9f));
            Box(102f, 219f, 30f, 2f, _alarm.FuseBlown ? black : new Color(0.6f, 0.6f, 0.6f));
            Text(98f, 228f, _alarm.FuseBlown ? "F1 BLOWN" : "F1 bell", _tinyLight);

            // The network: a blue cable from the board's socket out of the right side.
            Box(px + pw - 16f, 150f, 24f, 22f, new Color(0.75f, 0.75f, 0.78f));
            var network = new Color(0.2f, 0.4f, 0.9f);
            if (_alarm.EthernetConnected)
            {
                Wire(network, 5f, new Vector2(px + pw + 8f, 161f), new Vector2(r.width, 161f));
            }
            else
            {
                Box(px + pw + 26f, 155f, 14f, 12f, new Color(0.85f, 0.85f, 0.88f));
                Wire(network, 5f, new Vector2(px + pw + 40f, 161f), new Vector2(r.width, 161f));
            }

            Text(px + pw + 8f, 170f, "to the internet");

            // The mains: a supply block, its leads to the board, and its cable out of the bottom.
            Box(20f, 330f, 110f, 70f, new Color(0.55f, 0.56f, 0.58f));
            Text(30f, 336f, "PSU 230V~", _small);
            Box(112f, 336f, 8f, 8f, _alarm.MainsConnected ? new Color(0.2f, 0.95f, 0.3f) : Color.gray * 0.4f);
            Wire(red, 2f, new Vector2(80f, 330f), new Vector2(80f, py + ph - 2f));
            Wire(black, 2f, new Vector2(90f, 330f), new Vector2(90f, py + ph - 2f));
            Text(76f, py + ph - 16f, "AC", _tinyLight);
            var brown = new Color(0.45f, 0.28f, 0.12f);
            var blue = new Color(0.2f, 0.35f, 0.75f);
            var bottom = r.height;
            var mainsFrom = _alarm.MainsConnected ? 400f : bottom - 14f;
            Wire(brown, 3f, new Vector2(60f, mainsFrom), new Vector2(60f, bottom));
            Wire(blue, 3f, new Vector2(70f, mainsFrom), new Vector2(70f, bottom));
            Text(80f, bottom - 16f, _alarm.MainsConnected ? "mains in" : "mains: unplugged");

            // The standby battery, with its leads up to the board.
            Box(270f, 340f, 120f, 70f, black);
            Text(292f, 362f, "12V 7Ah", _small);
            Box(286f, 334f, 10f, 6f, red);
            Box(366f, 334f, 10f, 6f, new Color(0.3f, 0.3f, 0.3f));
            Text(300f, py + ph - 16f, "BATT", _tinyLight);
            if (_alarm.BatteryConnected)
            {
                Wire(red, 2f, new Vector2(291f, 334f), new Vector2(291f, py + ph - 2f));
                Wire(black, 2f, new Vector2(371f, 334f), new Vector2(371f, 300f), new Vector2(331f, 300f), new Vector2(331f, py + ph - 2f));
            }
            else
            {
                Wire(red, 2f, new Vector2(291f, py + ph - 2f), new Vector2(291f, 306f));
                Wire(black, 2f, new Vector2(331f, py + ph - 2f), new Vector2(331f, 300f), new Vector2(351f, 300f), new Vector2(351f, 310f));
                Box(288f, 306f, 7f, 5f, copper);
                Box(348f, 310f, 7f, 5f, copper);
            }

            void Light(float x, float y, string name, Color colour)
            {
                Box(x, y, 9f, 9f, colour);
                Text(x - 4f, y + 10f, name, _tinyLight);
            }
        }

        // ---- Drawing --------------------------------------------------------------------------

        private void Title(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text, _skin.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", _skin.Button, GUILayout.Width(32f)))
            {
                CloseWindows();
            }

            GUILayout.EndHorizontal();
        }

        private void Fill(Rect rect, Color colour)
        {
            var previous = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(rect, _white);
            GUI.color = previous;
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
            _tiny = new GUIStyle { fontSize = 9, normal = { textColor = new Color(0.12f, 0.12f, 0.12f) } };
            _tinyLight = new GUIStyle(_tiny) { normal = { textColor = new Color(0.88f, 0.92f, 0.86f) } };
            _small = new GUIStyle { fontSize = 12, wordWrap = true, normal = { textColor = new Color(0.9f, 0.9f, 0.88f) } };
            _smallButton = new GUIStyle(_skin.Button) { fixedHeight = 22, fontSize = 12 };
        }
    }
}
