using System;
using UnityEngine;

namespace Townscape.Runtime.Controls
{
    /// <summary>
    /// Scene-view style fly camera: hold the right mouse button to look, WASD to move, Q/E for
    /// down/up, Shift to go faster and the scroll wheel to change speed. It stays inside the map and
    /// above the ground and water, but does not collide with structures, so you can fly under the bridge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FreeFlyCamera : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 9f;
        [SerializeField] private float boostMultiplier = 4f;
        [SerializeField] private float lookDegreesPerPixel = 0.12f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private Vector3 boundsMin = new Vector3(-170f, -50f, -170f);
        [SerializeField] private Vector3 boundsMax = new Vector3(170f, 140f, 170f);
        [SerializeField] private float clearance = 0.6f;

        private ITownscapeInput _input;
        private Func<Vector2, float> _floorHeight;
        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;
        private float _speedScale = 1f;

        /// <param name="floorHeight">Lowest walkable height at a ground-plane position (x, z): the ground or the water surface.</param>
        public void Initialize(ITownscapeInput input, Func<Vector2, float> floorHeight)
        {
            _input = input;
            _floorHeight = floorHeight;
            var euler = transform.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            Look();
            Move(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Look()
        {
            if (!_input.LookHeld)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            var delta = _input.LookDelta * lookDegreesPerPixel;
            _yaw = Mathf.Repeat(_yaw + delta.x, 360f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -89f, 89f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void Move(float deltaTime)
        {
            var scroll = _input.Scroll;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _speedScale = Mathf.Clamp(_speedScale * (scroll > 0f ? 1.15f : 1f / 1.15f), 0.2f, 8f);
            }

            var move = _input.Move;
            var direction = (transform.right * move.x) + (Vector3.up * move.y) + (transform.forward * move.z);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            var speed = moveSpeed * _speedScale * (_input.Boost ? boostMultiplier : 1f);
            _velocity = Vector3.Lerp(_velocity, direction * speed, 1f - Mathf.Exp(-acceleration * deltaTime));

            var position = transform.position + (_velocity * deltaTime);
            position = Vector3.Max(boundsMin, Vector3.Min(boundsMax, position));
            if (_floorHeight != null)
            {
                position.y = Mathf.Max(position.y, _floorHeight(new Vector2(position.x, position.z)) + clearance);
            }

            transform.position = position;
        }
    }
}
