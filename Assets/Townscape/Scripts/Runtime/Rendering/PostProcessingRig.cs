using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Townscape.Runtime.Rendering
{
    /// <summary>A global URP volume with the cosy, slightly muted storm grade.</summary>
    public sealed class PostProcessingRig
    {
        private PostProcessingRig(Volume volume, VolumeProfile profile, ColorAdjustments colorAdjustments)
        {
            Volume = volume;
            Profile = profile;
            ColorAdjustments = colorAdjustments;
        }

        public Volume Volume { get; }

        public VolumeProfile Profile { get; }

        /// <summary>Exposed so the time-of-day system can lift exposure at night.</summary>
        public ColorAdjustments ColorAdjustments { get; }

        public static PostProcessingRig Create(Transform parent, HideFlags hideFlags, ICollection<Object> owned)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Townscape Storm Grade";
            profile.hideFlags = HideFlags.DontSave;
            owned.Add(profile);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.6f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.65f);

            var colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0f);
            colorAdjustments.contrast.Override(8f);
            colorAdjustments.saturation.Override(-8f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.45f);

            var gameObject = TownMeshSpawner.CreateChild("Post Processing", parent, hideFlags);
            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;

            return new PostProcessingRig(volume, profile, colorAdjustments);
        }
    }
}
