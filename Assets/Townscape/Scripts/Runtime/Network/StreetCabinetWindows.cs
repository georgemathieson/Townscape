using System.Collections.Generic;
using Townscape.Runtime.UI;
using Townscape.Simulation.Network;
using UnityEngine;

namespace Townscape.Runtime.Network
{
    /// <summary>
    /// What the street cabinet puts on screen with its doors open: the rack drawn on the left
    /// (the patch tray with each customer's yellow lead, the fibre switch and its port lights, the
    /// edge router, the splice shelf, the power strip, the UPS and its battery), and on the right
    /// the patch tray's plugs, the little computer's screen running speed tests, buttons to name
    /// what's wrong, and the fault-finding card from inside the door. It pauses the walker and
    /// frees the mouse while it's up.
    /// </summary>
    public sealed partial class StreetCabinetSystem
    {
        private const float TestSeconds = 6f;
        private const float PingSeconds = 1f;
        private const float DownloadSeconds = 3.5f;
        private const int BarWidth = 16;

        private static readonly string[] MonoFonts = { "Consolas", "Menlo", "DejaVu Sans Mono", "Liberation Mono", "Courier New" };

        private static readonly (LineFault Fault, string Name)[] Guesses =
        {
            (LineFault.None, "Healthy"),
            (LineFault.DirtyConnector, "Dirty connector"),
            (LineFault.BentFibre, "Bent fibre"),
            (LineFault.FailingPort, "Failing port"),
            (LineFault.CongestedUplink, "Busy uplink"),
        };

        private PanelSkin _skin;
        private Texture2D _white;
        private Font _mono;
        private GUIStyle _terminal;
        private GUIStyle _terminalDim;
        private GUIStyle _tiny;
        private GUIStyle _tinyLight;
        private GUIStyle _small;
        private GUIStyle _smallButton;
        private bool _open;
        private float _openedAt;

        // The speed test: the line on screen, whether it's still running, and what it found.
        private int _tested = -1;
        private bool _running;
        private float _testStarted;
        private SpeedTestResult _result;
        private string _verdict;
        private bool _verdictRight;

        /// <summary>True while the cabinet's kit is up on screen.</summary>
        public bool WindowOpen => _open;

        public void OpenWindow()
        {
            _open = true;
            _openedAt = Time.unscaledTime;
            if (_walking != null)
            {
                _walking.Paused = true;
            }
        }

        private void CloseWindow()
        {
            _open = false;
            if (_walking != null)
            {
                _walking.Paused = false;
            }
        }

        private void RunTest(int line)
        {
            _tested = line;
            _running = true;
            _testStarted = Time.unscaledTime;
            _result = _cabinet.RunTest(line);
            _verdict = null;
        }

        private void UpdateTest()
        {
            if (_running && (Time.unscaledTime - _testStarted >= TestSeconds || !_result.Connected))
            {
                _running = false;
            }
        }

        private void OnGUI()
        {
            if (!_open)
            {
                return;
            }

            CreateStyles();
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            HandleKeys();
            if (!_open)
            {
                return;
            }

            var width = Screen.width / scale;
            var height = Screen.height / scale;
            var area = new Rect((width * 0.5f) - 450f, (height * 0.5f) - 310f, 900f, 620f);
            GUILayout.BeginArea(area, _skin.Panel);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Street cabinet: {_spec.Name}", _skin.Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", _skin.Button, GUILayout.Width(32f)))
            {
                CloseWindow();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (!_open)
            {
                return;
            }

            DrawRack(new Rect(area.x + 16f, area.y + 50f, 400f, area.height - 66f));
            GUILayout.BeginArea(new Rect(area.x + 432f, area.y + 44f, area.width - 448f, area.height - 58f));
            Patching();
            Screenful();
            Diagnosis();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Faults found: {_cabinet.Diagnosed}   Wrong guesses: {_cabinet.Misdiagnosed}", _skin.Status);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Done", _smallButton, GUILayout.Width(90f)))
            {
                CloseWindow();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void HandleKeys()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape || (e.keyCode == KeyCode.E && Time.unscaledTime - _openedAt > 0.3f)))
            {
                CloseWindow();
                e.Use();
            }
        }

        // ---- The patch tray -----------------------------------------------------------------

        private void Patching()
        {
            GUILayout.Label("PATCH TRAY", _skin.Heading);
            for (var line = 0; line < _cabinet.Lines.Count; line++)
            {
                var fibre = _cabinet.Lines[line];
                var state = !fibre.Patched ? "unplugged" : !fibre.HasLight ? "no light" : !fibre.Up ? "getting in sync" : "up";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Port {fibre.Port}  {fibre.Customer}: {state}", _small, GUILayout.Width(330f));
                if (GUILayout.Button(fibre.Patched ? "Unplug" : "Plug in", _smallButton, GUILayout.Width(90f)))
                {
                    _cabinet.Patch(line, !fibre.Patched);
                }

                GUILayout.EndHorizontal();
            }
        }

        // ---- The little computer's screen ---------------------------------------------------

        private void Screenful()
        {
            GUILayout.Label("SCREEN", _skin.Heading);
            var screen = GUILayoutUtility.GetRect(420f, 172f, GUILayout.ExpandWidth(true));
            Fill(screen, new Color(0.03f, 0.05f, 0.04f));
            var y = screen.y + 8f;
            foreach (var (text, dim) in Terminal())
            {
                GUI.Label(new Rect(screen.x + 10f, y, screen.width - 20f, 18f), text, dim ? _terminalDim : _terminal);
                y += 17f;
            }

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUI.enabled = !_running;
            for (var line = 0; line < _cabinet.Lines.Count; line++)
            {
                if (GUILayout.Button($"Test port {_cabinet.Lines[line].Port}", _smallButton))
                {
                    RunTest(line);
                }
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        // What's on the screen: the test as it runs (a ping, then the download and upload
        // climbing), and what it found.
        private IEnumerable<(string Text, bool Dim)> Terminal()
        {
            var prompt = $"{_spec.Name.ToLowerInvariant().Replace(' ', '-')}:~$";
            if (_tested < 0)
            {
                yield return ($"{prompt} _", false);
                yield return ("Pick a port to test its line.", true);
                yield break;
            }

            var fibre = _cabinet.Lines[_tested];
            var r = _result;
            yield return ($"{prompt} speedtest --port {fibre.Port}", false);
            yield return ($"{fibre.Customer}, on {fibre.PlanDownMbps:0}/{fibre.PlanUpMbps:0} Mbps", true);
            yield return ($"light at the ONT  {(r.RxDbm <= StreetCabinet.NoLightDbm ? "none" : $"{r.RxDbm:0.0} dBm")}", false);
            if (!r.Connected)
            {
                yield return (r.RxDbm <= StreetCabinet.NoLightDbm ? "line down: no light reaching the ONT" : "line down: the ONT isn't in sync yet", false);
                yield return ($"port {fibre.Port} errors   {r.PortErrors} CRC", false);
                yield break;
            }

            var t = _running ? Time.unscaledTime - _testStarted : TestSeconds;
            if (t < PingSeconds)
            {
                yield return ("ping ...", false);
                yield break;
            }

            yield return ($"ping   {r.PingMs:0.0} ms   jitter {r.JitterMs:0.0} ms", false);
            yield return (Bar("down", Mathf.InverseLerp(PingSeconds, DownloadSeconds, t), r.DownMbps), false);
            if (t < DownloadSeconds)
            {
                yield break;
            }

            yield return (Bar("up  ", Mathf.InverseLerp(DownloadSeconds, TestSeconds, t), r.UpMbps), false);
            if (_running)
            {
                yield break;
            }

            yield return ($"packet loss  {r.LossPercent:0.0} %", false);
            yield return ($"port {fibre.Port} errors   {r.PortErrors} CRC", false);
            yield return ("done.", true);
        }

        // A speed climbing to what the test found, with a little wobble while it runs.
        private static string Bar(string name, float progress, float mbps)
        {
            var filled = Mathf.RoundToInt(progress * BarWidth);
            var wobble = progress < 1f ? 1f + (0.08f * Mathf.Sin(Time.unscaledTime * 13f)) : 1f;
            var shown = mbps * (1f - Mathf.Pow(1f - progress, 3f)) * wobble;
            return $"{name} [{new string('#', filled)}{new string('.', BarWidth - filled)}] {shown,6:0.0} Mbps";
        }

        // ---- Diagnosis ----------------------------------------------------------------------

        private void Diagnosis()
        {
            var ready = _tested >= 0 && !_running;
            GUILayout.Label(ready ? $"WHAT'S WRONG WITH PORT {_cabinet.Lines[_tested].Port}?" : "WHAT'S WRONG? (TEST A PORT FIRST)", _skin.Heading);
            GUI.enabled = ready;
            for (var row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                for (var i = row * 3; i < Mathf.Min(Guesses.Length, (row * 3) + 3); i++)
                {
                    if (GUILayout.Button(Guesses[i].Name, _smallButton))
                    {
                        Diagnose(Guesses[i].Fault, Guesses[i].Name);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUI.enabled = true;
            if (_verdict != null)
            {
                var previous = GUI.color;
                GUI.color = _verdictRight ? new Color(0.6f, 1f, 0.65f) : new Color(1f, 0.7f, 0.55f);
                GUILayout.Label(_verdict, _small);
                GUI.color = previous;
            }

            GUILayout.Label(
                "The card inside the door: healthy is -5 to -12 dBm, pings under 6 ms, nothing lost. Weak but steady light: a " +
                "connector. Very weak light that keeps dropping: the fibre's path. Errors climbing, light fine: the port. Every line slow, long pings: the uplink.",
                _skin.Status);
        }

        private void Diagnose(LineFault guess, string name)
        {
            _verdictRight = _cabinet.Diagnose(_tested, guess);
            _verdict = _verdictRight ? Fixed(guess) : $"Not \"{name}\": the readings don't fit that.";
        }

        private static string Fixed(LineFault fault)
        {
            switch (fault)
            {
                case LineFault.DirtyConnector: return "Right: a dirty connector. Cleaned it and plugged it back in. Test again to check.";
                case LineFault.BentFibre: return "Right: the fibre was bent too tight in the tray. Laid it back round the guides. Test again to check.";
                case LineFault.FailingPort: return "Right: the port's optic was failing. Swapped in a spare. Test again to check.";
                case LineFault.CongestedUplink: return "Right: the uplink was overloaded. The network team have added capacity. Test again to check.";
                default: return "Right: nothing wrong with this line.";
            }
        }

        // ---- The rack, drawn ----------------------------------------------------------------

        private void DrawRack(Rect r)
        {
            var o = new Vector2(r.x, r.y);
            void Box(float x, float y, float w, float h, Color colour) => Fill(new Rect(o.x + x, o.y + y, w, h), colour);
            void Text(float x, float y, string text, GUIStyle style = null) => GUI.Label(new Rect(o.x + x, o.y + y, 200f, 16f), text, style ?? _tinyLight);
            void Wire(Color colour, float thick, params Vector2[] points)
            {
                for (var i = 1; i < points.Length; i++)
                {
                    var a = points[i - 1];
                    var b = points[i];
                    Box(Mathf.Min(a.x, b.x) - (thick * 0.5f), Mathf.Min(a.y, b.y) - (thick * 0.5f), Mathf.Abs(b.x - a.x) + thick, Mathf.Abs(b.y - a.y) + thick, colour);
                }
            }

            const float u = 26f;
            const float left = 44f;
            const float right = 356f;
            var black = new Color(0.07f, 0.07f, 0.08f);
            var grey = new Color(0.32f, 0.33f, 0.35f);
            var silver = new Color(0.68f, 0.7f, 0.72f);
            var yellow = new Color(0.95f, 0.85f, 0.25f);
            var blink = Mathf.Repeat(Time.unscaledTime * BlinkPerSecond, 1f) < 0.5f;
            float Row(int units) => 16f + (units * u);
            void Unit(int at, int units, Color colour) => Box(left, Row(at) + 1f, right - left, (units * u) - 2f, colour);

            // The inside of the cabinet, and the rack's two posts.
            Fill(r, new Color(0.06f, 0.17f, 0.11f));
            Box(left - 12f, 8f, 10f, r.height - 16f, black);
            Box(right + 2f, 8f, 10f, r.height - 16f, black);

            // Patch tray: twelve adapters; the customers' leads plugged in, or hanging loose.
            Unit(0, 1, black);
            Text(right - 44f, Row(0) + 6f, "PATCH");
            var trayY = Row(0) + 6f;
            for (var i = 0; i < 12; i++)
            {
                Box(54f + (i * 20f), trayY, 14f, 14f, new Color(0.2f, 0.4f, 0.85f));
            }

            // Fibre switch: 24 ports in pairs, each with its light.
            Unit(1, 1, grey);
            Text(right - 50f, Row(1) + 6f, "SWITCH");
            for (var port = 1; port <= 24; port++)
            {
                var (x, y) = SwitchPort(Row(1), port);
                Box(x, y, 11f, 8f, black);
                Box(x + 13f, y + 2f, 4f, 4f, PortColour(port, blink));
            }

            for (var line = 0; line < _cabinet.Lines.Count; line++)
            {
                var fibre = _cabinet.Lines[line];
                var adapterX = 61f + (line * 20f);
                var (portX, portY) = SwitchPort(Row(1), fibre.Port);
                var lane = Row(1) - 6f + (line * 2f);
                if (fibre.Patched)
                {
                    Box(adapterX - 5f, trayY + 2f, 10f, 12f, yellow);
                    Wire(yellow, 2f, new Vector2(adapterX, trayY + 14f), new Vector2(adapterX, lane), new Vector2(portX + 5f, lane), new Vector2(portX + 5f, portY));
                }
                else
                {
                    // Pulled out, its end left hanging over the side of the rack.
                    var hang = left - 4f - (line * 7f);
                    Wire(yellow, 2f, new Vector2(portX + 5f, portY), new Vector2(portX + 5f, lane), new Vector2(hang, lane), new Vector2(hang, lane + 16f));
                    Box(hang - 4f, lane + 16f, 8f, 10f, yellow);
                }

                Text(adapterX - 3f, Row(0) - 12f, $"{fibre.Port}", _tinyLight);
            }

            // Edge router, with the uplink from the switch's last port.
            Unit(2, 1, silver);
            Text(left + 8f, Row(2) + 6f, "EDGE ROUTER", _tiny);
            for (var i = 0; i < 4; i++)
            {
                Box(196f + (i * 18f), Row(2) + 8f, 12f, 10f, black);
            }

            var wan = _cabinet.Fault == LineFault.CongestedUplink ? new Color(1f, 0.65f, 0.1f) : new Color(0.2f, 0.95f, 0.3f);
            Box(282f, Row(2) + 9f, 6f, 6f, new Color(0.2f, 0.95f, 0.3f));
            Box(300f, Row(2) + 9f, 6f, 6f, wan);
            Text(276f, Row(2) + 15f, "SYS  WAN", _tiny);
            var (upX, upY) = SwitchPort(Row(1), 24);
            Wire(yellow, 2f, new Vector2(upX + 5f, upY + 8f), new Vector2(upX + 5f, Row(2) - 3f), new Vector2(202f, Row(2) - 3f), new Vector2(202f, Row(2) + 8f));

            // Brush panel, and the splice shelf where the fibres from the duct are joined.
            Unit(3, 1, black);
            Box(left + 10f, Row(3) + 11f, right - left - 20f, 3f, grey);
            Unit(4, 2, new Color(0.86f, 0.86f, 0.84f));
            for (var i = 0; i < 8; i++)
            {
                Box(56f + (i * 36f), Row(4) + 8f, 28f, (2f * u) - 18f, grey);
            }

            Text(left + 6f, Row(6) - 14f, "SPLICES", _tiny);

            // A blanking panel, the power strip, the UPS and its battery pack.
            Unit(8, 2, grey);
            Unit(10, 1, black);
            Text(right - 50f, Row(10) + 6f, "PDU 230V");
            for (var i = 0; i < 6; i++)
            {
                Box(56f + (i * 34f), Row(10) + 6f, 22f, 14f, new Color(0.9f, 0.9f, 0.88f));
                if (i < 3)
                {
                    Box(61f + (i * 34f), Row(10) + 8f, 12f, 10f, black);
                    Wire(black, 3f, new Vector2(67f + (i * 34f), Row(10) + 8f), new Vector2(67f + (i * 34f), Row(10) - 4f - (i * 3f)), new Vector2(right - 6f - (i * 4f), Row(10) - 4f - (i * 3f)), new Vector2(right - 6f - (i * 4f), Row(1) + 13f));
                }
            }

            Unit(11, 2, black);
            Box(60f, Row(11) + 12f, 166f, 26f, new Color(0.45f, 0.75f, 0.85f));
            Text(66f, Row(11) + 18f, "ON LINE  BATT 100%  LOAD 24%", _tiny);
            Box(236f, Row(11) + 22f, 7f, 7f, new Color(0.2f, 0.95f, 0.3f));
            Text(right - 30f, Row(11) + 19f, "UPS");
            Unit(13, 4, black);
            for (var i = 0; i < 6; i++)
            {
                Box(58f + (i * 48f), Row(13) + 10f, 36f, (4f * u) - 20f, new Color(0.15f, 0.15f, 0.16f));
            }

            Text(left + 6f, Row(17) - 16f, "BATTERY");

            // The fibre bundle up from the duct below.
            Wire(yellow, 4f, new Vector2(left - 20f, r.height), new Vector2(left - 20f, Row(4) + 20f), new Vector2(left + 6f, Row(4) + 20f));
            Text(4f, r.height - 18f, "from the duct", _tinyLight);
        }

        // Port 1 is top left, port 2 below it, and so on in pairs.
        private static (float X, float Y) SwitchPort(float rowTop, int port) =>
            (54f + (((port - 1) / 2) * 20f), rowTop + 3f + (((port - 1) % 2) * 10f));

        private Color PortColour(int port, bool blink)
        {
            var off = new Color(0.18f, 0.2f, 0.2f);
            if (port == 24)
            {
                return Traffic(0) ? off : new Color(0.2f, 0.95f, 0.3f);
            }

            if (port > _cabinet.Lines.Count || !_cabinet.Lines[port - 1].HasLight)
            {
                return off;
            }

            if (_cabinet.FaultOn(port - 1) == LineFault.FailingPort)
            {
                return blink ? new Color(1f, 0.65f, 0.1f) : off;
            }

            return _cabinet.Lines[port - 1].Up && Traffic(port) ? off : new Color(0.2f, 0.95f, 0.3f);
        }

        // ---- Drawing --------------------------------------------------------------------------

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
            _mono = Font.CreateDynamicFontFromOSFont(MonoFonts, 13);
            _owned.Add(_mono);
            var green = new Color(0.45f, 1f, 0.55f);
            _terminal = new GUIStyle { font = _mono, fontSize = 13, normal = { textColor = green } };
            _terminalDim = new GUIStyle(_terminal) { normal = { textColor = new Color(green.r, green.g, green.b, 0.6f) } };
            _tiny = new GUIStyle { fontSize = 9, normal = { textColor = new Color(0.12f, 0.12f, 0.12f) } };
            _tinyLight = new GUIStyle(_tiny) { normal = { textColor = new Color(0.88f, 0.92f, 0.86f) } };
            _small = new GUIStyle { fontSize = 12, wordWrap = true, normal = { textColor = new Color(0.9f, 0.9f, 0.88f) } };
            _smallButton = new GUIStyle(_skin.Button) { fixedHeight = 24, fontSize = 12 };
        }
    }
}
