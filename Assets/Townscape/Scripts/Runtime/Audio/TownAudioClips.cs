using System;
using UnityEngine;

namespace Townscape.Runtime.Audio
{
    /// <summary>
    /// Slots for real recordings (for example from freesound.org). Any slot left empty plays a
    /// sound synthesised in code instead, so the village is never silent.
    /// </summary>
    [Serializable]
    public sealed class TownAudioClips
    {
        [Tooltip("A seamless loop of steady rain.")]
        public AudioClip rainLoop;

        [Tooltip("A seamless loop of wind.")]
        public AudioClip windLoop;

        [Tooltip("A seamless loop of a small river or beck.")]
        public AudioClip riverLoop;

        [Tooltip("Close thunder: a sharp crack and a roll. One is picked at random for each close strike.")]
        public AudioClip[] thunderCracks = Array.Empty<AudioClip>();

        [Tooltip("Distant thunder: a low rumble. One is picked at random for each distant strike.")]
        public AudioClip[] thunderRumbles = Array.Empty<AudioClip>();
    }
}
