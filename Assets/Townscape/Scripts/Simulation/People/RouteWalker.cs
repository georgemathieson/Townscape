using System;
using System.Numerics;
using Townscape.Generation;
using Townscape.Generation.Routes;

namespace Townscape.Simulation.People
{
    /// <summary>The town's doors, as the people walking about it use them, by name.</summary>
    public interface IDoors
    {
        bool IsOpen(string door);

        /// <summary>Starts it swinging open (if it's shut).</summary>
        void Open(string door);

        /// <summary>Starts it swinging shut (if it's open).</summary>
        void Shut(string door);
    }

    /// <summary>
    /// Someone (or something: a car) going along a <see cref="RoutePath"/>, stop to stop at a
    /// steady pace. At a door that's shut they open it and wait for it to swing out of the way;
    /// if they're the sort who shuts doors behind them, they do. Someone standing in the way
    /// holds them up.
    /// </summary>
    public sealed class RouteWalker
    {
        /// <summary>How long a door takes to swing open enough to go through.</summary>
        public const float DoorSeconds = 0.8f;

        private RoutePath _path;
        private int _next;
        private float _doorWait;
        private string _opened;
        private float _current;

        public RouteWalker(Vector3 position, float speed)
        {
            Position = position;
            Speed = speed;
        }

        public Vector3 Position { get; private set; }

        /// <summary>The level way they face.</summary>
        public Vector3 Facing { get; private set; } = Vector3.UnitZ;

        /// <summary>Their top speed, in metres a second.</summary>
        public float Speed { get; set; }

        /// <summary>How quickly they pick up speed and slow for the end of the way (zero: at once).</summary>
        public float Acceleration { get; set; }

        /// <summary>How fast they're going now.</summary>
        public float CurrentSpeed => _current;

        /// <summary>Shut each door behind them.</summary>
        public bool ShutsDoors { get; set; }

        /// <summary>Someone's in the way: they stand and wait.</summary>
        public bool Held { get; set; }

        /// <summary>How far they've gone altogether (for the swing of their arms and legs).</summary>
        public float Walked { get; private set; }

        /// <summary>Whether they moved in the last tick.</summary>
        public bool Moving { get; private set; }

        /// <summary>At the end of the way (or not going anywhere).</summary>
        public bool Arrived => _path == null || _next >= _path.Stops.Count;

        /// <summary>How far is left to go.</summary>
        public float Remaining
        {
            get
            {
                if (Arrived)
                {
                    return 0f;
                }

                var left = Vector3.Distance(Position, _path.Stops[_next].Position);
                for (var i = _next + 1; i < _path.Stops.Count; i++)
                {
                    left += Vector3.Distance(_path.Stops[i - 1].Position, _path.Stops[i].Position);
                }

                return left;
            }
        }

        /// <summary>Sets off along <paramref name="path"/> (from wherever its first stop is: they should be there).</summary>
        public void Follow(RoutePath path)
        {
            _path = path;
            _next = 0;
            _doorWait = 0f;
            _opened = null;
            if (path != null && path.Stops.Count > 0 && Vector3.DistanceSquared(path.Stops[0].Position, Position) < 1e-4f)
            {
                _next = 1;
            }
        }

        /// <summary>Stops where they are.</summary>
        public void Stop()
        {
            _path = null;
            _current = 0f;
        }

        /// <summary>Puts them somewhere at once, going nowhere.</summary>
        public void Place(Vector3 position)
        {
            Stop();
            Position = position;
        }

        /// <summary>Turns them to face <paramref name="direction"/> (only its level part counts).</summary>
        public void Face(Vector3 direction)
        {
            var level = new Vector3(direction.X, 0f, direction.Z);
            if (level.LengthSquared() > 1e-6f)
            {
                Facing = Vector3.Normalize(level);
            }
        }

        public void Tick(float deltaTime, IDoors doors)
        {
            Moving = false;
            if (Arrived || deltaTime <= 0f)
            {
                return;
            }

            if (Held)
            {
                _current = 0f;
                return;
            }

            var step = _path.Stops[_next];
            if (step.Door != null && doors != null && _opened != step.Door)
            {
                // A door in the way: open it, and wait for it to swing clear.
                Face(step.Position - Position);
                if (!doors.IsOpen(step.Door))
                {
                    doors.Open(step.Door);
                    _doorWait = DoorSeconds;
                }

                _opened = step.Door;
            }

            if (_doorWait > 0f)
            {
                _doorWait -= deltaTime;
                return;
            }

            _current = Acceleration > 0f ? Math.Min(_current + (Acceleration * deltaTime), Speed) : Speed;
            if (Acceleration > 0f)
            {
                // Slow for the end of the way, so as to stop on it rather than overshoot.
                var braking = MathF.Sqrt(2f * Acceleration * Remaining) + 0.5f;
                _current = Math.Min(_current, braking);
            }

            var travel = _current * deltaTime;
            while (travel > 0f && !Arrived)
            {
                var target = _path.Stops[_next].Position;
                var offset = target - Position;
                var distance = offset.Length();
                if (distance > 1e-5f)
                {
                    Face(offset);
                }

                if (distance > travel)
                {
                    Position += offset * (travel / distance);
                    Walked += travel;
                    Moving = true;
                    break;
                }

                Position = target;
                Walked += distance;
                travel -= distance;
                Moving = Moving || distance > 1e-5f;
                Reached(_path.Stops[_next], doors);
                _next++;
                if (!Arrived && _path.Stops[_next].Door != null)
                {
                    // Another door: stop at it, and open it next tick.
                    break;
                }
            }

            if (Arrived)
            {
                _current = 0f;
            }
        }

        // Through a door to this stop: shut it behind them, if they're that sort.
        private void Reached(RouteStop stop, IDoors doors)
        {
            if (stop.Door != null)
            {
                if (ShutsDoors && doors != null)
                {
                    doors.Shut(stop.Door);
                }

                _opened = null;
            }
        }
    }
}
