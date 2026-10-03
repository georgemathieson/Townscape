#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Townscape.Runtime.Controls
{
    /// <summary>Reads keyboard and mouse through the Input System package.</summary>
    public sealed class InputSystemInput : ITownscapeInput
    {
        public Vector2 LookDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        public Vector3 Move
        {
            get
            {
                var keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    return Vector3.zero;
                }

                return new Vector3(
                    Axis(keyboard.dKey, keyboard.aKey),
                    Axis(keyboard.eKey, keyboard.qKey),
                    Axis(keyboard.wKey, keyboard.sKey));
            }
        }

        public bool Boost => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        public bool LookHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;

        public float Scroll => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;

        public bool WasPressed(Shortcut shortcut)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            switch (shortcut)
            {
                case Shortcut.Dawn: return keyboard.digit1Key.wasPressedThisFrame;
                case Shortcut.Day: return keyboard.digit2Key.wasPressedThisFrame;
                case Shortcut.Dusk: return keyboard.digit3Key.wasPressedThisFrame;
                case Shortcut.Night: return keyboard.digit4Key.wasPressedThisFrame;
                case Shortcut.ToggleAutoCycle: return keyboard.tKey.wasPressedThisFrame;
                case Shortcut.HourBack: return keyboard.leftBracketKey.wasPressedThisFrame;
                case Shortcut.HourForward: return keyboard.rightBracketKey.wasPressedThisFrame;
                case Shortcut.ToggleHelp: return keyboard.hKey.wasPressedThisFrame;
                case Shortcut.CycleRain: return keyboard.rKey.wasPressedThisFrame;
                case Shortcut.CycleLightning: return keyboard.lKey.wasPressedThisFrame;
                case Shortcut.CycleWind: return keyboard.gKey.wasPressedThisFrame;
                case Shortcut.StrikeLightning: return keyboard.bKey.wasPressedThisFrame;
                case Shortcut.ToggleMute: return keyboard.mKey.wasPressedThisFrame;
                case Shortcut.CyclePerformance: return keyboard.fKey.wasPressedThisFrame;
                default: return false;
            }
        }

        private static float Axis(ButtonControl positive, ButtonControl negative) =>
            (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
    }
}
#endif
