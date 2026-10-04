using Townscape.Runtime.Walking;
using UnityEngine;

namespace Townscape.Runtime.Security
{
    /// <summary>An alarm's control box on the wall: press E to take its lid off and see the board inside.</summary>
    [DisallowMultipleComponent]
    public sealed class AlarmControlBox : MonoBehaviour, IInteractable
    {
        private AlarmSystem _system;

        public string Prompt => _system == null || _system.Alarm.EngineerMode || !_system.Alarm.Powered
            ? "Open the alarm control box"
            : "Open the alarm control box (it has a tamper switch)";

        public void Initialize(AlarmSystem system)
        {
            _system = system;
        }

        public void Interact()
        {
            if (_system != null)
            {
                _system.OpenControlBox();
            }
        }
    }
}
