namespace Townscape.State
{
    /// <summary>
    /// A pure function that returns the next state for an action. Reducers must not mutate
    /// <paramref name="state"/> and should return the same instance when nothing changed.
    /// </summary>
    public delegate TState Reducer<TState>(TState state, IAction action);
}
