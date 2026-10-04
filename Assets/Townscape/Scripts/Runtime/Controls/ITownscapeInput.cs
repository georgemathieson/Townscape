using UnityEngine;

namespace Townscape.Runtime.Controls
{
    public enum Shortcut
    {
        Dawn,
        Day,
        Dusk,
        Night,
        ToggleAutoCycle,
        HourBack,
        HourForward,
        ToggleHelp,
        CycleRain,
        CycleLightning,
        CycleWind,
        StrikeLightning,
        ToggleMute,
        CyclePerformance,
        ToggleCoffeeShop,
    }

    /// <summary>
    /// Strategy for reading the player's input. One implementation uses the Input System package
    /// and one the legacy Input Manager, so the project works whichever handling is enabled.
    /// </summary>
    public interface ITownscapeInput
    {
        /// <summary>Mouse movement this frame, in pixels.</summary>
        Vector2 LookDelta { get; }

        /// <summary>Requested movement: x is strafe, y is up/down, z is forward. Each in [-1, 1].</summary>
        Vector3 Move { get; }

        bool Boost { get; }

        /// <summary>True while the look button (right mouse) is held.</summary>
        bool LookHeld { get; }

        /// <summary>Scroll wheel this frame: positive is away from the user.</summary>
        float Scroll { get; }

        bool WasPressed(Shortcut shortcut);
    }

    public static class TownscapeInput
    {
        /// <summary>Picks the input strategy that matches the project's active input handling.</summary>
        public static ITownscapeInput CreateDefault()
        {
#if ENABLE_INPUT_SYSTEM
            return new InputSystemInput();
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new LegacyInput();
#else
            return new NoInput();
#endif
        }
    }

    /// <summary>Used when no input backend is enabled, so nothing breaks.</summary>
    public sealed class NoInput : ITownscapeInput
    {
        public Vector2 LookDelta => Vector2.zero;

        public Vector3 Move => Vector3.zero;

        public bool Boost => false;

        public bool LookHeld => false;

        public float Scroll => 0f;

        public bool WasPressed(Shortcut shortcut) => false;
    }
}
