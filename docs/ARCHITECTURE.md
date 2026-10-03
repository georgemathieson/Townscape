# Townscape architecture

## Overview

```
Assets/Townscape/Scripts/
  State/        Townscape.State        engine-free: Redux-style store, actions, reducers
  Generation/   Townscape.Generation   engine-free: layout, terrain, ground, structures, markings
  Runtime/      Townscape.Runtime      Unity: bootstrap, rendering, lighting, controls, UI
  Editor/       Townscape.Editor       Unity editor: project setup, menus, inspectors
Assets/Townscape/Tests/EditMode/       NUnit tests for State and Generation
Assets/Scenes/Town.unity               holds a single TownscapeBootstrap
tools/                                 checks and previews that run without Unity
```

`State` and `Generation` have `noEngineReferences: true`, so they cannot touch `UnityEngine`. That
keeps the interesting logic fast to test and lets it run outside Unity: in plain `dotnet test`, and in
the offline preview renderer. `Runtime` is a thin layer that turns their output into GameObjects and
drives Unity's lighting.

## State: a Redux-style store, with one rule

`Store<TState>` holds an immutable `TownState` (C# records). The UI and keyboard dispatch actions
(`SelectTimePreset`, `SetRainIntensity`, ...), pure reducers return the next state, and systems
subscribe to the slice they care about. Subscribers are called immediately with the current value and
then only when their slice changes. Actions dispatched from inside a subscriber are queued and run in
order.

**The rule:** the store holds what the user *asked for*, not what is on screen this frame.

| In the store | Owned by a system, recalculated every frame |
|---|---|
| Target hour, preset, auto-cycle on/off, cycle speed | The hour currently shown while blending |
| Rain intensity, lightning frequency, wind strength | Particle rates, gusts, the flash brightness |
| Weather profile | Lightning and thunder timing |

Pushing per-frame values through reducers would create garbage for the garbage collector every frame
(stutters) and adds nothing, because nobody asked for those changes. So, for example,
`TimeOfDayLighting` reads the target hour from the store and eases its own clock towards it. When the
UI needs the shown hour (to stop the auto-cycle where it is), it reads it from the system and passes
it in the action (`SetAutoCycle(false, currentHour)`).

Turn on **Log Actions** on the bootstrap to see every action in the console.

## Strategies and data

Behaviour that varies is a strategy (an interface with several implementations). Values that vary are
data.

| Strategy | Implementations today | Later |
|---|---|---|
| `ITownLayoutSource` | `LakeDistrictVillageLayout` | other towns |
| `IGroundFeature` | road, river, footpath, structure footprint | building plots, yards |
| `ISurfaceRegion` | flat, terrain-following, open ground, river bed and bank | puddles |
| `IRoadMarking` | centre line, double yellows, give way, zebra crossing | bus stop, "SLOW" |
| `IStructureGenerator` | `BridgeGenerator` (humpback bridge) | terraces, cottages, street furniture |
| `ITownscapeInput` | Input System, legacy Input Manager | gamepad |

Data: `SurfacePalette` (colours), `LightingProfile` (time-of-day keyframes) and `GenerationSettings`.
These are plain code today and are meant to become ScriptableObject assets once there is a UI to tune them.

## Generation pipeline

`TownGenerator.Generate(layout)` returns engine-free `MeshData` (positions, flat normals, and one
submesh per `SurfaceMaterial`):

1. **Ground model.** Every road, the river, each path and the bridge footprint is an `IGroundFeature`
   described as a distance field around a polyline or box. `GroundModel.Classify(point)` asks every
   feature and the highest priority claim wins (river > bridge footprint > road > pavement > path >
   grass), so junctions and crossings resolve themselves.
2. **Ground mesh** (the detailed 200 × 200 m core, 1 m grid, in 50 m chunks):
   - Grid edges whose ends lie in different regions cross a boundary; the end nearer that boundary
     is snapped onto it, so kerbs and walls come out straight instead of jagged.
   - Each triangle takes its region from its centroid and its heights from that region.
   - Wherever neighbouring triangles disagree on height, a vertical face fills the gap. That one rule
     makes the kerbs, verge edges and stone river embankments.
3. **Fells:** a coarse 10 m grid from the edge of the core out to 700 m, with the river valley and the
   lake carved in. The core border is perfectly flat so the two meshes meet cleanly.
4. **Water:** one plane at water level. Terrain sits above it everywhere except the river and lake.
5. **Markings:** each road's `IRoadMarking`s paint thin quads just above the carriageway. Anything
   that would land off the road (over the bridge, on a pavement) is dropped.
6. **Structures:** every `IStructureGenerator` adds its meshes. The bridge footprint leaves a hole in
   the ground that the bridge deck fills, meeting the road at road level.

Generation is deterministic (seeded noise, no `UnityEngine.Random`) and takes well under a second.

Coordinates: metres, x east, y up, z north, bridge at the origin. Triangles are wound clockwise from
the front, as Unity expects.

## Runtime

`TownscapeBootstrap` is the composition root and the only object saved in the scene. It has
`[ExecuteAlways]`:

- **Edit mode:** generates the town and a sun into `HideFlags.DontSave` objects, so the Scene view
  shows the town but nothing generated is ever written into the scene file.
- **Play mode:** also creates the camera, the post-processing volume, fog and environment, keyboard
  shortcuts and the help overlay.

It creates every object and hands each one its dependencies through an `Initialize` method. There
are no singletons and no `FindObjectOfType`, which also keeps the project safe with domain reload
disabled.

`MaterialLibrary` creates one URP Lit material per `SurfaceMaterial`. Later milestones swap in a
custom shader (wetness, ripples) in one place.

`TimeOfDayLighting` drives a single directional light: the sun by day, the moon by night. It also
sets ambient light, fog, the background colour, post exposure, and a generated sky gradient that
feeds reflections (so the water reflects a storm sky).

## Editor

- `TownscapeProjectSetup` runs on editor load and is idempotent. It creates and assigns the URP
  pipeline asset (`Assets/Townscape/Settings/`, Forward+, MSAA, soft shadows), switches to linear
  colour and adds the scene to the build.
- The **Townscape** menu has Open Town Scene, Rebuild Town, Set Up Project and Create Town Scene (which
  rebuilds the scene from code if it is ever lost).

## Checking work without Unity

| Tool | What it does |
|---|---|
| `dotnet test tools/verify/CoreTests` | Runs every EditMode test (State and Generation) under .NET 8 |
| `dotnet build tools/verify/UnityCompile/Editor.csproj` | Compiles every assembly the way Unity splits them, against Unity reference assemblies and URP/Input System signature stubs |
| `tools/preview` | Runs the real generator, exports glTF and renders PNGs with three.js in headless Chromium |
| `python3 tools/generate_meta.py` | Creates `.meta` files with GUIDs derived from the path, so references can be written by hand |

The compile check uses an older Unity's reference assemblies, so code behind newer version checks
compiles through its `#else` branch. The editor remains the final word.

## Conventions

- New files under `Assets/` need a `.meta`: run `python3 tools/generate_meta.py`. Move a file's
  `.meta` along with it.
- Keep generation engine-free. If it needs `UnityEngine`, it belongs in Runtime.
- Prefer a new strategy over a new branch in an existing generator.
