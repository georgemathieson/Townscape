using System;
using Townscape.CoffeeShop;
using Townscape.Generation.Buildings;
using Townscape.Runtime.Controls;
using Townscape.State;
using UnityEngine;

namespace Townscape.Runtime.CoffeeShop
{
    /// <summary>
    /// Fellside Coffee, the management game in the village's coffee shop. It owns the game's own
    /// store (separate from the town's), loads and saves it, and takes the camera to the shopfront
    /// when the game is opened. The rules all live in Townscape.CoffeeShop; this only connects them
    /// to Unity, and the window only dispatches actions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoffeeShopGame : MonoBehaviour
    {
        private const float SaveDelay = 1f;
        private const float ViewDistance = 8.5f;
        private const float ViewSideways = 2.5f;
        private const float EyeHeight = 2.1f;
        private const float SignHeight = 2.6f;

        private FreeFlyCamera _camera;
        private Footprint _shop;
        private Func<Vector2, float> _groundHeight;
        private IDisposable _subscription;
        private bool _first = true;
        private bool _dirty;
        private float _changedAt;

        public CoffeeShopBalance Balance { get; private set; }

        public Store<CoffeeShopState> Store { get; private set; }

        /// <summary>Whether the game's window is showing.</summary>
        public bool IsOpen { get; private set; }

        /// <param name="shop">The coffee shop's building, or null if the village doesn't have one.</param>
        /// <param name="groundHeight">Ground height at a ground-plane position (x, z).</param>
        public void Initialize(FreeFlyCamera flyCamera, Footprint shop, Func<Vector2, float> groundHeight, bool logActions)
        {
            _camera = flyCamera;
            _shop = shop;
            _groundHeight = groundHeight;

            Balance = DefaultBalance.Create();
            foreach (var problem in Balance.Validate())
            {
                Debug.LogError($"[Fellside Coffee] Balance problem: {problem}");
            }

            Store = new Store<CoffeeShopState>(CoffeeShopReducer.Create(Balance), CoffeeShopSaveFile.Load(Balance));
            if (logActions)
            {
                Store.ActionProcessed += (action, _) => Debug.Log($"[Fellside Coffee] {action}");
            }

            // Watching the saved text means only changes that would be saved count.
            _subscription = Store.Subscribe(CoffeeShopSave.Write, _ =>
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

        public void Open()
        {
            if (IsOpen || Store == null)
            {
                return;
            }

            IsOpen = true;
            if (_camera != null && _shop != null)
            {
                _camera.FlyTo(ViewPoint(out var lookAt), lookAt);
            }
        }

        public void Close()
        {
            IsOpen = false;
            Save();
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
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
            if (!_dirty || Store == null)
            {
                return;
            }

            CoffeeShopSaveFile.Save(Store.State);
            _dirty = false;
        }

        // Across the street from the shop, a little to one side, looking at the sign.
        private Vector3 ViewPoint(out Vector3 lookAt)
        {
            var front = ToUnity((_shop.FrontLeft + _shop.FrontRight) * 0.5f);
            var outward = ToUnity(_shop.Outward);
            var along = ToUnity(System.Numerics.Vector2.Normalize(_shop.FrontRight - _shop.FrontLeft));
            var eye = front + (outward * ViewDistance) + (along * ViewSideways);

            lookAt = new Vector3(front.x, Ground(front) + SignHeight, front.z);
            return new Vector3(eye.x, Ground(eye) + EyeHeight, eye.z);
        }

        private float Ground(Vector3 position) => _groundHeight != null ? _groundHeight(new Vector2(position.x, position.z)) : 0f;

        private static Vector3 ToUnity(System.Numerics.Vector2 v) => new Vector3(v.X, 0f, v.Y);
    }
}
