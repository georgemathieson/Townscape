using System;
using System.Collections.Generic;
using Townscape.Runtime.Rendering;
using UnityEditor;
using UnityEngine;

namespace Townscape.Editor
{
    /// <summary>
    /// The town makes all its materials in code while it runs, so a player build would otherwise
    /// leave out the shaders and shader variants they need (transparent glass, glowing windows,
    /// rippling water, weather particles, the lightning bolt). One small material per combination
    /// in a Resources folder makes Unity include them. Only missing materials are created.
    /// </summary>
    internal static class ShaderVariantKeeper
    {
        private const string Folder = "Assets/Townscape/Resources/Shader Variants";
        private const string FlatNormalPath = Folder + "/Flat Normal.asset";

        // Each keyword comes with what URP's material validation expects alongside it (a normal map,
        // a non-black emission colour), so opening a material in the inspector never strips it.
        private static readonly (string Name, string Shader, Action<Material> Setup)[] Keepers =
        {
            ("Lit", "Universal Render Pipeline/Lit", _ => { }),
            ("Lit Glowing", "Universal Render Pipeline/Lit", Glowing),
            ("Lit Rippling", "Universal Render Pipeline/Lit", Rippling),
            ("Lit Glass", "Universal Render Pipeline/Lit", MaterialLibrary.MakeTransparent),
            ("Lit Puddle", "Universal Render Pipeline/Lit", m =>
            {
                MaterialLibrary.MakeTransparent(m);
                Rippling(m);
            }),
            ("Particles", "Universal Render Pipeline/Particles/Unlit", null),
            ("Particles Soft", "Universal Render Pipeline/Particles/Unlit", null),
            ("Particles Soft Fading", "Universal Render Pipeline/Particles/Unlit", null),
            ("Lightning Bolt", "Townscape/Lightning Bolt", _ => { }),
            ("Sky", "Skybox/Panoramic", _ => { }),
        };

        public static void Ensure(List<string> changes)
        {
            var created = 0;
            foreach (var (name, shaderName, setup) in Keepers)
            {
                var path = $"{Folder}/{name}.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                {
                    continue;
                }

                var material = Create(name, shaderName, setup);
                if (material == null)
                {
                    continue;
                }

                TownscapeProjectSetup.EnsureFolder(Folder);
                material.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(material, path);
                created++;
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                changes.Add($"created {created} shader variant materials for player builds");
            }
        }

        private static void Glowing(Material material)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white);
        }

        private static void Rippling(Material material)
        {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", FlatNormal());
        }

        // A tiny normal map pointing straight out, saved next to the materials that use it.
        private static Texture2D FlatNormal()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(FlatNormalPath);
            if (existing != null)
            {
                return existing;
            }

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false, true) { name = "Flat Normal" };
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            TownscapeProjectSetup.EnsureFolder(Folder);
            AssetDatabase.CreateAsset(texture, FlatNormalPath);
            return texture;
        }

        private static Material Create(string name, string shaderName, Action<Material> setup)
        {
            // Particle materials are set up exactly as the weather effects set up theirs (without a
            // texture: the ones made at runtime can't be saved into an asset).
            switch (name)
            {
                case "Particles":
                    return EffectMaterials.CreateParticles(name, null);
                case "Particles Soft":
                    return EffectMaterials.CreateParticles(name, null, softDistance: 1f);
                case "Particles Soft Fading":
                    return EffectMaterials.CreateParticles(name, null, softDistance: 4f, cameraFade: new Vector2(3f, 12f));
            }

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[Townscape] Shader '{shaderName}' not found; player builds may be missing it.");
                return null;
            }

            var material = new Material(shader) { name = name };
            setup?.Invoke(material);
            return material;
        }
    }
}
