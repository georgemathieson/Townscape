using Townscape.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Editor
{
    internal static class TownscapeMenu
    {
        [MenuItem("Townscape/Open Town Scene", priority = 0)]
        private static void OpenTownScene()
        {
            TownscapeProjectSetup.OpenTownScene();
        }

        [MenuItem("Townscape/Rebuild Town", priority = 1)]
        private static void RebuildTown()
        {
#if UNITY_2022_2_OR_NEWER
            var bootstrap = Object.FindFirstObjectByType<TownscapeBootstrap>();
#else
            var bootstrap = Object.FindObjectOfType<TownscapeBootstrap>();
#endif
            if (bootstrap == null)
            {
                Debug.LogWarning("[Townscape] No TownscapeBootstrap in the open scene. Open the town scene first.");
                return;
            }

            bootstrap.Rebuild();
            SceneView.RepaintAll();
        }

        /// <summary>Recreates the town scene from scratch, in case the saved one is ever lost or broken.</summary>
        [MenuItem("Townscape/Create Town Scene", priority = 50)]
        private static void CreateTownScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Townscape").AddComponent<TownscapeBootstrap>();

            // Edit-mode look until play mode takes over the environment.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.26f, 0.27f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.28f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.07f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.30f, 0.27f, 0.34f);
            RenderSettings.fogDensity = 0.0095f;
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, TownscapeProjectSetup.ScenePath);
            Debug.Log($"[Townscape] Created {TownscapeProjectSetup.ScenePath}.");
        }
    }
}
