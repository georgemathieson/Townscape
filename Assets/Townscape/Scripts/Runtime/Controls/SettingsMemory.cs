using System;
using Townscape.State;
using UnityEngine;

namespace Townscape.Runtime.Controls
{
    /// <summary>
    /// Remembers the user's settings between sessions in PlayerPrefs, so the town opens at the time,
    /// weather and volume it was left at. Saves a second after the last change rather than on every
    /// step of a slider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsMemory : MonoBehaviour
    {
        private const string Key = "Townscape.Settings";
        private const float SaveDelay = 1f;

        private Store<TownState> _store;
        private IDisposable _subscription;
        private bool _first = true;
        private bool _dirty;
        private float _changedAt;

        /// <summary>The saved settings, or <paramref name="defaults"/> if nothing has been saved.</summary>
        public static TownState Load(TownState defaults)
        {
            var saved = PlayerPrefs.GetString(Key, string.Empty);
            return string.IsNullOrEmpty(saved) ? defaults : SavedSettings.Read(saved, defaults);
        }

        public void Initialize(Store<TownState> store)
        {
            _store = store;

            // Watching the saved text means only changes that would be saved count.
            _subscription = store.Subscribe(SavedSettings.Write, _ =>
            {
                if (_first)
                {
                    _first = false;
                    return;
                }

                _dirty = true;
                _changedAt = Time.unscaledTime;
            });
        }

        private void Update()
        {
            if (_dirty && Time.unscaledTime - _changedAt >= SaveDelay)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnDestroy()
        {
            Save();
            _subscription?.Dispose();
        }

        private void Save()
        {
            if (!_dirty || _store == null)
            {
                return;
            }

            PlayerPrefs.SetString(Key, SavedSettings.Write(_store.State));
            PlayerPrefs.Save();
            _dirty = false;
        }
    }
}
