// Compile-check stubs for com.unity.inputsystem. Not used by Unity.
#pragma warning disable
using UnityEngine;

namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl
    {
        public bool isPressed => false;
        public bool wasPressedThisFrame => false;
    }

    public class Vector2Control
    {
        public Vector2 ReadValue() => Vector2.zero;
    }

    public class DeltaControl : Vector2Control { }
}

namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;

    public class Keyboard
    {
        public static Keyboard current => null;
        public ButtonControl aKey, dKey, eKey, qKey, sKey, wKey, tKey, hKey, rKey, lKey, gKey, bKey, mKey, fKey, cKey, nKey;
        public ButtonControl digit1Key, digit2Key, digit3Key, digit4Key;
        public ButtonControl leftShiftKey, leftBracketKey, rightBracketKey;
    }

    public class Mouse
    {
        public static Mouse current => null;
        public DeltaControl delta;
        public DeltaControl scroll;
        public ButtonControl rightButton;
    }
}
