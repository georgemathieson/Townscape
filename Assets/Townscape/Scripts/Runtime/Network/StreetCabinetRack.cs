using Townscape.Runtime.Walking;
using UnityEngine;

namespace Townscape.Runtime.Network
{
    /// <summary>The kit inside the street cabinet, behind its doors: look at it and press E to work on it.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetCabinetRack : MonoBehaviour, IInteractable
    {
        private StreetCabinetSystem _system;

        public string Prompt => "Use the cabinet's screen and patch tray";

        public void Initialize(StreetCabinetSystem system)
        {
            _system = system;
        }

        public void Interact()
        {
            if (_system != null)
            {
                _system.OpenWindow();
            }
        }
    }
}
