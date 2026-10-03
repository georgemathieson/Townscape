using System;
using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Simulation.Weather
{
    /// <summary>One lightning strike: where and when, how bright, and whether its bolt is seen.</summary>
    public sealed class Strike
    {
        private readonly float[] _strokeTimes;

        public Strike(int id, float time, Vector2 position, float distance, float brightness, bool hasBolt, int seed, float[] strokeTimes)
        {
            Id = id;
            Time = time;
            Position = position;
            Distance = distance;
            Brightness = brightness;
            HasBolt = hasBolt;
            Seed = seed;
            _strokeTimes = strokeTimes;
        }

        public int Id { get; }

        public float Time { get; }

        /// <summary>Where the bolt meets the ground: x east, y north.</summary>
        public Vector2 Position { get; }

        /// <summary>Distance from the listener when it struck, in metres.</summary>
        public float Distance { get; }

        /// <summary>Peak brightness of the flash, from 0 to 1.</summary>
        public float Brightness { get; }

        /// <summary>Whether the bolt itself is visible, rather than only the flash in the clouds.</summary>
        public bool HasBolt { get; }

        public int Seed { get; }

        /// <summary>When the thunder reaches the listener.</summary>
        public float ThunderTime => Time + (Distance / LightningStorm.SpeedOfSound);

        /// <summary>When the flash has completely faded.</summary>
        public float EndTime => Time + _strokeTimes[_strokeTimes.Length - 1] + LightningStorm.StrokeFade * 6f;

        /// <summary>
        /// The flash at <paramref name="time"/>, from 0 to <see cref="Brightness"/>. A strike is a
        /// few return strokes in quick succession, each a sharp flash that fades in tens of
        /// milliseconds, which gives lightning its flicker.
        /// </summary>
        public float FlashAt(float time)
        {
            var since = time - Time;
            if (since < 0f)
            {
                return 0f;
            }

            var flash = 0f;
            for (var i = 0; i < _strokeTimes.Length; i++)
            {
                var age = since - _strokeTimes[i];
                if (age < 0f)
                {
                    continue;
                }

                // Later strokes are a little weaker.
                var stroke = MathF.Exp(-age / LightningStorm.StrokeFade) * (1f - (0.18f * i));
                flash = MathF.Max(flash, stroke);
            }

            return flash * Brightness;
        }
    }

    /// <summary>Thunder arriving at the listener.</summary>
    public readonly struct Thunder
    {
        public Thunder(int strikeId, float time, float distance, Vector2 position = default)
        {
            StrikeId = strikeId;
            Time = time;
            Distance = distance;
            Position = position;
        }

        public int StrikeId { get; }

        public float Time { get; }

        public float Distance { get; }

        /// <summary>Where the strike was (x east, y north), so the sound can come from that direction.</summary>
        public Vector2 Position { get; }

        /// <summary>From 0 to 1: close strikes are loud.</summary>
        public float Loudness => 1f / (1f + (Distance / 450f));

        /// <summary>Close strikes crack; distant ones only rumble.</summary>
        public bool IsCrack => Distance < 700f;

        /// <summary>Rumbles last longer from further away, as the sound arrives from along the whole bolt.</summary>
        public float Duration => 2.5f + (Distance / 500f);
    }

    /// <summary>
    /// Schedules lightning: strikes arrive at random (a Poisson process at the current rate),
    /// each flashes for a fraction of a second, and its thunder arrives later, delayed by
    /// distance at the speed of sound. Deterministic for a given seed.
    /// </summary>
    public sealed class LightningStorm
    {
        public const float SpeedOfSound = 343f;
        public const float StrokeFade = 0.05f;
        public const float MinDistance = 180f;
        public const float MaxDistance = 2600f;

        // Keeps flashes distinct even when the rate is high.
        private const float MinGap = 1.5f;

        private readonly Random _random;
        private readonly List<Strike> _active = new List<Strike>();
        private readonly List<Strike> _awaitingThunder = new List<Strike>();
        private float _lastStrikeTime = float.NegativeInfinity;
        private int _nextId;

        public LightningStorm(int seed = 7)
        {
            _random = new Random(seed);
        }

        /// <summary>Strikes whose flash has not yet faded.</summary>
        public IReadOnlyList<Strike> Active => _active;

        /// <summary>
        /// Moves the storm on to <paramref name="time"/>, <paramref name="deltaTime"/> seconds after
        /// the last step. New strikes are added to <paramref name="strikes"/> and thunder that has
        /// reached the listener to <paramref name="thunder"/>.
        /// </summary>
        /// <param name="listenerForward">Where the listener is looking, so visible bolts favour the view.</param>
        public void Step(float time, float deltaTime, float strikesPerMinute, Vector2 listener, Vector2 listenerForward, List<Strike> strikes, List<Thunder> thunder)
        {
            var chance = 1f - MathF.Exp(-MathF.Max(0f, strikesPerMinute) * deltaTime / 60f);
            if (time - _lastStrikeTime >= MinGap && _random.NextDouble() < chance)
            {
                strikes.Add(StrikeNow(time, listener, listenerForward));
            }

            _active.RemoveAll(strike => strike.EndTime < time);
            for (var i = _awaitingThunder.Count - 1; i >= 0; i--)
            {
                var strike = _awaitingThunder[i];
                if (strike.ThunderTime <= time)
                {
                    thunder.Add(new Thunder(strike.Id, strike.ThunderTime, strike.Distance, strike.Position));
                    _awaitingThunder.RemoveAt(i);
                }
            }
        }

        /// <summary>Strikes immediately, whatever the rate.</summary>
        public Strike StrikeNow(float time, Vector2 listener, Vector2 listenerForward)
        {
            // Bias distance towards the near end of the range, so most strikes are a few hundred metres to a kilometre away.
            var t = (float)_random.NextDouble();
            var distance = MinDistance + ((MaxDistance - MinDistance) * t * t);

            // Visible bolts mostly appear somewhere in front of the listener.
            var hasBolt = distance < 1800f && _random.NextDouble() < 0.5;
            var forward = listenerForward.LengthSquared() > 1e-6f ? Vector2.Normalize(listenerForward) : Vector2.UnitY;
            var heading = MathF.Atan2(forward.X, forward.Y);
            var spread = hasBolt && _random.NextDouble() < 0.7 ? 0.6f : MathF.PI;
            var bearing = heading + (((float)_random.NextDouble() * 2f) - 1f) * spread;
            var position = listener + (new Vector2(MathF.Sin(bearing), MathF.Cos(bearing)) * distance);

            var brightness = Math.Clamp(1.25f - (distance / MaxDistance), 0.3f, 1f) * (0.75f + (0.25f * (float)_random.NextDouble()));
            var strokes = new float[1 + _random.Next(4)];
            var at = 0f;
            for (var i = 0; i < strokes.Length; i++)
            {
                strokes[i] = at;
                at += 0.05f + (0.1f * (float)_random.NextDouble());
            }

            var strike = new Strike(_nextId++, time, position, distance, brightness, hasBolt, _random.Next(), strokes);
            _active.Add(strike);
            _awaitingThunder.Add(strike);
            _lastStrikeTime = time;
            return strike;
        }

        /// <summary>The combined flash of every active strike, from 0 to 1.</summary>
        public float Flash(float time)
        {
            var flash = 0f;
            foreach (var strike in _active)
            {
                flash = MathF.Max(flash, strike.FlashAt(time));
            }

            return flash;
        }
    }
}
