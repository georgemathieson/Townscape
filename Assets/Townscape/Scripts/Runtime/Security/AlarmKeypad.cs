using Townscape.Runtime.Walking;
using UnityEngine;

namespace Townscape.Runtime.Security
{
    /// <summary>The alarm's control panel on the hall wall: look at it and press E to bring up its keypad.</summary>
    [DisallowMultipleComponent]
    public sealed class AlarmKeypad : MonoBehaviour, IInteractable
    {
        private AlarmSystem _system;

        public string Prompt => "Use the alarm panel";

        public void Initialize(AlarmSystem system)
        {
            _system = system;
        }

        public void Interact()
        {
            if (_system != null)
            {
                _system.OpenPanel();
            }
        }
    }
}
