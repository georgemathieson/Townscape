using System;
using System.Numerics;

namespace Townscape.Simulation.Walking
{
    /// <summary>
    /// How a walker moves: an adult's size and eye height, walking and jogging pace, a small jump,
    /// and enough step to go up kerbs and stairs without jumping. Collisions are left to Unity's
    /// character controller; this decides the velocity it is asked to move at.
    /// </summary>
    public static class WalkMotion
    {
        public const float BodyHeight = 1.75f;

        public const float BodyRadius = 0.28f;

        public const float EyeHeight = 1.62f;

        /// <summary>The tallest step taken in stride: a kerb is 0.14 m, a stair about 0.2 m.</summary>
        public const float StepHeight = 0.4f;

        public const float WalkSpeed = 2.6f;

        public const float RunSpeed = 5.2f;

        public const float JumpSpeed = 4.4f;

        public const float Gravity = 18f;

        /// <summary>How far away a door can be and still be opened.</summary>
        public const float Reach = 2.2f;

        // Pressed down a little while on the ground, so the walker follows slopes and steps down kerbs.
        private const float GroundedFall = -2f;
        private const float TerminalFall = -30f;
        private const float GroundGrip = 12f;
        private const float AirGrip = 2f;

        /// <summary>How high a jump goes, in metres.</summary>
        public static float JumpHeight => JumpSpeed * JumpSpeed / (2f * Gravity);

        /// <summary>
        /// Horizontal velocity after <paramref name="deltaTime"/> seconds of heading in
        /// <paramref name="direction"/> (each axis from -1 to 1; zero to stop). It speeds up and
        /// slows down quickly on the ground and only a little in the air.
        /// </summary>
        public static Vector2 Steer(Vector2 velocity, Vector2 direction, bool running, bool grounded, float deltaTime)
        {
            if (direction.LengthSquared() > 1f)
            {
                direction = Vector2.Normalize(direction);
            }

            var target = direction * (running ? RunSpeed : WalkSpeed);
            var grip = grounded ? GroundGrip : AirGrip;
            return Vector2.Lerp(velocity, target, 1f - MathF.Exp(-grip * deltaTime));
        }

        /// <summary>Vertical speed after <paramref name="deltaTime"/> seconds: a jump from the ground, or falling.</summary>
        public static float Fall(float verticalSpeed, bool grounded, bool jump, float deltaTime)
        {
            if (grounded && verticalSpeed <= 0f)
            {
                return jump ? JumpSpeed : GroundedFall;
            }

            return MathF.Max(TerminalFall, verticalSpeed - (Gravity * deltaTime));
        }
    }
}
