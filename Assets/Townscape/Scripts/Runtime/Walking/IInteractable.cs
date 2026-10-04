namespace Townscape.Runtime.Walking
{
    /// <summary>Something a walker can use by looking at it and pressing E or clicking: a door, say.</summary>
    public interface IInteractable
    {
        /// <summary>What pressing E would do, shown under the crosshair ("Open the door").</summary>
        string Prompt { get; }

        void Interact();
    }
}
