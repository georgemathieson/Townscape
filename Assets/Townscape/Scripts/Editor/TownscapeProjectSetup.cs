using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Townscape.Editor
{
    /// <summary>
    /// One-time project configuration that would otherwise be manual clicking: creates and assigns
    /// the URP pipeline asset, switches to linear colour and adds the town scene to the build.
    /// Runs automatically when the editor loads and is safe to run again (Townscape > Set Up Project).
    /// </summary>
    [InitializeOnLoad]
    internal static class TownscapeProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Town.unity";
        private const string SettingsFolder = "Assets/Townscape/Settings";
        private const string PipelinePath = SettingsFolder + "/Townscape URP.asset";
        private const string RendererPath = SettingsFolder + "/Townscape URP Renderer.asset";

        static TownscapeProjectSetup()
        {
            EditorApplication.delayCall += RunWhenReady;
        }

        [MenuItem("Townscape/Set Up Project", priority = 100)]
        private static void RunFromMenu()
        {
            Run(openScene: true);
        }

        private static void RunWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunWhenReady;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Run(openScene: false);
        }

        private static void Run(bool openScene)
        {
            var changes = new List<string>();
            var createdPipeline = EnsureRenderPipeline(changes);
            EnsureLinearColour(changes);
            EnsureSceneInBuild(changes);

            if (changes.Count > 0)
            {
                Debug.Log($"[Townscape] Project setup: {string.Join("; ", changes)}.");
            }

            if (openScene || (createdPipeline && IsEmptyUntitledScene()))
            {
                OpenTownScene();
            }
        }

        /// <summary>Returns true if a pipeline asset was created on this run.</summary>
        private static bool EnsureRenderPipeline(List<string> changes)
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                return false;
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var created = false;
            if (pipeline == null)
            {
                pipeline = CreatePipelineAsset();
                if (pipeline == null)
                {
                    Debug.LogError(
                        "[Townscape] Could not create a URP pipeline asset automatically. Create one with " +
                        "Assets > Create > Rendering > URP Asset (with Universal Renderer) and assign it in " +
                        "Project Settings > Graphics and Project Settings > Quality.");
                    return false;
                }

                created = true;
                changes.Add($"created {PipelinePath}");
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssignToQualityLevels(pipeline);
            changes.Add("assigned the URP pipeline asset in Graphics and Quality settings");
            return created;
        }

        private static UniversalRenderPipelineAsset CreatePipelineAsset()
        {
            EnsureFolder(SettingsFolder);

            var rendererData = CreateRendererData();
            if (rendererData == null)
            {
                return null;
            }

            rendererData.renderingMode = RenderingMode.ForwardPlus;
            EditorUtility.SetDirty(rendererData);

            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            pipeline.shadowDistance = 150f;
            pipeline.shadowCascadeCount = 2;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = true;
            AssetDatabase.CreateAsset(pipeline, PipelinePath);

            // Not all of these are public; set what exists and ignore the rest.
            var serialized = new SerializedObject(pipeline);
            SetBool(serialized, "m_SoftShadowsSupported", true);
            SetBool(serialized, "m_AdditionalLightShadowsSupported", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        /// <summary>
        /// URP's own "create renderer" helper fills in shader and post-processing references, but it
        /// is internal, so call it by reflection and fall back to a bare instance if it has moved.
        /// </summary>
        private static UniversalRendererData CreateRendererData()
        {
            try
            {
                var method = typeof(UniversalRenderPipelineAsset)
                    .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "CreateRendererAsset" && m.GetParameters().Length >= 2);

                if (method != null)
                {
                    var parameters = method.GetParameters();
                    var arguments = new object[parameters.Length];
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        if (i == 0)
                        {
                            arguments[i] = RendererPath;
                        }
                        else if (parameter.ParameterType.IsEnum)
                        {
                            arguments[i] = Enum.Parse(parameter.ParameterType, "UniversalRenderer");
                        }
                        else if (parameter.ParameterType == typeof(bool))
                        {
                            // "relativePath": false means use RendererPath exactly as given.
                            arguments[i] = false;
                        }
                        else
                        {
                            arguments[i] = parameter.HasDefaultValue ? parameter.DefaultValue : null;
                        }
                    }

                    if (method.Invoke(null, arguments) is UniversalRendererData created)
                    {
                        return created;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Townscape] URP's renderer helper failed ({exception.GetBaseException().Message}); creating the renderer directly.");
            }

            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(data, RendererPath);
            return data;
        }

        private static void AssignToQualityLevels(RenderPipelineAsset pipeline)
        {
            var current = QualitySettings.GetQualityLevel();
            for (var level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                if (QualitySettings.renderPipeline != null && QualitySettings.renderPipeline != pipeline)
                {
                    QualitySettings.renderPipeline = pipeline;
                }
            }

            QualitySettings.SetQualityLevel(current, false);
        }

        private static void EnsureLinearColour(List<string> changes)
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                return;
            }

            PlayerSettings.colorSpace = ColorSpace.Linear;
            changes.Add("switched to linear colour space");
        }

        private static void EnsureSceneInBuild(List<string> changes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                return;
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            changes.Add("added the town scene to the build");
        }

        private static bool IsEmptyUntitledScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.path) && !scene.isDirty;
        }

        internal static void OpenTownScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogWarning($"[Townscape] {ScenePath} is missing. Use Townscape > Create Town Scene to make it.");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static void SetBool(SerializedObject serialized, string propertyPath, bool value)
        {
            var property = serialized.FindProperty(propertyPath);
            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = value;
            }
        }
    }
}
