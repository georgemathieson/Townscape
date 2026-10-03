using System;
using System.Collections.Generic;
using Townscape.Simulation.Weather;
using UnityEngine;

namespace Townscape.Runtime.Weather
{
    /// <summary>Everything an effect needs to know about the weather this frame.</summary>
    public readonly struct WeatherFrame
    {
        public WeatherFrame(
            WeatherConditions conditions,
            float time,
            float deltaTime,
            float hour,
            float wetness,
            float flash,
            Color ambient,
            Transform camera,
            IReadOnlyList<Strike> newStrikes)
        {
            Conditions = conditions;
            Time = time;
            DeltaTime = deltaTime;
            Hour = hour;
            Wetness = wetness;
            Flash = flash;
            Ambient = ambient;
            Camera = camera;
            NewStrikes = newStrikes;
        }

        public WeatherConditions Conditions { get; }

        public float Time { get; }

        public float DeltaTime { get; }

        /// <summary>The hour shown on the clock.</summary>
        public float Hour { get; }

        /// <summary>How wet surfaces are, from 0 to 1. Lags behind the rain.</summary>
        public float Wetness { get; }

        /// <summary>Lightning brightness right now, from 0 to 1.</summary>
        public float Flash { get; }

        /// <summary>
        /// The light falling on things that aren't lit by the renderer (rain, mist, smoke): the sky's
        /// ambient light, brightened by lightning.
        /// </summary>
        public Color Ambient { get; }

        public Transform Camera { get; }

        public Vector3 CameraPosition => Camera.position;

        public IReadOnlyList<Strike> NewStrikes { get; }

        /// <summary>The wind as a Unity vector: x east, z north.</summary>
        public Vector3 Wind => new Vector3(Conditions.Wind.X, 0f, Conditions.Wind.Y);
    }

    /// <summary>
    /// Strategy for one part of the storm: rain, lightning, wet surfaces, water, mist, smoke or
    /// wind. The storm system ticks each in turn; adding an effect never touches the others.
    /// </summary>
    public interface IWeatherEffect : IDisposable
    {
        /// <summary>What the effect is, as shown in the performance readout.</summary>
        string Name { get; }

        void Tick(in WeatherFrame frame);
    }
}
