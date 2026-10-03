using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Townscape.Generation.Layout;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.Weather;
using Townscape.Simulation.Audio;
using Townscape.Simulation.Weather;
using Townscape.State;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Townscape.Runtime.Audio
{
    /// <summary>
    /// The village's sound: rain and wind all round, the river nearby, and thunder from the
    /// direction of each strike, arriving when the storm says the sound gets here.
    /// </summary>
    /// <remarks>
    /// Sounds made in code are synthesised on a background thread (it takes a moment), so starting
    /// play mode never stalls; each starts playing as soon as it is ready.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TownAudio : MonoBehaviour
    {
        private const int ThunderVoices = 4;
        private const float ThunderDistance = 40f;
        private const float Smoothing = 1.5f;

        private readonly List<Object> _generated = new List<Object>();
        private readonly AudioSource[] _thunder = new AudioSource[ThunderVoices];
        private IDisposable _subscription;
        private StormSystem _storm;
        private RiverSpec _river;
        private float _waterLevel;
        private Transform _listener;
        private AudioSource _rain;
        private AudioSource _wind;
        private AudioSource _riverSource;
        private AudioClip[] _cracks;
        private AudioClip[] _rumbles;
        private Task<Synthesised> _synthesis;
        private int _nextVoice;

        public void Initialize(Store<TownState> store, StormSystem storm, TownLayout layout, Transform listener, TownAudioClips clips, HideFlags hideFlags)
        {
            _storm = storm;
            _river = layout.River;
            _waterLevel = layout.WaterLevel;
            _listener = listener;
            clips ??= new TownAudioClips();

            _rain = Loop("Rain", spatial: false, hideFlags);
            _wind = Loop("Wind", spatial: false, hideFlags);
            _riverSource = Loop("River", spatial: true, hideFlags);
            _riverSource.minDistance = 6f;
            _riverSource.maxDistance = 70f;
            _cracks = Assigned(clips.thunderCracks);
            _rumbles = Assigned(clips.thunderRumbles);

            // Recordings play straight away; anything missing is made on a background thread.
            // (Unity's null check, which also catches empty slots in the inspector, has to run here.)
            var needed = new Needed(clips.rainLoop == null, clips.windLoop == null, clips.riverLoop == null, _cracks.Length == 0, _rumbles.Length == 0);
            Play(_rain, clips.rainLoop);
            Play(_wind, clips.windLoop);
            Play(_riverSource, clips.riverLoop);
            if (needed.Any)
            {
                _synthesis = Task.Run(() => Synthesised.Make(needed));
            }

            for (var i = 0; i < _thunder.Length; i++)
            {
                var voice = TownMeshSpawner.CreateChild($"Thunder {i}", transform, hideFlags).AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = 0.7f;
                voice.rolloffMode = AudioRolloffMode.Linear;
                voice.minDistance = ThunderDistance;
                voice.maxDistance = 600f;
                voice.dopplerLevel = 0f;
                _thunder[i] = voice;
            }

            _storm.ThunderArrived += OnThunder;
            _subscription = store.Subscribe(state => state.Audio, audio => AudioListener.volume = audio.EffectiveVolume);
        }

        private void Update()
        {
            if (_storm == null || _listener == null)
            {
                return;
            }

            if (_synthesis != null && _synthesis.IsCompleted)
            {
                UseSynthesised(_synthesis);
                _synthesis = null;
            }

            var conditions = _storm.Conditions;
            var step = Smoothing * Time.unscaledDeltaTime;
            _rain.volume = Mathf.MoveTowards(_rain.volume, AudioMix.RainVolume(conditions.Rain), step);
            _wind.volume = Mathf.MoveTowards(_wind.volume, AudioMix.WindVolume(conditions.WindSpeed), step);
            _wind.pitch = Mathf.MoveTowards(_wind.pitch, AudioMix.WindPitch(conditions.WindSpeed), step * 0.2f);
            _riverSource.volume = AudioMix.RiverVolume(conditions.Rain);

            // The river is heard from the nearest point on its course.
            var listener = _listener.position;
            var nearest = _river.Centre.Closest(new System.Numerics.Vector2(listener.x, listener.z)).Point;
            _riverSource.transform.position = new Vector3(nearest.X, _waterLevel, nearest.Y);
        }

        private void OnThunder(Thunder thunder)
        {
            var close = thunder.IsCrack;
            var clips = close ? _cracks : _rumbles;
            var variant = AudioMix.Variant(thunder.StrikeId, clips.Length);
            if (variant < 0 || _listener == null)
            {
                return;
            }

            // Placed a little way off towards the strike, so it comes from the right direction.
            var listener = _listener.position;
            var toStrike = new Vector3(thunder.Position.X - listener.x, 0f, thunder.Position.Y - listener.z);
            var direction = toStrike.sqrMagnitude > 1f ? toStrike.normalized : Vector3.forward;

            var voice = _thunder[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _thunder.Length;
            voice.transform.position = listener + (direction * ThunderDistance) + (Vector3.up * 15f);
            voice.clip = clips[variant];
            voice.volume = AudioMix.ThunderVolume(thunder);
            voice.pitch = AudioMix.ThunderPitch(thunder);
            voice.Play();
        }

        private void OnDestroy()
        {
            if (_storm != null)
            {
                _storm.ThunderArrived -= OnThunder;
            }

            _subscription?.Dispose();
            foreach (var generated in _generated)
            {
                ObjectUtility.Destroy(generated);
            }

            _generated.Clear();
        }

        private void UseSynthesised(Task<Synthesised> synthesis)
        {
            if (synthesis.IsFaulted)
            {
                Debug.LogWarning($"[Townscape] Could not make the town's sounds: {synthesis.Exception?.GetBaseException().Message}");
                return;
            }

            var sounds = synthesis.Result;
            if (sounds.Rain != null)
            {
                Play(_rain, Generate("Rain", sounds.Rain));
            }

            if (sounds.Wind != null)
            {
                Play(_wind, Generate("Wind", sounds.Wind));
            }

            if (sounds.River != null)
            {
                Play(_riverSource, Generate("River", sounds.River));
            }

            if (sounds.Cracks != null)
            {
                _cracks = Array.ConvertAll(sounds.Cracks, samples => Generate("Thunder Crack", samples));
            }

            if (sounds.Rumbles != null)
            {
                _rumbles = Array.ConvertAll(sounds.Rumbles, samples => Generate("Thunder Rumble", samples));
            }
        }

        private AudioSource Loop(string name, bool spatial, HideFlags hideFlags)
        {
            var source = TownMeshSpawner.CreateChild(name, transform, hideFlags).AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.spatialBlend = spatial ? 1f : 0f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        private static void Play(AudioSource source, AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            source.clip = clip;

            // Start somewhere different in each loop, so they never line up the same way twice.
            source.timeSamples = clip.samples > 0 ? UnityEngine.Random.Range(0, clip.samples) : 0;
            source.Play();
        }

        private static AudioClip[] Assigned(AudioClip[] clips)
        {
            var assigned = new List<AudioClip>();
            foreach (var clip in clips ?? Array.Empty<AudioClip>())
            {
                if (clip != null)
                {
                    assigned.Add(clip);
                }
            }

            return assigned.ToArray();
        }

        private AudioClip Generate(string name, float[] samples)
        {
            var clip = AudioClip.Create($"Townscape {name}", samples.Length, 1, ProceduralSounds.SampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontSave;
            _generated.Add(clip);
            return clip;
        }

        private readonly struct Needed
        {
            public Needed(bool rain, bool wind, bool river, bool cracks, bool rumbles)
            {
                Rain = rain;
                Wind = wind;
                River = river;
                Cracks = cracks;
                Rumbles = rumbles;
            }

            public bool Rain { get; }

            public bool Wind { get; }

            public bool River { get; }

            public bool Cracks { get; }

            public bool Rumbles { get; }

            public bool Any => Rain || Wind || River || Cracks || Rumbles;
        }

        // Plain sample arrays: made on a background thread, turned into clips on the main thread.
        private sealed class Synthesised
        {
            public float[] Rain { get; private set; }

            public float[] Wind { get; private set; }

            public float[] River { get; private set; }

            public float[][] Cracks { get; private set; }

            public float[][] Rumbles { get; private set; }

            public static Synthesised Make(Needed needed) => new Synthesised
            {
                Rain = needed.Rain ? ProceduralSounds.Rain(11) : null,
                Wind = needed.Wind ? ProceduralSounds.Wind(12) : null,
                River = needed.River ? ProceduralSounds.River(13) : null,
                Cracks = needed.Cracks ? new[] { ProceduralSounds.Thunder(21, true), ProceduralSounds.Thunder(22, true) } : null,
                Rumbles = needed.Rumbles ? new[] { ProceduralSounds.Thunder(31, false), ProceduralSounds.Thunder(32, false), ProceduralSounds.Thunder(33, false) } : null,
            };
        }
    }
}
