namespace Townscape.State
{
    public sealed record SetVolume(float Volume) : IAction;

    public sealed record SetMuted(bool Muted) : IAction;
}
