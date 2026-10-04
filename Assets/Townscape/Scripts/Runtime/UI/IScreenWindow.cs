namespace Townscape.Runtime.UI
{
    /// <summary>
    /// Something in the town that puts a window up while you're walking about (an alarm's keypad,
    /// a street cabinet's screen): while it's up the keys are for it, not the shortcuts.
    /// </summary>
    public interface IScreenWindow
    {
        bool WindowOpen { get; }
    }
}
