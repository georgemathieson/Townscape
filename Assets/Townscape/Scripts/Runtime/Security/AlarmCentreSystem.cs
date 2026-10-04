using System;
using System.Collections.Generic;
using System.Linq;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.UI;
using Townscape.Runtime.Walking;
using Townscape.Simulation.Audio;
using Townscape.Simulation.Security;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Townscape.Runtime.Security
{
    /// <summary>
    /// The alarm receiving centre at Mill Works, in play. Every building's alarm reports to it
    /// over its broadband (<see cref="AlarmReceivingCentre"/>); its wall of screens shows each
    /// site at a glance, it chimes while an alarm waits to be acknowledged, and any operator's
    /// console brings up the screen where you acknowledge alarms, send a guard, call the police
    /// and close them, with the log of everything the sites have reported.
    /// </summary>
    /// <remarks>
    /// Like the alarms' own state, the centre's lives here rather than in the store. For now the
    /// guard and police are timed by <see cref="AlarmReceivingCentre"/>; people walking the
    /// streets can take their place later.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed partial class AlarmCentreSystem : MonoBehaviour, IScreenWindow
    {
        private const float ChimeEverySeconds = 5f;

        private static readonly Color TileQuiet = new Color(0.08f, 0.35f, 0.14f);
        private static readonly Color TileSet = new Color(0.08f, 0.16f, 0.45f);
        private static readonly Color TileAlarm = new Color(1.6f, 0.08f, 0.05f);
        private static readonly Color TileFault = new Color(1.1f, 0.55f, 0.04f);
        private static readonly Color TileIdle = new Color(0.04f, 0.06f, 0.1f);

        private readonly List<AlarmGlow> _tiles = new List<AlarmGlow>();
        private readonly List<Object> _owned = new List<Object>();

        private TownAlarmCentre _spec;
        private AlarmReceivingCentre _centre;
        private WalkingController _walking;
        private Func<float> _hour;
        private AudioSource _speaker;
        private AudioClip _chime;
        private float _nextChime;

        public AlarmReceivingCentre Centre => _centre;

        /// <summary>Puts the town's alarm receiving centre in play, with every alarm reporting to it (null if there's none).</summary>
        public static AlarmCentreSystem Create(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, WalkingController walking, IReadOnlyList<AlarmSystem> alarms, Func<float> hour)
        {
            if (town.AlarmCentres.Count == 0)
            {
                return null;
            }

            var spec = town.AlarmCentres[0];
            var system = TownMeshSpawner.CreateChild($"Alarm Centre: {spec.Name}", parent, hideFlags).AddComponent<AlarmCentreSystem>();
            system.Initialize(spec, materials, hideFlags, walking, alarms, hour);
            return system;
        }

        private void Initialize(TownAlarmCentre spec, MaterialLibrary materials, HideFlags hideFlags, WalkingController walking, IReadOnlyList<AlarmSystem> alarms, Func<float> hour)
        {
            _spec = spec;
            _walking = walking;
            _hour = hour ?? (() => 12f);
            _centre = new AlarmReceivingCentre();
            foreach (var alarm in alarms ?? new List<AlarmSystem>())
            {
                if (alarm != null && alarm.Spec != null)
                {
                    _centre.Add(alarm.Name, alarm.Alarm, alarm.Spec.Zones.Select(z => z.Name).ToList());
                }
            }

            // The consoles: each operator's pair of monitors, to press E on.
            for (var i = 0; i < spec.Consoles.Count; i++)
            {
                var mount = spec.Consoles[i];
                var console = TownMeshSpawner.CreateChild($"Console {i + 1}", transform, hideFlags);
                console.transform.SetPositionAndRotation(ToUnity(mount.Position), Quaternion.LookRotation(ToUnity(mount.Facing), Vector3.up));
                var box = console.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0f, -0.02f);
                box.size = new Vector3(mount.Width, mount.Height, 0.08f);
                console.AddComponent<AlarmCentreConsole>().Initialize(this);
            }

            // The wall of screens: one lit tile per site.
            var screen = materials.Get(SurfaceMaterial.AlarmLed);
            foreach (var tile in spec.VideoWall)
            {
                _tiles.Add(new AlarmGlow("Video wall tile", transform, hideFlags, screen, new Color(0.2f, 0.25f, 0.3f), ToUnity(tile.Position), ToUnity(tile.Facing), new Vector3(tile.Width, tile.Height, 0.004f), _owned));
            }

            // Its chime: two falling tones, heard round the room.
            var speaker = TownMeshSpawner.CreateChild("Chime", transform, hideFlags);
            speaker.transform.position = ToUnity(spec.Room);
            _speaker = speaker.AddComponent<AudioSource>();
            _speaker.playOnAwake = false;
            _speaker.spatialBlend = 1f;
            _speaker.rolloffMode = AudioRolloffMode.Logarithmic;
            _speaker.minDistance = 4f;
            _speaker.maxDistance = 30f;
            _speaker.dopplerLevel = 0f;
            var samples = AlarmSounds.Beep(1320f, 0.16f).Concat(new float[ProceduralSounds.SampleRate / 25]).Concat(AlarmSounds.Beep(990f, 0.24f)).Select(s => s * 0.5f).ToArray();
            _chime = AudioClip.Create("Alarm centre chime", samples.Length, 1, ProceduralSounds.SampleRate, false);
            _chime.SetData(samples, 0);
            _owned.Add(_chime);
        }

        private void Update()
        {
            if (_centre == null)
            {
                return;
            }

            _centre.Tick(Time.deltaTime, _hour());

            // Chime every few seconds while an alarm waits to be acknowledged.
            if (_centre.Unacknowledged > 0 && Time.time >= _nextChime)
            {
                _speaker.PlayOneShot(_chime);
                _nextChime = Time.time + ChimeEverySeconds;
            }

            var flash = Mathf.Repeat(Time.time, 1f) < 0.5f;
            for (var i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].Set(i < _centre.Sites.Count ? TileColour(_centre.Sites[i], flash) : TileIdle);
            }

            if (WindowOpen && (_walking == null || !_walking.Active))
            {
                CloseWindow();
            }
        }

        private void OnDestroy()
        {
            if (WindowOpen)
            {
                CloseWindow();
            }

            _skin?.Dispose();
            foreach (var owned in _owned)
            {
                ObjectUtility.Destroy(owned);
            }

            _owned.Clear();
        }

        // Red and flashing in alarm, amber flashing with no signal, amber with a fault, blue while
        // set, green while unset.
        private Color TileColour(ArcSite site, bool flash)
        {
            if (!site.Online)
            {
                return flash ? TileFault : TileIdle;
            }

            if (site.State == AlarmState.Sounding || _centre.OpenIncident(site) is { Acknowledged: false })
            {
                return flash ? TileAlarm : TileAlarm * 0.25f;
            }

            if (site.Faults != AlarmFaults.None)
            {
                return TileFault;
            }

            return site.State == AlarmState.Unset ? TileQuiet : TileSet;
        }

        private static Vector3 ToUnity(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
