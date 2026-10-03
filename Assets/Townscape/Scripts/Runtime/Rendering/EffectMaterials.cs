using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.Rendering
{
    /// <summary>
    /// Materials for weather effects: URP's unlit particle shader set up from code (the material
    /// inspector normally does this), and the fog-free lightning bolt shader.
    /// </summary>
    public static class EffectMaterials
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>
        /// An unlit, alpha-blended particle material. <paramref name="softDistance"/> fades
        /// particles where they meet solid geometry, and <paramref name="cameraFade"/> fades them
        /// out as they reach the camera, so big mist puffs never show a hard edge.
        /// </summary>
        public static Material CreateParticles(string name, Texture texture, float softDistance = 0f, Vector2 cameraFade = default)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("[Townscape] URP particle shader not found; weather particles fall back to Sprites/Default.");
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader) { name = $"Townscape {name}", hideFlags = HideFlags.DontSave };
            SetTexture(material, BaseMapId, texture);
            SetTexture(material, MainTexId, texture);
            SetColour(material, Color.white);

            SetFloat(material, "_Surface", 1f);
            SetFloat(material, "_Blend", 0f);
            SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(material, "_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            if (softDistance > 0f)
            {
                material.EnableKeyword("_SOFTPARTICLES_ON");
                SetFloat(material, "_SoftParticlesEnabled", 1f);
                SetFloat(material, "_SoftParticlesNearFadeDistance", 0f);
                SetFloat(material, "_SoftParticlesFarFadeDistance", softDistance);
                material.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f / softDistance, 0f, 0f));
            }

            if (cameraFade.y > cameraFade.x)
            {
                material.EnableKeyword("_FADING_ON");
                SetFloat(material, "_CameraFadingEnabled", 1f);
                SetFloat(material, "_CameraNearFadeDistance", cameraFade.x);
                SetFloat(material, "_CameraFarFadeDistance", cameraFade.y);
                material.SetVector("_CameraFadeParams", new Vector4(cameraFade.x, 1f / (cameraFade.y - cameraFade.x), 0f, 0f));
            }

            return material;
        }

        /// <summary>The lightning bolt material, or an additive particle material if the bolt shader is missing.</summary>
        public static Material CreateBolt()
        {
            var shader = Shader.Find("Townscape/Lightning Bolt");
            if (shader != null && shader.isSupported)
            {
                return new Material(shader) { name = "Townscape Lightning Bolt", hideFlags = HideFlags.DontSave };
            }

            Debug.LogWarning("[Townscape] Lightning bolt shader not found or not supported; bolts fall back to particles and may be lost in the fog.");
            var material = CreateParticles("Lightning Bolt", Texture2D.whiteTexture);
            SetFloat(material, "_Blend", 2f);
            SetFloat(material, "_DstBlend", (float)BlendMode.One);
            return material;
        }

        /// <summary>Sets the tint of a particle or bolt material.</summary>
        public static void SetColour(Material material, Color colour)
        {
            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, colour);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, colour);
            }
        }

        private static void SetTexture(Material material, int id, Texture texture)
        {
            if (material.HasProperty(id))
            {
                material.SetTexture(id, texture);
            }
        }

        private static void SetFloat(Material material, string name, float value)
        {
            if (material.HasProperty(name))
            {
                material.SetFloat(name, value);
            }
        }
    }
}
