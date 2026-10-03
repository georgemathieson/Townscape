// Compile-check stubs for com.unity.render-pipelines.core / universal (URP 17). Not used by Unity.
#pragma warning disable
using System;
using UnityEngine;

namespace UnityEngine.Rendering
{
    public abstract class VolumeParameter
    {
        public bool overrideState { get; set; }
    }

    public class VolumeParameter<T> : VolumeParameter
    {
        public virtual T value { get; set; }
        public void Override(T x) { overrideState = true; value = x; }
    }

    public class FloatParameter : VolumeParameter<float> { }
    public class MinFloatParameter : FloatParameter { }
    public class ClampedFloatParameter : FloatParameter { }
    public class ColorParameter : VolumeParameter<Color> { }

    public class VolumeComponent : ScriptableObject
    {
        public bool active = true;
    }

    public sealed class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides = false) where T : VolumeComponent => throw new NotImplementedException();
        public bool TryGet<T>(out T component) where T : VolumeComponent => throw new NotImplementedException();
    }

    public class Volume : MonoBehaviour
    {
        public bool isGlobal = true;
        public float priority;
        public float weight = 1f;
        public VolumeProfile sharedProfile;
        public VolumeProfile profile { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public enum TonemappingMode { None, Neutral, ACES }

    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }

    public sealed class Tonemapping : VolumeComponent
    {
        public TonemappingModeParameter mode;
    }

    public sealed class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold;
        public MinFloatParameter intensity;
        public ClampedFloatParameter scatter;
        public ColorParameter tint;
    }

    public sealed class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure;
        public ClampedFloatParameter contrast;
        public ColorParameter colorFilter;
        public ClampedFloatParameter hueShift;
        public ClampedFloatParameter saturation;
    }

    public sealed class Vignette : VolumeComponent
    {
        public ColorParameter color;
        public ClampedFloatParameter intensity;
        public ClampedFloatParameter smoothness;
    }

    public sealed class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
        public bool renderShadows { get; set; }
    }

    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera) => throw new NotImplementedException();
    }

    public enum RenderingMode { Forward = 0, Deferred = 1, ForwardPlus = 2 }

    public abstract class ScriptableRendererData : ScriptableObject { }

    public class UniversalRendererData : ScriptableRendererData
    {
        public RenderingMode renderingMode { get; set; }
    }

    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public static UniversalRenderPipelineAsset Create(ScriptableRendererData rendererData = null) => throw new NotImplementedException();
        public float shadowDistance { get; set; }
        public int shadowCascadeCount { get; set; }
        public bool supportsHDR { get; set; }
        public int msaaSampleCount { get; set; }
        public bool supportsCameraDepthTexture { get; set; }
        public bool supportsCameraOpaqueTexture { get; set; }
        protected override RenderPipeline CreatePipeline() => throw new NotImplementedException();
    }
}
