using System;
using UnityEngine;

namespace Townscape.Runtime.Controls
{
    /// <summary>
    /// Scene-view style fly camera: hold the right mouse button to look, WASD to move, Q/E for
    /// down/up, Shift to go faster and the scroll wheel to change speed. It stays inside the map and
    /// above the ground and water, but does not collide with structures, so you can fly under the bridge.
    /// <see cref="FlyTo"/> glides it somewhere on its own; moving or looking takes back control.
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
        [SerializeField] private float glideSeconds = 1.6f;

        private ITownscapeInput _input;
        private Func<Vector2, float> _floorHeight;
        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;
        private float _speedScale = 1f;
        private bool _gliding;
        private float _glideTime;
        private Vector3 _glideFrom;
        private Vector3 _glideTo;
        private Quaternion _glideFromRotation;
        private Quaternion _glideToRotation;

        /// <param name="floorHeight">Lowest walkable height at a ground-plane position (x, z): the ground or the water surface.</param>
        public void Initialize(ITownscapeInput input, Func<Vector2, float> floorHeight)
        {
            _input = input;
            _floorHeight = floorHeight;
            var euler = transform.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        /// <summary>True while <see cref="FlyTo"/> is moving the camera.</summary>
        public bool IsGliding => _gliding;

        /// <summary>Glides to <paramref name="position"/>, ending up looking at <paramref name="lookAt"/>.</summary>
        public void FlyTo(Vector3 position, Vector3 lookAt)
        {
            _gliding = true;
            _glideTime = 0f;
            _glideFrom = transform.position;
            _glideTo = position;
            _glideFromRotation = transform.rotation;
            _glideToRotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            _velocity = Vector3.zero;
        }

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            if (_gliding)
            {
                Glide(Time.unscaledDeltaTime);
                return;
            }

            Look();
            Move(Time.unscaledDeltaTime);
        }

        private void Glide(float deltaTime)
        {
            // Moving or looking takes back control from wherever the glide has got to.
            if (_input.LookHeld || _input.Move.sqrMagnitude > 0.01f)
            {
                EndGlide();
                return;
            }

            _glideTime += deltaTime;
            var t = Mathf.Clamp01(_glideTime / Mathf.Max(0.01f, glideSeconds));
            var eased = t * t * (3f - (2f * t));
            transform.SetPositionAndRotation(
                Vector3.Lerp(_glideFrom, _glideTo, eased),
                Quaternion.Slerp(_glideFromRotation, _glideToRotation, eased));

            if (t >= 1f)
            {
                EndGlide();
            }
        }

        private void EndGlide()
        {
            _gliding = false;
            var euler = transform.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        // Pick up wherever something else (walking, the coffee shop) left the camera pointing.
        private void OnEnable()
        {
            var euler = transform.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            _velocity = Vector3.zero;
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
