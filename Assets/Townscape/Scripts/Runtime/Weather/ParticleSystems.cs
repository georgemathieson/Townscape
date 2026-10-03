using Townscape.Runtime.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.Weather
{
    /// <summary>Particle systems set up from code for the weather effects.</summary>
    internal static class ParticleSystems
    {
        /// <summary>
        /// A stopped, world-space system with no emission of its own: effects either set a rate
        /// or place each particle themselves with <c>Emit</c>.
        /// </summary>
        public static ParticleSystem Create(string name, Transform parent, HideFlags hideFlags, int maxParticles, Material material)
        {
            var system = TownMeshSpawner.CreateChild(name, parent, hideFlags).AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.enabled = false;

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        /// <summary>Fades particles in, holds them, then fades them out over their lives.</summary>
        public static void FadeInAndOut(ParticleSystem system, float fadeIn, float fadeOut)
        {
            var colour = system.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, 1f - fadeOut), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
        }
    }
}
