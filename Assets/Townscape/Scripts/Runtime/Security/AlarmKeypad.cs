using Townscape.Runtime.Walking;
using UnityEngine;

namespace Townscape.Runtime.Security
{
    /// <summary>One of an alarm's keypads on the wall inside a front door: look at it and press E to bring it up.</summary>
    [DisallowMultipleComponent]
    public sealed class AlarmKeypad : MonoBehaviour, IInteractable
    {
        private AlarmSystem _system;

        public string Prompt => "Use the alarm keypad";

        /// <summary>The keypad's buzzer, for its key clicks and beeps.</summary>
        public AudioSource Beeper { get; private set; }

        public void Initialize(AlarmSystem system, AudioSource beeper)
        {
            _system = system;
            Beeper = beeper;
        }

        public void Interact()
        {
            if (_system != null)
            {
                _system.OpenKeypad(this);
            }
        }
    }
}
