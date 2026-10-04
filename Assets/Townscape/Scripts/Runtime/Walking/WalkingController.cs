using System;
using Townscape.Runtime.CoffeeShop;
using Townscape.Runtime.Controls;
using Townscape.Runtime.UI;
using Townscape.Simulation.Walking;
using UnityEngine;

namespace Townscape.Runtime.Walking
{
    /// <summary>
    /// First-person walking. V drops you to the ground below the camera and you walk at eye
    /// height: the mouse looks round, WASD walks, Shift jogs and Space jumps, kerbs and stairs are
    /// taken in stride, and buildings, walls, lamp posts and tree trunks stop you. The river turns
    /// you back at its edge. Look at something you can use and press E (or click) to use it. V
    /// again goes back to flying from where you are.
    /// </summary>
    /// <remarks>
    /// While walking the control panel tucks away and the mouse is captured for looking; H brings
    /// the panel and the pointer back (looking then needs the right mouse button, as when flying).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class WalkingController : MonoBehaviour
    {
        private const float LookDegreesPerPixel = 0.12f;
        private const float SearchRadius = 16f;
        private const float MapHalfExtent = 170f;

        private CharacterController _body;
        private Transform _camera;
        private FreeFlyCamera _fly;
        private ITownscapeInput _input;
        private TownColliders _colliders;
        private ControlPanel _panel;
        private CoffeeShopGame _coffee;
        private Func<Vector2, float> _groundHeight;
        private float _waterLevel;
        private System.Numerics.Vector2 _velocity;
        private float _fall;
        private float _yaw;
        private float _pitch;
        private bool _panelWasVisible;

        /// <summary>True while walking.</summary>
        public bool Active { get; private set; }

        /// <summary>True between pressing V and the town being solid enough to walk in.</summary>
        public bool Waiting { get; private set; }

        /// <summary>What the crosshair is on and within reach, if it can be used.</summary>
        public IInteractable Focus { get; private set; }

        /// <summary>When walking last began, for the HUD's short reminder of the keys.</summary>
        public float StartedAt { get; private set; }

        /// <summary>
        /// Something on screen has the pointer, such as the alarm's keypad: the walker stands
        /// still, the mouse is free and nothing is looked at.
        /// </summary>
        public bool Paused { get; set; }

        /// <param name="groundHeight">Height of the ground (or the river bed) at a ground-plane position (x, z).</param>
        public void Initialize(
            Transform camera,
            FreeFlyCamera fly,
            ITownscapeInput input,
            TownColliders colliders,
            ControlPanel panel,
            CoffeeShopGame coffee,
            Func<Vector2, float> groundHeight,
            float waterLevel)
        {
            _camera = camera;
            _fly = fly;
            _input = input;
            _colliders = colliders;
            _panel = panel;
            _coffee = coffee;
            _groundHeight = groundHeight;
            _waterLevel = waterLevel;

            // On the Ignore Raycast layer, so looking at things never hits your own body.
            gameObject.layer = 2;
            _body = gameObject.AddComponent<CharacterController>();
            _body.height = WalkMotion.BodyHeight;
            _body.radius = WalkMotion.BodyRadius;
            _body.center = new Vector3(0f, WalkMotion.BodyHeight * 0.5f, 0f);
            _body.stepOffset = WalkMotion.StepHeight;
            _body.slopeLimit = 50f;
            _body.skinWidth = 0.03f;
            _body.minMoveDistance = 0f;
            _body.enabled = false;
        }

        /// <summary>Starts walking (as soon as the town is solid), or goes back to flying.</summary>
        public void Toggle()
        {
            if (Active || Waiting)
            {
                Stop();
            }
            else
            {
                Waiting = true;
            }
        }

        /// <summary>Goes back to flying from wherever you are.</summary>
        public void Stop()
        {
            Waiting = false;
            if (!Active)
            {
                return;
            }

            Active = false;
            Focus = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_body != null)
            {
                _body.enabled = false;
            }

            if (_panel != null)
            {
                _panel.Visible = _panelWasVisible || _panel.Visible;
            }

            if (_fly != null)
            {
                _fly.enabled = true;
            }
        }

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            var coffeeOpen = _coffee != null && _coffee.IsOpen;
            if (Waiting && coffeeOpen)
            {
                Waiting = false;
            }

            if (Waiting && _colliders.IsReady)
            {
                Begin();
            }

            if (!Active)
            {
                return;
            }

            // The coffee shop takes the camera away.
            if (coffeeOpen)
            {
                Stop();
                return;
            }

            if (Paused)
            {
                Focus = null;
                _velocity = System.Numerics.Vector2.Zero;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            var deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Look();
            Move(deltaTime);
            PlaceCamera();
            Aim();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void Begin()
        {
            Waiting = false;

            // The colliders may have been added this very frame.
            Physics.SyncTransforms();
            var camera = _camera.position;
            transform.position = FindStandingSpot(new Vector2(camera.x, camera.z));
            _body.enabled = true;
            _velocity = System.Numerics.Vector2.Zero;
            _fall = 0f;

            var euler = _camera.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = Mathf.Clamp(euler.x > 180f ? euler.x - 360f : euler.x, -85f, 85f);

            _fly.enabled = false;
            if (_panel != null)
            {
                _panelWasVisible = _panel.Visible;
                _panel.Visible = false;
            }

            Active = true;
            StartedAt = Time.unscaledTime;
            PlaceCamera();
        }

        private void Look()
        {
            // With the panel away the mouse always looks; with it showing, the pointer is free for
            // the panel and the right button looks, as when flying.
            var panelShowing = _panel != null && _panel.Visible;
            var looking = !panelShowing || _input.LookHeld;
            Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !looking;
            if (!looking)
            {
                return;
            }

            var delta = _input.LookDelta * LookDegreesPerPixel;
            _yaw = Mathf.Repeat(_yaw + delta.x, 360f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -85f, 85f);
        }

        private void Move(float deltaTime)
        {
            var move = _input.Move;
            var heading = Quaternion.Euler(0f, _yaw, 0f);
            var wish = (heading * Vector3.right * move.x) + (heading * Vector3.forward * move.z);
            var grounded = _body.isGrounded;
            _velocity = WalkMotion.Steer(_velocity, new System.Numerics.Vector2(wish.x, wish.z), _input.Boost, grounded, deltaTime);
            _fall = WalkMotion.Fall(_fall, grounded, _input.WasPressed(Shortcut.Jump), deltaTime);

            var before = transform.position;
            var hit = _body.Move(new Vector3(_velocity.X, _fall, _velocity.Y) * deltaTime);
            if ((hit & CollisionFlags.Above) != 0 && _fall > 0f)
            {
                _fall = 0f;
            }

            var after = transform.position;
            var flat = new Vector2(after.x, after.z);
            if (IsWater(flat) || Mathf.Abs(after.x) > MapHalfExtent || Mathf.Abs(after.z) > MapHalfExtent)
            {
                // The river and the edge of the map turn you back.
                Teleport(new Vector3(before.x, after.y, before.z));
                _velocity = System.Numerics.Vector2.Zero;
            }
            else if (after.y < _groundHeight(flat) - 3f)
            {
                // Somehow under the ground: put the walker back on it.
                Teleport(FindStandingSpot(flat));
                _fall = 0f;
            }
        }

        private void PlaceCamera()
        {
            _camera.SetPositionAndRotation(transform.position + (Vector3.up * WalkMotion.EyeHeight), Quaternion.Euler(_pitch, _yaw, 0f));
        }

        private void Aim()
        {
            Focus = null;
            if (Physics.Raycast(_camera.position, _camera.forward, out var hit, WalkMotion.Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Focus = hit.collider.GetComponentInParent<IInteractable>();
            }

            var clicked = _input.WasPressed(Shortcut.Click) && Cursor.lockState == CursorLockMode.Locked;
            if (Focus != null && (_input.WasPressed(Shortcut.Interact) || clicked))
            {
                Focus.Interact();
            }
        }

        // Rings outward from where the camera is until there's room to stand on dry, open ground.
        private Vector3 FindStandingSpot(Vector2 around)
        {
            for (var ring = 0f; ring <= SearchRadius; ring += 1f)
            {
                var steps = ring == 0f ? 1 : Mathf.CeilToInt(ring * 6.3f);
                for (var i = 0; i < steps; i++)
                {
                    var angle = i * Mathf.PI * 2f / steps;
                    var flat = around + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring);
                    if (IsWater(flat) || UnderARoof(flat))
                    {
                        continue;
                    }

                    var feet = new Vector3(flat.x, _groundHeight(flat) + 0.05f, flat.y);
                    if (!Blocked(feet))
                    {
                        return feet;
                    }
                }
            }

            return new Vector3(around.x, _groundHeight(around) + 0.05f, around.y);
        }

        private bool IsWater(Vector2 flat) => _groundHeight(flat) < _waterLevel - 0.05f;

        // Something solid well above the ground here: a building, so not a place to appear.
        private bool UnderARoof(Vector2 flat)
        {
            var ground = _groundHeight(flat);
            return Physics.Raycast(new Vector3(flat.x, ground + 300f, flat.y), Vector3.down, out var hit, 300f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.point.y > ground + 1f;
        }

        private bool Blocked(Vector3 feet)
        {
            var radius = WalkMotion.BodyRadius;
            var bottom = feet + (Vector3.up * (radius + 0.05f));
            var top = feet + (Vector3.up * (WalkMotion.BodyHeight - radius));
            return Physics.CheckCapsule(bottom, top, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        // A character controller has to be switched off to be moved by hand.
        private void Teleport(Vector3 position)
        {
            _body.enabled = false;
            transform.position = position;
            _body.enabled = true;
        }
    }
}
