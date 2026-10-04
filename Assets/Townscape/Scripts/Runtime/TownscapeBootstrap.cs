using System;
using System.Collections.Generic;
using System.Diagnostics;
using Townscape.Generation;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Layout;
using Townscape.Runtime.Audio;
using Townscape.Runtime.CoffeeShop;
using Townscape.Runtime.Controls;
using Townscape.Runtime.Lighting;
using Townscape.Runtime.Network;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.Security;
using Townscape.Runtime.UI;
using Townscape.Runtime.Walking;
using Townscape.Runtime.Weather;
using Townscape.State;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
    /// In play mode it also creates the camera, post-processing, fog, the storm, shortcuts, the control
    /// panel and the Fellside Coffee game.
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
        [Tooltip("Start each session with the time, weather and sound it was left at. Turn off to always start at the initial time preset.")]
        private bool rememberSettings = true;

        [SerializeField]
        [Tooltip("Log every store action to the console.")]
        private bool logActions = true;

        [SerializeField] private Vector3 cameraStartPosition = new Vector3(-30f, 7f, -30f);
        [SerializeField] private Vector3 cameraStartLookAt = new Vector3(0f, 1f, 0f);

        [Header("Sounds (optional)")]
        [SerializeField]
        [Tooltip("Recordings to use instead of the sounds made in code. Leave any slot empty to keep the generated sound.")]
        private TownAudioClips sounds = new TownAudioClips();

        private readonly List<Object> _owned = new List<Object>();
        private GameObject _root;
        private MaterialLibrary _materials;
        private IReadOnlyList<SpawnedMesh> _spawned;
        private IReadOnlyList<SwingingDoor> _doors;
        private bool _builtForPlayMode;
        private Type _lastLoggedType;
        private float _lastLoggedAt;
        private IAction _unlogged;

        public Store<TownState> Store { get; private set; }

        public GeneratedTown Town { get; private set; }

        public TimeOfDayLighting Lighting { get; private set; }

        public TownLights Lights { get; private set; }

        /// <summary>How long the last build took, shown in the performance readout.</summary>
        public float BuildMilliseconds { get; private set; }

        /// <summary>The storm, in play mode only. Audio listens to its thunder.</summary>
        public StormSystem Storm { get; private set; }

        /// <summary>The coffee shop management game, in play mode only.</summary>
        public CoffeeShopGame CoffeeShop { get; private set; }

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

            if (_unlogged != null && Time.unscaledTime - _lastLoggedAt >= LogQuietSeconds)
            {
                Log(_unlogged);
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

            var initialState = TownState.CreateDefault(initialTimePreset);
            if (playing && rememberSettings)
            {
                initialState = SettingsMemory.Load(initialState);
            }

            Store = new Store<TownState>(TownReducer.Reduce, initialState);
            if (playing && logActions)
            {
                Store.ActionProcessed += (action, _) => LogAction(action);
            }

            Town = new TownGenerator().Generate(new LakeDistrictVillageLayout().Create());
            _materials = new MaterialLibrary();
            _spawned = TownMeshSpawner.Spawn(Town, _materials, _root.transform, hideFlags, _owned);
            _doors = SwingingDoor.SpawnAll(Town, _materials, _root.transform, hideFlags, _owned, solid: playing);

            var sun = CreateSunAndMoon(hideFlags);
            Lighting = TownMeshSpawner.CreateChild("Time Of Day", _root.transform, hideFlags).AddComponent<TimeOfDayLighting>();
            Lighting.Initialize(Store, sun, LightingProfile.CreateStorm(), controlEnvironment: playing);
            Lights = TownMeshSpawner.CreateChild("Lights", _root.transform, hideFlags).AddComponent<TownLights>();
            Lights.Initialize(Town, _materials, Lighting, hideFlags);

            if (playing)
            {
                BuildPlayModeSystems(hideFlags);
            }

            BuildMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.Log($"[Townscape] Built the town in {stopwatch.ElapsedMilliseconds} ms ({(playing ? "play mode" : "edit-mode preview")}).");
        }

        private void BuildPlayModeSystems(HideFlags hideFlags)
        {
            var postProcessing = PostProcessingRig.Create(_root.transform, hideFlags, _owned);
            var camera = CreateCamera(hideFlags);
            Lighting.AttachCamera(camera, postProcessing.ColorAdjustments);

            var input = TownscapeInput.CreateDefault();
            var ground = Town.Context.Ground;
            var terrain = Town.Context.Terrain;
            var core = Town.Context.Settings.CoreHalfExtent;
            var waterLevel = Town.Context.Layout.WaterLevel;
            float GroundHeight(Vector2 flat)
            {
                var p = new System.Numerics.Vector2(flat.x, flat.y);
                return Mathf.Abs(flat.x) < core && Mathf.Abs(flat.y) < core ? ground.HeightAt(p) : terrain.FarHeightAt(p);
            }

            var colliders = TownMeshSpawner.CreateChild("Colliders", _root.transform, hideFlags).AddComponent<TownColliders>();
            colliders.Initialize(_spawned, Town.Anchors, hideFlags);
            var flyCamera = camera.gameObject.AddComponent<FreeFlyCamera>();
            flyCamera.Initialize(
                input,
                flat => Mathf.Max(ground.HeightAt(new System.Numerics.Vector2(flat.x, flat.y)), waterLevel));

            Storm = TownMeshSpawner.CreateChild("Storm", _root.transform, hideFlags).AddComponent<StormSystem>();
            Storm.Initialize(Store, Town, _materials, Lighting, camera.transform, _spawned, hideFlags);

            var audio = TownMeshSpawner.CreateChild("Audio", _root.transform, hideFlags).AddComponent<TownAudio>();
            audio.Initialize(Store, Storm, Town.Context.Layout, camera.transform, sounds, hideFlags);

            CoffeeShop = TownMeshSpawner.CreateChild("Fellside Coffee", _root.transform, hideFlags).AddComponent<CoffeeShopGame>();
            CoffeeShop.Initialize(
                flyCamera,
                ShopLocator.Find(Town.Context.Buildings, VillageShops.FellsideCoffee)?.Footprint,
                flat => ground.HeightAt(new System.Numerics.Vector2(flat.x, flat.y)),
                logActions);

            var controls = TownMeshSpawner.CreateChild("Controls", _root.transform, hideFlags);
            var panel = controls.AddComponent<ControlPanel>();
            panel.Initialize(Store, Lighting, Storm, CoffeeShop);
            controls.AddComponent<CoffeeShopPaper>().Initialize(CoffeeShop);
            var performance = controls.AddComponent<PerformanceOverlay>();
            performance.Initialize(Storm, Lights, () => BuildMilliseconds);
            var walking = TownMeshSpawner.CreateChild("Walker", _root.transform, hideFlags).AddComponent<WalkingController>();
            walking.Initialize(camera.transform, flyCamera, input, colliders, panel, CoffeeShop, GroundHeight, waterLevel);
            panel.Walking = walking;
            controls.AddComponent<WalkingHud>().Initialize(walking);
            var alarms = AlarmSystem.CreateAll(Town, _materials, _root.transform, hideFlags, walking, _doors);
            var windows = new List<IScreenWindow>(alarms);
            var cabinet = StreetCabinetSystem.Create(Town, _materials, _root.transform, hideFlags, walking, alarms);
            if (cabinet != null)
            {
                windows.Add(cabinet);
            }

            controls.AddComponent<TownscapeShortcuts>().Initialize(Store, input, Lighting, panel, performance, CoffeeShop, walking, windows);
            if (rememberSettings)
            {
                controls.AddComponent<SettingsMemory>().Initialize(Store);
            }
        }

        // A slider sends a stream of actions; log the first, then at most a few a second, and always the last.
        private const float LogQuietSeconds = 0.3f;

        private void LogAction(IAction action)
        {
            if (action.GetType() == _lastLoggedType && Time.unscaledTime - _lastLoggedAt < LogQuietSeconds)
            {
                _unlogged = action;
                return;
            }

            if (_unlogged != null && _unlogged.GetType() != action.GetType())
            {
                Log(_unlogged);
            }

            Log(action);
        }

        private void Log(IAction action)
        {
            Debug.Log($"[Townscape] {action}");
            _lastLoggedType = action.GetType();
            _lastLoggedAt = Time.unscaledTime;
            _unlogged = null;
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
            _spawned = null;
            Storm = null;
            CoffeeShop = null;
            _materials?.Dispose();
            _materials = null;
            Lighting = null;
            Lights = null;
            Town = null;
            Store = null;
        }
    }
}
