namespace Townscape.State
{
    /// <summary>The user's sound settings.</summary>
    /// <param name="Volume">Master volume, from 0 to 1.</param>
    /// <param name="Muted">Silences everything without losing the volume.</param>
    public sealed record AudioState(float Volume, bool Muted)
    {
        public static readonly AudioState Default = new AudioState(0.8f, false);

        /// <summary>The volume to actually play at.</summary>
        public float EffectiveVolume => Muted ? 0f : Volume;
    }
}
