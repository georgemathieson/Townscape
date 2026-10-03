# Townscape architecture

## Overview

```
Assets/Townscape/Scripts/
  State/        Townscape.State        engine-free: Redux-style store, actions, reducers
  Generation/   Townscape.Generation   engine-free: layout, terrain, ground, structures, markings
  Simulation/   Townscape.Simulation   engine-free: lights after dark, the storm, and its sounds
  Runtime/      Townscape.Runtime      Unity: bootstrap, rendering, lighting, controls, UI
  Editor/       Townscape.Editor       Unity editor: project setup, menus, inspectors
Assets/Townscape/Shaders/              the one hand-written shader: the fog-free lightning bolt
Assets/Townscape/Tests/EditMode/       NUnit tests for State, Generation and Simulation
Assets/Scenes/Town.unity               holds a single TownscapeBootstrap
tools/                                 checks and previews that run without Unity
```

`State`, `Generation` and `Simulation` have `noEngineReferences: true`, so they cannot touch `UnityEngine`. That
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
| Weather profile | Lightning and thunder timing, how wet things are |
| Requests for a lightning strike (a count that only goes up) | Which windows and lamps are lit, the beacons' flash |
| Volume and mute | How loud the rain, wind and river are right now |

The store is also what gets saved: `SavedSettings` writes the user's choices as a line of text and
reads it back forgivingly (anything missing or damaged falls back to the default), and
`SettingsMemory` keeps it in `PlayerPrefs` a second after the last change. Things that are only about
how the screen looks, such as whether the panel is showing, are not in the store.

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
| `IStructureGenerator` | `BridgeGenerator`, `BuildingGenerator` | street furniture, trees |
| `IBuildingStyle` | terraced unit, detached house (cottages, the mill, a detached shop), church | chapel, barn |
| `IGroundFloorStyle` | traditional shopfront, inn, house front | bay-windowed shop |
| `IShopDisplay` | books, coffee, computers, newsagent, bakery, chippy, florist, gallery, generic shelves | anything new a shop needs |
| `IDressingRule` | street lamps, Belisha beacons, river railings, placed props, dry-stone walls, churchyard, trees, ground cover, flower beds, puddles | hedges, parked cars |
| `IProp` | lamp post, K6 phone box, pillar box, bench, bus stop, Belisha beacon, memorial, gravestone, tree (four species), flower clump | anything placed |
| `ITownscapeInput` | Input System, legacy Input Manager | gamepad |
| `IWeatherProfile` | thunderstorm | clear skies, fog, snow |
| `IWeatherEffect` | rain and splashes, lightning, wet surfaces, water, mist, chimney smoke, wind sway | hail, a rainbow |

Data: `SurfacePalette` (colours), `LightingProfile` (time-of-day keyframes), `NightLights` (glow
colours and light brightness), `GenerationSettings` and `VillageShops` (each shop's name, paint,
lettering and display).
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
4. **Water:** one plane just below water level; terrain sits above it everywhere except the river
   and lake. The river has its own ribbon of water on top whose texture coordinates run downstream,
   so its ripples can flow round the bends.
5. **Markings:** each road's `IRoadMarking`s paint thin quads just above the carriageway. Anything
   that would land off the road (over the bridge, on a pavement) is dropped.
6. **Structures:** every `IStructureGenerator` adds its meshes. The bridge footprint leaves a hole in
   the ground that the bridge deck fills, meeting the road at road level.

### Buildings

Buildings are **planned before the ground is generated**:

1. `BuildingPlanner` turns the layout's `TerraceSpec`s and `DetachedBuildingSpec`s into footprints.
   Terraced units are split along the road by width weights. Their side walls follow the road's
   normals, so units are slightly wedge-shaped on curves and neighbours share walls exactly.
   `TerraceDesigner` picks each unit's look: wall finish, floors, windows, dormer, door colour.
2. Each footprint becomes a `PlotFeature`: flat flagstones at pavement height with a 0.6 m apron,
   so hummocks never poke through a floor.
3. `BuildingGenerator` builds each plan with its `IBuildingStyle`, one mesh per terrace or building.

Styles are assembled from small parts in `Buildings/Parts`:
- `WallFrame`: wall-space coordinates, where x runs along the wall, y is height and z is out of the wall.
- `WallBuilder`: walls with recessed openings.
- `Glazing`: sash and casement windows, doors and fanlights.
- `GableRoof`: slate roofs on any four-sided footprint.
- `Chimney` and `Dormer`.
- `PixelFont`: shop signs built as geometry, so no font assets are needed and Unity and the preview
  look the same.

Shop windows use transparent glass in front of a shallow display box that an `IShopDisplay` dresses.

### Dressing

After the buildings, the layout's `IDressingRule`s run in order through `DressingGenerator`. Rules
place `IProp`s (each built in its own `PropFrame`: x right, y up, z towards its front) or draw
long features directly (railings, dry-stone walls). The `DressingContext` gives them:
- ground height and region queries that also cover the fells beyond the core
- an occupancy map, so trees keep clear of lamps, benches, walls and each other
- mesh builders chunked by area: 50 m in the village and 200 m on the fells, split into Furniture,
  Vegetation and Puddles because each is treated differently
- sway heights: while a plant is built, every vertex records how high it is above the plant's base
  (`MeshData.SwayHeights`), so the wind can bend tree tops more than trunks

Rules lean on the ground model, so placement stays sensible without hand-tuning. For example,
dry-stone walls break wherever they would cross a road, path or yard, which leaves gateways at
every cottage and junction.

Generators also leave **anchors** (`TownAnchor`) for later systems:
- chimney pots, for smoke from the fires
- windows, fanlights and shop windows
- the inn's lanterns and the street lamps
- Belisha beacons, which flash
- the phone box's lit sign

Generation is deterministic (seeded noise, no `UnityEngine.Random`) and takes well under a second.

Coordinates: metres, x east, y up, z north, bridge at the origin. Triangles are wound clockwise from
the front, as Unity expects.

## Runtime

`TownscapeBootstrap` is the composition root and the only object saved in the scene. It has
`[ExecuteAlways]`:

- **Edit mode:** generates the town, a sun and the town's lights into `HideFlags.DontSave` objects, so
  the Scene view shows the town but nothing generated is ever written into the scene file.
- **Play mode:** also creates the camera, the post-processing volume, fog and environment, the
  storm, sound, the control panel and keyboard shortcuts, and loads the saved settings.

It creates every object and hands each one its dependencies through an `Initialize` method. There
are no singletons and no `FindObjectOfType`, which also keeps the project safe with domain reload
disabled.

`MaterialLibrary` creates one URP Lit material per `SurfaceMaterial`, and systems change them while
running through it (glow, wetness, ripples).

`TimeOfDayLighting` drives a single directional light: the sun by day, the moon by night. It also
sets ambient light, fog, the background colour, post exposure, and a generated sky gradient that
feeds reflections (so the water reflects a storm sky).

## Lighting after dark

The rules live in the engine-free `Simulation` assembly, so they are unit tested and the preview
renderer uses them too. `TownLights` (Runtime) only applies them to Unity.

- **Darkness.** Each lighting keyframe has a `Darkness` value from 0 (broad day) to 1 (night). It
  blends with the rest of the keyframe, so lamps and windows follow presets, the hour keys and the
  auto-cycle without knowing about any of them.
- **`LightSchedule`** decides *when*, as a level from 0 to 1:
  - Home windows come in eight groups. Each group lights at its own darkness, so windows come on
    one by one at dusk, and goes to bed at its own time between 10pm and 1am. Some are lit early
    on a winter morning, and one group is on through a gloomy day.
  - Group 7 is the television: a flickering blue-white.
  - Street lamps each switch on at a slightly different darkness and stutter as they come on.
  - Shops are lit during opening hours and dimly after closing. The inn stays lit until 11.30pm.
  - Belisha beacons flash about once a second.
- **`NightLights`** decides *how*: the glow colour of each emissive material, and a `LightSpec`
  (colour, range, intensity) for each kind of anchor that casts real light.
- **Window groups.** Generation gives every home window one of eight glass materials, `Window0` to
  `Window7`, picked at random. A whole group lights together, so the village lights up window by
  window while `TownLights` only updates eight materials a frame and needs no object per window.
- **`TownLights`** puts an unshadowed point light at every street lamp, shop window, inn lantern,
  beacon and the phone box (Forward+ copes with many small lights), and each frame sets the
  emission of every glowing material. Lights fade rather than snap, except the beacons, and are
  disabled when they are off.

Units: `NightLights` colours are linear and its intensities are in Unity's units. Unity treats light
colours as sRGB, so `TownLights` converts them, and emission is written as a raw linear vector
because `Material.SetColor` would treat it as sRGB too. three.js divides diffuse light by π and
Unity doesn't, so the preview multiplies light intensities by π. Both use ACES tone mapping, which
bleaches bright glows towards white, so glow colours are set more saturated than they should look:
the street lamps' glass comes out a creamy warm white and the beacons amber-orange.

## The storm

Like the lights, the rules are engine-free (`Simulation/Weather`) and tested, and Unity only
applies them. Everything is driven from code on stock URP materials, apart from one small shader.

- **Profiles.** An `IWeatherProfile` turns the user's settings and the clock into
  `WeatherConditions`: how hard it is raining, the wind, strikes a minute and mist. The
  `ThunderstormProfile` makes rain come in surges, the south-westerly wind gust and veer, and storm
  cells drift through so lightning comes in bursts.
- **`StormSystem`** (Runtime) samples the profile each frame, moves the wetness on, runs the
  lightning and ticks every `IWeatherEffect` with a `WeatherFrame`. It also raises `Struck` and
  `ThunderArrived` events for the audio milestone.
- **Lightning.** `LightningStorm` schedules strikes at random at the current rate. Each is a few
  return strokes a fraction of a second apart, which makes the flicker. Its thunder arrives
  distance ÷ 343 m/s later, louder and sharper when close. `LightningBolt` draws the jagged channel
  and its branches by midpoint displacement. The flash lifts the ambient light and the fog through
  `TimeOfDayLighting.Weather` (lamps keep reading the clean time of day), and a directional light
  shines from the strike.
- **Bolts** use the one custom shader (`Shaders/LightningBolt.shader`): unlit, additive and without
  fog, because the storm's fog would otherwise hide a bolt a kilometre away completely. Bolts beyond
  the camera's far plane are drawn nearer and scaled down so they look the same.
- **Rain** is a particle system in a box that follows the camera, slanted by the wind.
  `RainCatchMap` is a height map of whatever rain hits first (roofs, awnings, the bridge, the road,
  the river), rasterised once from the generated meshes; drops end there and splash.
- **Wet surfaces.** `Wetness` darkens and glosses each kind of surface: tarmac and flagstones most,
  render and paint less, glass not at all. Surfaces soak up in about half a minute of heavy rain.
- **Water.** `RippleField` makes looping, tileable normal maps of raindrop rings (and a swell for
  open water). They play as flipbooks on puddles, the lake and the river, and the river's texture
  slides downstream.
- **Mist** is big soft particles near the ground plus thicker fog. **Chimney smoke** comes from the
  chimneys with a fire lit (more in the evening): it rises, slows as it cools and bends downwind.
- **Wind sway** moves plant vertices on the CPU, only near the camera. `WindSway.Motion` is worked
  out once per 2.5 m patch (gusts roll across the land as waves, so neighbours move together), and
  each vertex scales it by its own bend.

## Controls and sound

- **`ControlPanel`** is drawn with Unity's immediate-mode GUI and a skin made in code
  (`PanelSkin`), so it needs no assets, works with either input system and scales with the screen.
  Like `TownscapeShortcuts`, it only dispatches actions. The time slider dispatches
  `SetTargetHour(hour, Scrub: true)`, which sets a very short blend so the clock follows the hand.
- **`TownAudio`** plays rain and wind loops, the river from the nearest point on its course, and
  thunder from a pool of voices placed towards each strike when `StormSystem.ThunderArrived` fires.
  Volumes come from `AudioMix`. Any clip slot left empty on the bootstrap is filled by
  `ProceduralSounds`, which synthesises rain, wind, the river and thunder from filtered noise:
  seamless loops, and close thunder that cracks while distant thunder only rumbles.

## Editor

- `TownscapeProjectSetup` runs on editor load and is idempotent. It creates and assigns the URP
  pipeline asset (`Assets/Townscape/Settings/`, Forward+, MSAA, soft shadows), switches to linear
  colour and adds the scene to the build.
- `ShaderVariantKeeper` (part of the setup) saves one small material per shader and keyword
  combination the town switches on at runtime in `Assets/Townscape/Resources/Shader Variants/`. All
  the town's materials are made in code, so without these a player build would leave out
  transparent glass, glow, ripples, the weather particles and the bolt shader.
- The **Townscape** menu has Open Town Scene, Rebuild Town, Set Up Project and Create Town Scene (which
  rebuilds the scene from code if it is ever lost).

## Checking work without Unity

| Tool | What it does |
|---|---|
| `dotnet test tools/verify/CoreTests` | Runs every EditMode test (State, Generation and Simulation) under .NET 8 |
| `dotnet build tools/verify/UnityCompile/Editor.csproj` | Compiles every assembly the way Unity splits them, against Unity reference assemblies and URP/Input System signature stubs |
| `tools/preview` | Runs the real generator, exports glTF, the night lights and the storm, and renders PNGs with three.js in headless Chromium |
| `python3 tools/generate_meta.py` | Creates `.meta` files with GUIDs derived from the path, so references can be written by hand |

The compile check uses an older Unity's reference assemblies, so code behind newer version checks
compiles through its `#else` branch. The editor remains the final word.

## Conventions

- New files under `Assets/` need a `.meta`: run `python3 tools/generate_meta.py`. Move a file's
  `.meta` along with it.
- Keep generation and simulation engine-free. If it needs `UnityEngine`, it belongs in Runtime.
- Prefer a new strategy over a new branch in an existing generator.
