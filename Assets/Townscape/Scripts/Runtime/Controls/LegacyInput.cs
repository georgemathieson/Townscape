#if ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine;

namespace Townscape.Runtime.Controls
{
    /// <summary>Reads keyboard and mouse through the legacy Input Manager.</summary>
    public sealed class LegacyInput : ITownscapeInput
    {
        // The legacy mouse axes report roughly a tenth of the pixel delta.
        private const float MouseAxisToPixels = 10f;

        public Vector2 LookDelta => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * MouseAxisToPixels;

        public Vector3 Move => new Vector3(
            Axis(KeyCode.D, KeyCode.A),
            Axis(KeyCode.E, KeyCode.Q),
            Axis(KeyCode.W, KeyCode.S));

        public bool Boost => Input.GetKey(KeyCode.LeftShift);

        public bool LookHeld => Input.GetMouseButton(1);

        public float Scroll => Input.mouseScrollDelta.y;

        public bool WasPressed(Shortcut shortcut)
        {
            switch (shortcut)
            {
                case Shortcut.Dawn: return Input.GetKeyDown(KeyCode.Alpha1);
                case Shortcut.Day: return Input.GetKeyDown(KeyCode.Alpha2);
                case Shortcut.Dusk: return Input.GetKeyDown(KeyCode.Alpha3);
                case Shortcut.Night: return Input.GetKeyDown(KeyCode.Alpha4);
                case Shortcut.ToggleAutoCycle: return Input.GetKeyDown(KeyCode.T);
                case Shortcut.HourBack: return Input.GetKeyDown(KeyCode.LeftBracket);
                case Shortcut.HourForward: return Input.GetKeyDown(KeyCode.RightBracket);
                case Shortcut.ToggleHelp: return Input.GetKeyDown(KeyCode.H);
                case Shortcut.CycleRain: return Input.GetKeyDown(KeyCode.R);
                case Shortcut.CycleLightning: return Input.GetKeyDown(KeyCode.L);
                case Shortcut.CycleWind: return Input.GetKeyDown(KeyCode.G);
                case Shortcut.StrikeLightning: return Input.GetKeyDown(KeyCode.B);
                default: return false;
            }
        }

        private static float Axis(KeyCode positive, KeyCode negative) =>
            (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);
    }
}
#endif
