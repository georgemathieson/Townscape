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
    /// <see cref="SurfacePalette"/>. Systems change them while running (glow at night, a wet
    /// sheen in the rain, ripples on water) through the methods here, so the generators never
    /// need to know.
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
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");
        private static readonly int BumpScaleId = Shader.PropertyToID("_BumpScale");

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

        /// <summary>Turns on emission for a material so <see cref="SetEmission"/> can make it glow.</summary>
        public void EnableEmission(SurfaceMaterial surface)
        {
            var material = Get(surface);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            SetColorIfPresent(material, EmissionColorId, Color.black);
        }

        /// <summary>
        /// Sets a material's glow as a linear HDR colour. Values above 1 bloom. Written as a raw
        /// vector because <c>SetColor</c> treats colours as sRGB in a linear project, which would
        /// brighten HDR values far beyond what was asked for.
        /// </summary>
        public void SetEmission(SurfaceMaterial surface, Color linearEmission)
        {
            var material = Get(surface);
            if (material.HasProperty(EmissionColorId))
            {
                material.SetVector(EmissionColorId, new Vector4(linearEmission.r, linearEmission.g, linearEmission.b, 1f));
            }
        }

        /// <summary>Changes a material's colour, opacity and smoothness, for example to make it look wet.</summary>
        public void SetAppearance(SurfaceMaterial surface, SurfaceAppearance appearance)
        {
            var material = Get(surface);
            var colour = new Color(appearance.R, appearance.G, appearance.B, appearance.Alpha);
            SetColorIfPresent(material, BaseColorId, colour);
            SetColorIfPresent(material, ColorId, colour);
            SetFloatIfPresent(material, SmoothnessId, appearance.Smoothness);
            SetFloatIfPresent(material, GlossinessId, appearance.Smoothness);
        }

        /// <summary>
        /// Gives a material a normal map that repeats every <paramref name="tileMetres"/> metres
        /// of its texture coordinates (which are in metres). Swapping the texture each frame plays
        /// a flipbook.
        /// </summary>
        public void SetNormalMap(SurfaceMaterial surface, Texture texture, float strength, float tileMetres)
        {
            var material = Get(surface);
            if (!material.HasProperty(BumpMapId))
            {
                return;
            }

            material.EnableKeyword("_NORMALMAP");
            material.SetTexture(BumpMapId, texture);
            SetFloatIfPresent(material, BumpScaleId, strength);
            if (material.HasProperty(BaseMapId))
            {
                // URP Lit reads every map with the base map's tiling and offset.
                material.SetTextureScale(BaseMapId, Vector2.one / tileMetres);
            }
        }

        /// <summary>Slides a material's textures, for water flowing downstream.</summary>
        public void SetTextureOffset(SurfaceMaterial surface, Vector2 offset)
        {
            var material = Get(surface);
            if (material.HasProperty(BaseMapId))
            {
                material.SetTextureOffset(BaseMapId, offset);
            }
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
        public static void MakeTransparent(Material material)
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
