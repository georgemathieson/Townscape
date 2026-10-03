using System.Collections.Generic;
using System.Diagnostics;
using Townscape.Generation;
using Townscape.Generation.Layout;
using Townscape.Runtime.Controls;
using Townscape.Runtime.Lighting;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.UI;
using Townscape.State;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;

namespace Townscape.Runtime
{
    /// <summary>
    /// Composition root: the only object saved in the scene. It creates the store, generates the
    /// town and wires every system together. Generated objects are never saved; the town is
    /// rebuilt from code whenever the scene opens or play mode starts.
    /// </summary>
    /// <remarks>
    /// In edit mode it builds a preview (geometry, the sun and the town's lights) so the Scene view
    /// shows the town.
    /// In play mode it also creates the camera, post-processing, fog, shortcuts and help overlay.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TownscapeBootstrap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Time of day when the scene starts.")]
        private TimePreset initialTimePreset = TimePreset.Dusk;

        [SerializeField]
        [Tooltip("Generate the town in the Scene view outside play mode.")]
        private bool previewInEditMode = true;

        [SerializeField]
        [Tooltip("Log every store action to the console.")]
        private bool logActions = true;

        [SerializeField] private Vector3 cameraStartPosition = new Vector3(-30f, 7f, -30f);
        [SerializeField] private Vector3 cameraStartLookAt = new Vector3(0f, 1f, 0f);

        private readonly List<Object> _owned = new List<Object>();
        private GameObject _root;
        private MaterialLibrary _materials;
        private bool _builtForPlayMode;

        public Store<TownState> Store { get; private set; }

        public GeneratedTown Town { get; private set; }

        public TimeOfDayLighting Lighting { get; private set; }

        public TownLights Lights { get; private set; }

        /// <summary>Throws away everything generated and builds it again.</summary>
        public void Rebuild()
        {
            Teardown();
            Build();
        }

        private void OnEnable()
        {
            if (Application.isPlaying || previewInEditMode)
            {
                Build();
            }
        }

        private void OnDisable()
        {
            Teardown();
        }

        private void Update()
        {
            // With "Enter Play Mode Options" the edit-mode preview can survive into play mode
            // without OnEnable running again; rebuild so the play-mode systems exist.
            if (_root != null && _builtForPlayMode != Application.isPlaying)
            {
                Rebuild();
            }
        }

        private void Build()
        {
            Teardown();

            var playing = Application.isPlaying;
            var hideFlags = playing ? HideFlags.None : HideFlags.DontSave;
            var stopwatch = Stopwatch.StartNew();

            _builtForPlayMode = playing;
            _root = TownMeshSpawner.CreateChild("Generated Town", transform, hideFlags);

            Store = new Store<TownState>(TownReducer.Reduce, TownState.CreateDefault(initialTimePreset));
            if (playing && logActions)
            {
                Store.ActionProcessed += (action, _) => Debug.Log($"[Townscape] {action}");
            }

            Town = new TownGenerator().Generate(new LakeDistrictVillageLayout().Create());
            _materials = new MaterialLibrary();
            TownMeshSpawner.Spawn(Town, _materials, _root.transform, hideFlags, _owned);

            var sun = CreateSunAndMoon(hideFlags);
            Lighting = TownMeshSpawner.CreateChild("Time Of Day", _root.transform, hideFlags).AddComponent<TimeOfDayLighting>();
            Lighting.Initialize(Store, sun, LightingProfile.CreateStorm(), controlEnvironment: playing);
            Lights = TownMeshSpawner.CreateChild("Lights", _root.transform, hideFlags).AddComponent<TownLights>();
            Lights.Initialize(Town, _materials, Lighting, hideFlags);

            if (playing)
            {
                BuildPlayModeSystems(hideFlags);
            }

            Debug.Log($"[Townscape] Built the town in {stopwatch.ElapsedMilliseconds} ms ({(playing ? "play mode" : "edit-mode preview")}).");
        }

        private void BuildPlayModeSystems(HideFlags hideFlags)
        {
            var postProcessing = PostProcessingRig.Create(_root.transform, hideFlags, _owned);
            var camera = CreateCamera(hideFlags);
            Lighting.AttachCamera(camera, postProcessing.ColorAdjustments);

            var input = TownscapeInput.CreateDefault();
            var ground = Town.Context.Ground;
            var waterLevel = Town.Context.Layout.WaterLevel;
            camera.gameObject.AddComponent<FreeFlyCamera>().Initialize(
                input,
                flat => Mathf.Max(ground.HeightAt(new System.Numerics.Vector2(flat.x, flat.y)), waterLevel));

            var controls = TownMeshSpawner.CreateChild("Controls", _root.transform, hideFlags);
            var help = controls.AddComponent<HelpOverlay>();
            help.Initialize(Store, Lighting);
            controls.AddComponent<TownscapeShortcuts>().Initialize(Store, input, Lighting, help);
        }

        private Light CreateSunAndMoon(HideFlags hideFlags)
        {
            var light = TownMeshSpawner.CreateChild("Sun & Moon", _root.transform, hideFlags).AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            return light;
        }

        private Camera CreateCamera(HideFlags hideFlags)
        {
            var cameraObject = TownMeshSpawner.CreateChild("Camera", _root.transform, hideFlags);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(
                cameraStartPosition,
                Quaternion.LookRotation(cameraStartLookAt - cameraStartPosition, Vector3.up));

            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1500f;
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private void Teardown()
        {
            if (_root != null)
            {
                ObjectUtility.Destroy(_root);
                _root = null;
            }

            foreach (var owned in _owned)
            {
                ObjectUtility.Destroy(owned);
            }

            _owned.Clear();
            _materials?.Dispose();
            _materials = null;
            Lighting = null;
            Lights = null;
            Town = null;
            Store = null;
        }
    }
}
