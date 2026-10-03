using System;
using System.Collections.Generic;
using Townscape.Generation.Geometry;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Townscape.Runtime.Rendering
{
    /// <summary>
    /// Creates one URP Lit material per <see cref="SurfaceMaterial"/>, coloured from the shared
    /// <see cref="SurfacePalette"/>. Later milestones swap the shader for a custom one (wet
    /// surfaces, rain ripples) without the generators noticing.
    /// </summary>
    public sealed class MaterialLibrary : IDisposable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int PreserveSpecularId = Shader.PropertyToID("_BlendModePreserveSpecular");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        private readonly Dictionary<SurfaceMaterial, Material> _materials = new Dictionary<SurfaceMaterial, Material>();
        private readonly Shader _shader;

        public MaterialLibrary()
        {
            _shader = Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null)
            {
                Debug.LogWarning("[Townscape] URP Lit shader not found; is the Universal Render Pipeline installed and active? Falling back to Standard.");
                _shader = Shader.Find("Standard");
            }
        }

        public Material Get(SurfaceMaterial surface)
        {
            if (!_materials.TryGetValue(surface, out var material))
            {
                material = Create(surface);
                _materials.Add(surface, material);
            }

            return material;
        }

        public void Dispose()
        {
            foreach (var material in _materials.Values)
            {
                ObjectUtility.Destroy(material);
            }

            _materials.Clear();
        }

        private Material Create(SurfaceMaterial surface)
        {
            var appearance = SurfacePalette.Get(surface);
            var colour = new Color(appearance.R, appearance.G, appearance.B, appearance.Alpha);
            var material = new Material(_shader)
            {
                name = $"Townscape {surface}",
                hideFlags = HideFlags.DontSave,
                enableInstancing = true,
            };

            SetColorIfPresent(material, BaseColorId, colour);
            SetColorIfPresent(material, ColorId, colour);
            SetFloatIfPresent(material, SmoothnessId, appearance.Smoothness);
            SetFloatIfPresent(material, GlossinessId, appearance.Smoothness);
            SetFloatIfPresent(material, MetallicId, 0f);
            if (appearance.IsTransparent)
            {
                MakeTransparent(material);
            }

            return material;
        }

        /// <summary>
        /// Switches a URP Lit material to alpha-blended transparency. Setting properties from code
        /// skips the material inspector's own setup, so the keywords and blend states are set here.
        /// </summary>
        private static void MakeTransparent(Material material)
        {
            SetFloatIfPresent(material, SurfaceId, 1f);
            SetFloatIfPresent(material, BlendId, 0f);
            SetFloatIfPresent(material, PreserveSpecularId, 0f);
            SetFloatIfPresent(material, SrcBlendId, (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, SrcBlendAlphaId, (float)BlendMode.One);
            SetFloatIfPresent(material, DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, ZWriteId, 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetColorIfPresent(Material material, int id, Color value)
        {
            if (material.HasProperty(id))
            {
                material.SetColor(id, value);
            }
        }

        private static void SetFloatIfPresent(Material material, int id, float value)
        {
            if (material.HasProperty(id))
            {
                material.SetFloat(id, value);
            }
        }
    }

    internal static class ObjectUtility
    {
        /// <summary>Destroys in play mode, destroys immediately in edit mode.</summary>
        public static void Destroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
