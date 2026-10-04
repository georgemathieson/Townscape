using Townscape.Runtime.Walking;
using UnityEngine;

namespace Townscape.Runtime.UI
{
    /// <summary>
    /// What's on screen while walking: a small dot in the middle so you can see what you're
    /// looking at, a hint under it when that can be used ("E  Open the door"), and a reminder of
    /// the keys for the first few seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WalkingHud : MonoBehaviour
    {
        private const float HintSeconds = 6f;

        private WalkingController _walking;
        private Texture2D _white;
        private GUIStyle _prompt;
        private GUIStyle _hint;

        public void Initialize(WalkingController walking)
        {
            _walking = walking;
        }

        private void OnGUI()
        {
            if (_walking == null || (!_walking.Active && !_walking.Waiting))
            {
                return;
            }

            CreateStyles();
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            var centre = new Vector2(width * 0.5f, height * 0.5f);

            if (_walking.Waiting)
            {
                Shadowed(new Rect(0f, centre.y - 12f, width, 24f), "Getting ready to walk…", _prompt, 1f);
                return;
            }

            // The crosshair: a light dot with a dark rim, so it shows against snow and night alike.
            var focused = _walking.Focus != null;
            var dot = focused ? 6f : 4f;
            Box(new Rect(centre.x - (dot * 0.5f) - 1f, centre.y - (dot * 0.5f) - 1f, dot + 2f, dot + 2f), new Color(0f, 0f, 0f, 0.55f));
            Box(new Rect(centre.x - (dot * 0.5f), centre.y - (dot * 0.5f), dot, dot), new Color(1f, 1f, 1f, focused ? 1f : 0.8f));

            if (focused)
            {
                Shadowed(new Rect(0f, centre.y + 14f, width, 24f), $"<b>E</b>   {_walking.Focus.Prompt}", _prompt, 1f);
            }

            var shown = Time.unscaledTime - _walking.StartedAt;
            if (shown < HintSeconds)
            {
                var fade = Mathf.Clamp01((HintSeconds - shown) / 1.5f);
                Shadowed(
                    new Rect(0f, height - 56f, width, 24f),
                    "Walking   ·   WASD walk, Shift jog, Space jump, E open   ·   V to fly   ·   H for the panel",
                    _hint,
                    fade);
            }
        }

        private void OnDestroy()
        {
            if (_white != null)
            {
                Destroy(_white);
            }
        }

        private void CreateStyles()
        {
            if (_white != null)
            {
                return;
            }

            _white = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
            _prompt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, richText = true };
            _hint = new GUIStyle(_prompt) { fontSize = 13 };
        }

        private void Box(Rect rect, Color colour)
        {
            var previous = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(rect, _white);
            GUI.color = previous;
        }

        private static void Shadowed(Rect rect, string text, GUIStyle style, float alpha)
        {
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f * alpha);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }
    }
}
