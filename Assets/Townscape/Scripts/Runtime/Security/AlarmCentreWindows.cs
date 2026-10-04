using System.Linq;
using Townscape.Runtime.UI;
using Townscape.Simulation.Security;
using UnityEngine;

namespace Townscape.Runtime.Security
{
    /// <summary>
    /// The alarm receiving centre's console: every site down the left with what it last
    /// reported, the open incidents with buttons to acknowledge one, send a guard, call the
    /// police or close it, and the log underneath. It pauses the walker and frees the mouse while
    /// it's up.
    /// </summary>
    public sealed partial class AlarmCentreSystem
    {
        private const int LogLines = 18;

        private PanelSkin _skin;
        private Texture2D _white;
        private GUIStyle _small;
        private GUIStyle _smallBold;
        private GUIStyle _smallButton;
        private GUIStyle _row;
        private bool _open;
        private float _openedAt;
        private ArcIncident _selected;

        /// <summary>True while the console is up on screen.</summary>
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

        private void OnGUI()
        {
            if (!_open)
            {
                return;
            }

            CreateStyles();
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape || (e.keyCode == KeyCode.E && Time.unscaledTime - _openedAt > 0.3f)))
            {
                CloseWindow();
                e.Use();
                return;
            }

            var width = Screen.width / scale;
            var height = Screen.height / scale;
            var area = new Rect((width * 0.5f) - 490f, (height * 0.5f) - 320f, 980f, 640f);
            GUILayout.BeginArea(area, _skin.Panel);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Alarm receiving centre: {_spec.Name}   {Clock(_hour())}", _skin.Title);
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

            Sites(new Rect(area.x + 16f, area.y + 50f, 300f, area.height - 66f));
            GUILayout.BeginArea(new Rect(area.x + 332f, area.y + 46f, area.width - 348f, area.height - 60f));
            Incidents();
            Log();
            GUILayout.EndArea();
        }

        // ---- The sites ----------------------------------------------------------------------

        private void Sites(Rect r)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 18f), "SITES", _skin.Heading);
            var y = r.y + 24f;
            var flash = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
            foreach (var site in _centre.Sites)
            {
                var card = new Rect(r.x, y, r.width, 66f);
                Fill(card, new Color(0.13f, 0.14f, 0.17f));
                Fill(new Rect(card.x, card.y, 8f, card.height), Gui(TileColour(site, flash)));
                GUI.Label(new Rect(card.x + 16f, card.y + 6f, card.width - 20f, 18f), site.Name, _smallBold);
                GUI.Label(new Rect(card.x + 16f, card.y + 25f, card.width - 20f, 18f), Status(site), _small);
                GUI.Label(new Rect(card.x + 16f, card.y + 43f, card.width - 20f, 18f), Faults(site), _small);
                y += 74f;
            }

            GUI.Label(
                new Rect(r.x, y + 6f, r.width, 120f),
                "Each alarm reports here over its building's broadband. If a line goes down at the street cabinet, its signal is lost, and nothing it does reaches the centre until the line comes back.",
                _skin.Status);
        }

        private static string Status(ArcSite site)
        {
            if (!site.Online)
            {
                return $"NO SIGNAL (last heard {site.State.ToString().ToLowerInvariant()})";
            }

            switch (site.State)
            {
                case AlarmState.Exiting: return "SETTING";
                case AlarmState.Set: return "SET";
                case AlarmState.Sounding: return "IN ALARM";
                default: return "UNSET";
            }
        }

        private static string Faults(ArcSite site)
        {
            if (site.Faults == AlarmFaults.None)
            {
                return site.Online ? "No faults" : "Signal path lost";
            }

            var text = "Fault:";
            text += (site.Faults & AlarmFaults.Mains) != 0 ? " mains" : string.Empty;
            text += (site.Faults & AlarmFaults.Battery) != 0 ? " battery" : string.Empty;
            text += (site.Faults & AlarmFaults.Bell) != 0 ? " bell box" : string.Empty;
            return text;
        }

        // ---- Incidents ----------------------------------------------------------------------

        private void Incidents()
        {
            var open = _centre.Incidents.Where(i => !i.Closed).Reverse().ToList();
            if (_selected == null || _selected.Closed)
            {
                _selected = open.FirstOrDefault();
            }

            GUILayout.Label(open.Count == 0 ? "INCIDENTS: NONE OPEN" : $"INCIDENTS ({open.Count} OPEN)", _skin.Heading);
            foreach (var incident in open.Take(4))
            {
                var label = $"{Clock(incident.OpenedHour)}   {incident.Site.Name}: {incident.Summary}   ({Stage(incident)})";
                var chosen = incident == _selected;
                if (GUILayout.Toggle(chosen, label, _row) && !chosen)
                {
                    _selected = incident;
                }
            }

            if (_selected == null)
            {
                GUILayout.Label("All quiet. Alarms from any site open an incident here, and the room chimes until someone acknowledges it.", _small);
                return;
            }

            var selected = _selected;
            GUILayout.Space(4f);
            GUILayout.Label($"Guard: {Responder(selected.Guard)}.   Police: {Responder(selected.Police)}.{(selected.CancelledAtKeypad ? "   Someone put the code in at the keypad." : string.Empty)}", _small);
            GUILayout.BeginHorizontal();
            GUI.enabled = !selected.Acknowledged;
            if (GUILayout.Button("Acknowledge", _smallButton))
            {
                _centre.Acknowledge(selected);
            }

            GUI.enabled = selected.Guard.Stage == ResponderStage.None;
            if (GUILayout.Button("Send a guard", _smallButton))
            {
                _centre.DispatchGuard(selected);
            }

            GUI.enabled = selected.Police.Stage == ResponderStage.None;
            if (GUILayout.Button("Call the police", _smallButton))
            {
                _centre.CallPolice(selected);
            }

            GUI.enabled = true;
            if (GUILayout.Button("Close incident", _smallButton))
            {
                _centre.Close(selected);
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(
                "The police only come to a confirmed alarm (two different zones), or once a guard has found a break-in. A guard who finds the building secure puts the code in and unsets the alarm.",
                _skin.Status);
        }

        private static string Stage(ArcIncident incident)
        {
            if (incident.Police.Stage == ResponderStage.EnRoute || incident.Police.Stage == ResponderStage.OnSite)
            {
                return incident.Police.Stage == ResponderStage.EnRoute ? "police on the way" : "police on site";
            }

            switch (incident.Guard.Stage)
            {
                case ResponderStage.EnRoute: return "guard on the way";
                case ResponderStage.OnSite: return "guard on site";
                case ResponderStage.Done: return incident.BreakInFound ? "break-in found" : "guard reported back";
                default: return incident.Acknowledged ? "acknowledged" : "NEW";
            }
        }

        private static string Responder(Responder responder)
        {
            switch (responder.Stage)
            {
                case ResponderStage.EnRoute: return $"on the way, {Mathf.CeilToInt(responder.Remaining)} s";
                case ResponderStage.OnSite: return $"looking round, {Mathf.CeilToInt(responder.Remaining)} s";
                case ResponderStage.Done: return "reported back";
                default: return "not sent";
            }
        }

        // ---- The log ------------------------------------------------------------------------

        private void Log()
        {
            GUILayout.Space(6f);
            GUILayout.Label("LOG", _skin.Heading);
            var rect = GUILayoutUtility.GetRect(600f, (LogLines * 17f) + 12f, GUILayout.ExpandWidth(true));
            Fill(rect, new Color(0.04f, 0.05f, 0.06f));
            var log = _centre.Log;
            var first = Mathf.Max(0, log.Count - LogLines);
            var y = rect.y + 6f;
            for (var i = first; i < log.Count; i++)
            {
                var entry = log[i];
                var previous = GUI.color;
                GUI.color = Severity(entry.Severity);
                GUI.Label(new Rect(rect.x + 8f, y, rect.width - 16f, 17f), $"{Clock(entry.Hour)}  {entry.Site}: {entry.Text}", _small);
                GUI.color = previous;
                y += 17f;
            }

            if (log.Count == 0)
            {
                GUI.Label(new Rect(rect.x + 8f, y, rect.width - 16f, 17f), "Nothing reported yet.", _small);
            }
        }

        private static Color Severity(ArcSeverity severity)
        {
            switch (severity)
            {
                case ArcSeverity.Alarm: return new Color(1f, 0.45f, 0.4f);
                case ArcSeverity.Warning: return new Color(1f, 0.78f, 0.35f);
                case ArcSeverity.Response: return new Color(0.55f, 0.8f, 1f);
                default: return new Color(0.82f, 0.86f, 0.82f);
            }
        }

        // ---- Drawing --------------------------------------------------------------------------

        private static string Clock(float hour)
        {
            var minutes = Mathf.FloorToInt(Mathf.Repeat(hour, 24f) * 60f);
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }

        // A tile's HDR glow, brought back into range for the screen.
        private static Color Gui(Color glow)
        {
            var peak = Mathf.Max(glow.r, Mathf.Max(glow.g, glow.b));
            return peak > 1f ? new Color(glow.r / peak, glow.g / peak, glow.b / peak) : new Color(glow.r * 1.6f, glow.g * 1.6f, glow.b * 1.6f);
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
            _small = new GUIStyle { fontSize = 12, wordWrap = true, normal = { textColor = new Color(0.9f, 0.9f, 0.88f) } };
            _smallBold = new GUIStyle(_small) { fontStyle = FontStyle.Bold, fontSize = 13 };
            _smallButton = new GUIStyle(_skin.Button) { fixedHeight = 26, fontSize = 12 };
            _row = new GUIStyle(_skin.Button) { alignment = TextAnchor.MiddleLeft, fixedHeight = 26, fontSize = 12 };
        }
    }
}
