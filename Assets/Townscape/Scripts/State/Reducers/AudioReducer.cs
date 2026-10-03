namespace Townscape.State
{
    public static class AudioReducer
    {
        public static AudioState Reduce(AudioState state, IAction action)
        {
            AudioState next;
            switch (action)
            {
                case SetVolume volume:
                    next = state with { Volume = TimeMath.Clamp01(volume.Volume) };
                    break;
                case SetMuted muted:
                    next = state with { Muted = muted.Muted };
                    break;
                default:
                    return state;
            }

            return next == state ? state : next;
        }
    }
}
