using System.Collections.Generic;
using System.Linq;
using Townscape.Generation;
using Townscape.Generation.People;
using Townscape.Runtime.Rendering;
using Townscape.Runtime.Security;
using Townscape.Runtime.Walking;
using Townscape.Simulation.Audio;
using Townscape.Simulation.People;
using UnityEngine;

namespace Townscape.Runtime.People
{
    /// <summary>
    /// The people who come when an alarm goes off, in play: the guard who waits in the alarm
    /// receiving centre, the police and their car, and the burglar the control panel sends.
    /// <see cref="TownPeople"/> decides where they go and what they do; this draws them, opens
    /// and shuts the town's doors for them, lets them see (nothing solid in between) and keeps
    /// them from walking through the player. The alarms' sensors see them as they see the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PeopleSystem : MonoBehaviour, ITownWorld
    {
        private readonly Dictionary<string, SwingingDoor> _doors = new Dictionary<string, SwingingDoor>();
        private readonly Dictionary<Person, FigureView> _views = new Dictionary<Person, FigureView>();
        private readonly Dictionary<PersonRole, FigureParts> _parts = new Dictionary<PersonRole, FigureParts>();
        private readonly List<Object> _owned = new List<Object>();

        private TownPeople _people;
        private WalkingController _walking;
        private HideFlags _hideFlags;
        private PoliceCarView _car;
        private AudioSource _sounds;
        private AudioClip _forced;

        /// <summary>Who's about and what they're doing, and where to send a burglar.</summary>
        public TownPeople People => _people;

        /// <summary>Everyone out and about, for the alarms' sensors: the middle of them, and whether they're on the move.</summary>
        public IEnumerable<(Vector3 Chest, bool Moving)> Bodies =>
            _people == null ? Enumerable.Empty<(Vector3, bool)>() : _people.People.Select(p => (ToUnity(p.Chest), p.Walker.Moving));

        /// <summary>Puts the people in the town (null if there's no alarm receiving centre for them to answer to).</summary>
        public static PeopleSystem Create(GeneratedTown town, MaterialLibrary materials, Transform parent, HideFlags hideFlags, WalkingController walking, IReadOnlyList<SwingingDoor> doors, AlarmCentreSystem centre)
        {
            if (centre == null || centre.Centre == null)
            {
                return null;
            }

            var system = TownMeshSpawner.CreateChild("People", parent, hideFlags).AddComponent<PeopleSystem>();
            system.Initialize(town, materials, hideFlags, walking, doors, centre);
            return system;
        }

        private void Initialize(GeneratedTown town, MaterialLibrary materials, HideFlags hideFlags, WalkingController walking, IReadOnlyList<SwingingDoor> doors, AlarmCentreSystem centre)
        {
            _walking = walking;
            _hideFlags = hideFlags;
            foreach (var door in doors ?? new List<SwingingDoor>())
            {
                if (door != null && door.Door != null)
                {
                    _doors[door.Door.Name] = door;
                }
            }

            _parts[PersonRole.Guard] = new FigureParts(PeopleModels.Figure(FigureKind.Guard), materials, _owned);
            _parts[PersonRole.Police] = new FigureParts(PeopleModels.Figure(FigureKind.Police), materials, _owned);
            _parts[PersonRole.Burglar] = new FigureParts(PeopleModels.Figure(FigureKind.Burglar), materials, _owned);
            _car = new PoliceCarView(PeopleModels.PoliceCar(), materials, transform, hideFlags, _owned);

            // A door being forced, heard from wherever it happens.
            var samples = PoliceSounds.DoorForced();
            _forced = AudioClip.Create("Door forced", samples.Length, 1, ProceduralSounds.SampleRate, false);
            _forced.SetData(samples, 0);
            _owned.Add(_forced);
            _sounds = TownMeshSpawner.CreateChild("Sounds", transform, hideFlags).AddComponent<AudioSource>();
            _sounds.playOnAwake = false;
            _sounds.spatialBlend = 1f;
            _sounds.rolloffMode = AudioRolloffMode.Logarithmic;
            _sounds.minDistance = 4f;
            _sounds.maxDistance = 60f;

            _people = new TownPeople(town, centre.Centre, seed: 1847);
            _people.DoorForced += position =>
            {
                _sounds.transform.position = ToUnity(position);
                _sounds.PlayOneShot(_forced);
            };
        }

        private void Update()
        {
            if (_people == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            _people.Tick(deltaTime, this);

            // A figure for everyone about, made as they turn up and cleared away when they've gone.
            var about = new HashSet<Person>(_people.People);
            foreach (var person in about)
            {
                if (!_views.TryGetValue(person, out var view))
                {
                    view = new FigureView(person, _parts[person.Role], transform, _hideFlags);
                    _views.Add(person, view);
                }

                view.Sync(deltaTime);
            }

            foreach (var gone in _views.Keys.Where(p => !about.Contains(p)).ToList())
            {
                _views[gone].Destroy();
                _views.Remove(gone);
            }

            _car.Sync(_people.Car, deltaTime);
        }

        private void OnDestroy()
        {
            foreach (var owned in _owned)
            {
                ObjectUtility.Destroy(owned);
            }

            _owned.Clear();
        }

        // ---- The town around them ----------------------------------------------------------

        public bool IsOpen(string door) => _doors.TryGetValue(door, out var swinging) && swinging.IsOpen;

        public void Open(string door)
        {
            if (_doors.TryGetValue(door, out var swinging) && !swinging.IsOpen)
            {
                swinging.Interact();
            }
        }

        public void Shut(string door)
        {
            if (_doors.TryGetValue(door, out var swinging) && swinging.IsOpen)
            {
                swinging.Interact();
            }
        }

        // People (and the player) are on the Ignore Raycast layer, so only walls, doors and
        // furniture get in the way.
        public bool CanSee(System.Numerics.Vector3 eye, System.Numerics.Vector3 target) =>
            !Physics.Linecast(ToUnity(eye), ToUnity(target), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        public bool InTheWay(System.Numerics.Vector3 position, System.Numerics.Vector3 facing, float reach)
        {
            if (_walking == null || !_walking.Active)
            {
                return false;
            }

            var offset = _walking.transform.position - ToUnity(position);
            if (Mathf.Abs(offset.y) > 1.5f)
            {
                return false;
            }

            offset.y = 0f;
            var distance = offset.magnitude;
            return distance < reach && distance > 1e-3f && Vector3.Dot(offset / distance, ToUnity(facing)) > 0.5f;
        }

        private static Vector3 ToUnity(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
