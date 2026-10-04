using Townscape.Runtime.Walking;
using UnityEngine;

namespace Townscape.Runtime.Security
{
    /// <summary>An operator's monitors in the alarm receiving centre: look at them and press E to sit down at the console.</summary>
    [DisallowMultipleComponent]
    public sealed class AlarmCentreConsole : MonoBehaviour, IInteractable
    {
        private AlarmCentreSystem _system;

        public string Prompt => _system != null && _system.Centre.Unacknowledged > 0
            ? "Use the alarm console (an alarm needs attention)"
            : "Use the alarm console";

        public void Initialize(AlarmCentreSystem system)
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
