# Townscape architecture

## Overview

```
Assets/Townscape/Scripts/
  State/        Townscape.State        engine-free: Redux-style store, actions, reducers
  Generation/   Townscape.Generation   engine-free: layout, terrain, ground, structures, markings
  Simulation/   Townscape.Simulation   engine-free: lights after dark, the storm, and its sounds
  CoffeeShop/   Townscape.CoffeeShop   engine-free: Fellside Coffee, the management game
  Runtime/      Townscape.Runtime      Unity: bootstrap, rendering, lighting, controls, UI
  Editor/       Townscape.Editor       Unity editor: project setup, menus, inspectors
Assets/Townscape/Shaders/              the one hand-written shader: the fog-free lightning bolt
Assets/Townscape/Tests/EditMode/       NUnit tests for State, Generation, Simulation and CoffeeShop
Assets/Scenes/Town.unity               holds a single TownscapeBootstrap
tools/                                 checks and previews that run without Unity
```

`State`, `Generation`, `Simulation` and `CoffeeShop` have `noEngineReferences: true`, so they cannot touch `UnityEngine`. That
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
| Rain and snow intensity, lightning frequency, wind strength | Particle rates, gusts, the flash brightness |
| Weather profile (thunderstorm or snowstorm) | Lightning and thunder timing, how wet things are, how much snow is lying |
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
| `IBuildingStyle` | terraced unit, detached house (cottages, the mill, a detached shop), church, petrol station | chapel, barn |
| `IGroundFloorStyle` | traditional shopfront, inn, house front | bay-windowed shop |
| `IShopDisplay` | books, coffee, computers, newsagent, bakery, chippy, florist, gallery, generic shelves | anything new a shop needs |
| `IDressingRule` | street lamps, Belisha beacons, river railings, placed props, dry-stone walls, churchyard, trees, ground cover, flower beds, puddles | hedges, parked cars |
| `IProp` | lamp post, K6 phone box, pillar box, bench, bus stop, Belisha beacon, memorial, gravestone, tree (four species), flower clump | anything placed |
| `ITownscapeInput` | Input System, legacy Input Manager | gamepad |
| `IWeatherProfile` | thunderstorm, snowstorm | clear skies, fog |
| `IWeatherEffect` | rain and splashes, snow, lightning, wet and snowy surfaces, water, mist and blowing snow, chimney smoke, wind sway | hail, a rainbow |

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
   so hummocks never poke through a floor. A style that also implements `IYardFinish` picks another
   surface: the petrol station's forecourt is concrete.
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

`PetrolStationStyle` takes a whole plot (at least 22 by 17.5 m) and places everything in site metres
from the plot's front-left corner: the garage at the back (a shop and a gabled workshop whose roof
runs back into the main one), the pump island and canopy in front of it, a price sign by the road,
string lights behind the shop glass and bunting strung from the canopy's corners. Its name, colours,
prices, flag and bulb colours come from a `PetrolStationDesign`. Its footprint is the whole plot, so
dressing keeps lamps and trees off the forecourt.

#### Going inside: the Copper Kettle

A shop marked `Enterable` (only the Copper Kettle) is built inside as well as out, by
`TerracedUnitStyle` and the parts in `Buildings/Interiors`:
- `UnitSpace` measures a unit the way you'd pace it out: x across from the left of the front, d back
  from the front wall, y up. It builds floors, ceilings, walls with doorways and windows cut out,
  and boxes and round things for furniture, every surface facing into its room.
- `StairFlight` is a straight flight of solid steps. Risers are kept under the walker's step height.
- `CafeAndFlat` lays out the rooms: the café (counter and till, cake cabinet, espresso machine,
  chalkboard, tables, window bar) and its storeroom on the ground floor, a hall from the flat's
  door up a dog-leg stair on the left, a living room and kitchen on the first floor, a bedroom and
  bathroom on the second, and an attic study and snug under the roof, lit through the dormer. The
  attic has upright knee walls where the slopes come down, and the chimney stacks on its party walls
  are plastered in as chimney breasts.
- Its windows are real openings with `ClearGlass`, front and back, and the dormer is built
  see-through (`Dormer.Build(seeThrough: true)`), so you can look in and out. A see-through
  `WindowStyle` puts the faintly tinted glass on both sides and stands the frames proud of it inside
  as well as out, with a wooden board along the bottom. Interior surfaces use their own materials
  (tiles, fabrics, linen) so the weather never wets or snows on them.
- Two roof windows (`RoofWindow`) light the attic's snug from the back slope: `GableRoof.Build`
  leaves a `RoofOpening` in the slates for each, the attic's lining is cut round them with reveals
  up to the slates, and the glazed sash pivots open about a level line across its middle.
- The Copper Kettle is a little wedge-shaped (its back wider than its front), so the back windows
  and the roof's holes are placed through `UnitSpace` (`AcrossAt`, `FractionAcross`): straight
  behind where the rooms put them, not at the same fraction of a longer wall.
- `CafeAndFlat` fits two burglar alarms, the café's and the flat's, out of `AlarmFittings`: a
  sensor high in a corner of every room and a contact on every door and roof window (each a zone),
  a keypad inside each front door, a white control box the zones are wired back to (in the
  storeroom; in the attic), and a bell box (one plain case, 26 by 34 cm, with a blue strobe across
  its foot): red on the right of the café's sign, which moves its lettering over to make room
  (`ShopDefinition.BellBox`), and white high on the front between the first two second-floor
  windows for the flat. Each alarm is a `TownAlarm` (its zones, keypads, control box and strobe)
  in `GeneratedTown.Alarms`, collected through `BuildContext.Alarms` as doors are.
- `CafeAndFlat` also fits each its fibre broadband out of `BroadbandFittings`: a white ONT with
  four lights and a black router with three, side by side on a wall with a network cable between
  them (the café's in its storeroom, cabled across to its alarm's control box; the flat's in the
  living room). Each is a `TownBroadband` (customer, plan speed, both boxes and their lights) in
  `GeneratedTown.Broadband`, collected through `BuildContext.Broadband`; its customer is named as
  the building's alarm is, so the alarm can report through it.
- `CafeAndFlat.Walkways` lists the ways in from each door and through each doorway; a test keeps
  every bit of furniture out of them.
- Hanging lights go in `BuildContext.Fittings`, a mesh of their own (`MeshCategory.Fittings`) that
  isn't solid, so you never snag on one; the landings have flush lights instead of pendants.

Doors that open are not part of the building's mesh. `BuildContext.Door` builds the leaf in its own
mesh around its hinge and records a `TownDoor` (hinge, the line it turns about, the way it closes
across, the way it opens, size), which `GeneratedTown.Doors` hands to the runtime. The shopfront
makes two: the café door in the glazed lobby on the right and the flat's door on the left. The roof
windows are `TownDoor`s too, turning about a level axis across the slope, so the top of the sash
tips into the room.

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

Props can have doors that open too: `PropFrame.Door` builds a leaf in its own mesh, in the prop's
own coordinates, and records a `TownDoor` as `BuildContext.Door` does. The phone box's whole front
is a door that swings out, and inside (clear glass both ways, so you can see in and out) are a
payphone, a 999 card, a shelf of directories and a ceiling light; it's a shade roomier than a real
K6 so there's room to stand. Beside it, `FibreCabinet` is the street cabinet: a dark green case on
a concrete plinth with double doors, and inside a 19 inch rack (patch tray, fibre switch, edge
router, splice shelf, power strip, UPS and battery) and a little computer and screen. It records a
`TownCabinet` (`GeneratedTown.Cabinets`): the front of the rack, the screen, and where each light on
the kit is.

Rules lean on the ground model, so placement stays sensible without hand-tuning. For example,
dry-stone walls break wherever they would cross a road, path or yard, which leaves gateways at
every cottage and junction.

Generators also leave **anchors** (`TownAnchor`) for later systems:
- chimney pots, for smoke from the fires
- windows, fanlights and shop windows
- the inn's lanterns and the street lamps
- Belisha beacons, which flash
- the phone box's light (its signs and inside) and the petrol station's lit sign
- the lights in the petrol station's canopy, shining down on the pumps
- room lights inside the Copper Kettle's flat (`RoomLight`), which come on in the evening like a home's windows

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
  - The petrol station's canopy lights are dimly on all day and full on after dark. The string
    lights in its shop windows brighten at dusk, and each colour breathes at its own pace.
- **`NightLights`** decides *how*: the glow colour of each emissive material, and a `LightSpec`
  (colour, range, intensity) for each kind of anchor that casts real light. The string-light bulbs
  (`BulbRed` to `BulbBlue`) glow in nearly pure colours at a modest strength, because ACES tone
  mapping turns a bright, slightly impure colour into a pastel.
- **Window groups.** Generation gives every home window one of eight glass materials, `Window0` to
  `Window7`, picked at random. A whole group lights together, so the village lights up window by
  window while `TownLights` only updates eight materials a frame and needs no object per window.
- **`TownLights`** puts an unshadowed point light at every street lamp, shop window, inn lantern,
  beacon, lit sign and petrol station canopy light (Forward+ copes with many small lights), and each frame sets the
  emission of every glowing material. Lights fade rather than snap, except the beacons, and are
  disabled when they are off.

Units: `NightLights` colours are linear and its intensities are in Unity's units. Unity treats light
colours as sRGB, so `TownLights` converts them, and emission is written as a raw linear vector
because `Material.SetColor` would treat it as sRGB too. three.js divides diffuse light by π and
Unity doesn't, so the preview multiplies light intensities by π. Both use ACES tone mapping, which
bleaches bright glows towards white, so glow colours are set more saturated than they should look:
the street lamps' glass comes out a creamy warm white and the beacons amber-orange.

## The weather

Like the lights, the rules are engine-free (`Simulation/Weather`) and tested, and Unity only
applies them. Everything is driven from code on stock URP materials, apart from one small shader.

- **Profiles.** An `IWeatherProfile` turns the user's settings and the clock into
  `WeatherConditions`: how hard it is raining or snowing, the wind, strikes a minute and mist. The
  `ThunderstormProfile` makes rain come in surges, the south-westerly wind gust and veer, and storm
  cells drift through so lightning comes in bursts. The `SnowstormProfile` brings snow in squalls
  on a gusty north-easterly, with the odd strike of thundersnow. `WeatherState.Kind` picks the
  profile; the store keeps the rain and snow settings apart, so switching keeps both as they were.
- **`StormSystem`** (Runtime) samples the profile each frame, moves the wetness and the lying snow
  on, runs the lightning and ticks every `IWeatherEffect` with a `WeatherFrame`. It also raises
  `Struck` and `ThunderArrived` events for the audio. Effects only see the conditions, so the rain
  stops and the snow starts on their own when the profile changes.
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
- **Snow** (`SnowEffect`) is soft flakes born all through a box of air round the camera rather
  than at its top, because the wind carries a flake much further than a raindrop. `SnowFall` sets
  their slow fall, their drift and flutter, and how many fill the air; they end on the same
  `RainCatchMap` as the rain, so none fall indoors.
- **Wet and snowy surfaces.** `Wetness` darkens and glosses each kind of surface: tarmac and
  flagstones most, render and paint less, glass not at all. Surfaces soak up in about half a minute
  of heavy rain. `SnowCover` whitens them as snow lies: each kind over its own part of the build-up,
  roofs and grass first, pavements next and the road last, as a grey slush. Walls, glass and water
  take none. Snow settles in a minute or two, thaws over several and washes away in rain.
  `SurfaceWeather` puts the two together (snow on top of wet) for `WeatheredSurfacesEffect`. A whole
  material changes at once, so snow can't sit on top of a wall whose sides share its material.
- **Fog.** `WeatherFog` thickens the fog a little in mist and a lot in heavy, wind-driven snow, and
  pales it towards grey-white at the time of day's brightness. Lying snow also lifts the ambient
  light from below. Both reach `TimeOfDayLighting` through `WeatherLighting`.
- **Water.** `RippleField` makes looping, tileable normal maps of raindrop rings (and a swell for
  open water). They play as flipbooks on puddles, the lake and the river, and the river's texture
  slides downstream.
- **Mist** is big soft particles near the ground plus thicker fog; in the snowstorm the same puffs
  are whiter and driven faster, as blowing snow. **Chimney smoke** comes from the chimneys with a
  fire lit (more in the evening, and more again with snow lying): it rises, slows as it cools and
  bends downwind.
- **Wind sway** moves plant vertices on the CPU, only near the camera. `WindSway.Motion` is worked
  out once per 2.5 m patch (gusts roll across the land as waves, so neighbours move together), and
  each vertex scales it by its own bend.

## Controls and sound

- **Walking.** V swaps the free-fly camera for `WalkingController`: a Unity character controller
  at eye height, moved by `WalkMotion` (engine-free and tested: an adult's size, walking and
  jogging pace, a small jump, steps up kerbs and stairs). `TownColliders` makes the town solid as
  soon as it is built: it cooks a mesh collider for every mesh whose `MeshCategory` is solid
  (ground, fells, structures, buildings, street furniture) on worker threads with
  `Physics.BakeMesh`, and puts a capsule round every tree from its `TreeTrunk` anchor; plants,
  water and markings stay soft. Walking starts on the nearest open, dry ground below the camera
  and the river's edge turns you back. Anything implementing `IInteractable` that the crosshair
  is on within reach shows its prompt in `WalkingHud` and is used with E or a click.
- **Doors.** `SwingingDoor` turns each `TownDoor` into a hinge with the leaf under it, and is the
  `IInteractable` that swings it open into the room or shut again, about the door's own axis (so it
  also opens the roof windows). In play mode the leaf gets a
  convex collider on a kinematic body, so a shut door blocks you and an open one stands aside.
- **The burglar alarms.** `BurglarAlarm` (engine-free and tested) is a control panel's logic:
  - Setting and unsetting: the code, a 30 second exit time after setting, a 30 second entry time
    once it's set and a zone sees something, then the bell box's sounder (which cuts out after 20
    minutes) and the strobe (which flashes until it's unset), and the beeps, once a second and
    twice a second for the last ten.
  - Inside the control box: each zone's wire can be cut (that zone then sees nothing); the
    engineer code puts it into engineer mode, without which opening the lid trips the tamper and
    sets it off, set or not; without mains it runs ten minutes on its standby battery, then dies,
    and the bell box, losing the panel, sounds for two minutes on its own battery; the network
    cable and the battery each show a fault when unplugged; and wiring the bell box's power the
    wrong way round blows its fuse, silencing it (even on its own battery) and showing a fault
    until it's wired right and given a new fuse.
  `MotionSensor` decides what a sensor's wide, downward-tilted cone covers. In Unity, one
  `AlarmSystem` per `TownAlarm` watches the walker through each sensor (a line of sight, so walls
  and shut doors hide you; only movement counts) and its doors opening or shutting; lights each
  sensor's LED, and the keypads' power and fault lights (`AlarmGlow`, a little light of its own
  for each); flashes the strobe and a blue light once a second; and plays the sounder and beeps
  from `AlarmSounds` (a piezo tone sweeping between 2.4 and 3.6 kHz five times a second, looped
  seamlessly). `AlarmKeypad` brings up the keypad (Set, Unset and Eng) and `AlarmControlBox` the
  inside of the box, drawn: the board with its microcontroller and lights, the zone wires, the
  mains supply, battery, network cable and the bell box's wires through fuse F1, with a button for
  each. Either pauses the walker and frees the mouse while `TownscapeShortcuts` stands aside. Both
  can only be reached from their own side of the wall. Like a door's open or shut, the alarms'
  state isn't in the store.
- **Fibre broadband.** `StreetCabinet` (engine-free and tested) is the cabinet's logic: a line per
  customer on its own switch port, plugged in or out at the patch tray; a line plugged back in
  takes eight seconds to get in sync. After a quiet minute, faults turn up at random (about every
  four minutes, one at a time): a dirty connector (weaker light, slower, a little loss), a fibre
  bent too tight (so little light that it keeps dropping, with heavy loss and jitter), a failing
  switch port (good light, but errors climbing and packets lost) or an overloaded uplink (every
  line slow, long pings). `RunTest` gives what a speed test finds (light in dBm, speeds, ping,
  jitter, loss, port errors) and `Diagnose` fixes the fault if it's named rightly. `Ont` and
  `Router` say what each customer's lights show: the ONT's loss-of-signal light and the router's
  internet light blink red with no light, and the fibre light blinks green and the internet light
  amber while it gets in sync. In Unity, `StreetCabinetSystem` runs it, lights the ONTs, routers and
  the kit in the cabinet (`AlarmGlow`), and tells each building's `BurglarAlarm` whether its
  internet is up (`ConnectInternet`: a comms fault while it's down). Open the cabinet's doors
  (`SwingingDoor`) and `StreetCabinetRack` brings up its window: the rack drawn, the patch tray's
  plugs, the screen running a speed test (a ping, then the download and upload climbing) and
  buttons to name the fault. `AlarmSystem` and `StreetCabinetSystem` are both `IScreenWindow`s, which
  `TownscapeShortcuts` stands aside for while one is up.
- **`ControlPanel`** is drawn with Unity's immediate-mode GUI and a skin made in code
  (`PanelSkin`), so it needs no assets, works with either input system and scales with the screen.
  Like `TownscapeShortcuts`, it only dispatches actions. Its weather section switches between the
  thunderstorm and the snowstorm and shows the rain or the snow slider to match. The time slider dispatches
  `SetTargetHour(hour, Scrub: true)`, which sets a very short blend so the clock follows the hand.
- **`PerformanceOverlay`** sits in the top-right corner, apart from the panel so it stays up when
  the panel is hidden. It shows the frame rate and the slowest recent frame (`FrameTimes`), and in
  its detailed view the GPU time where the platform reports it, how long the town took to build,
  each storm effect's and the lights' cost per frame (timed and smoothed by `StormSystem` and
  `TownLights`), and the render counters from Unity's `ProfilerRecorder`.
- **`TownAudio`** plays rain and wind loops (the rain falls silent in the snowstorm, as its volume
  follows the conditions), the river from the nearest point on its course, and
  thunder from a pool of voices placed towards each strike when `StormSystem.ThunderArrived` fires.
  Volumes come from `AudioMix`. Any clip slot left empty on the bootstrap is filled by
  `ProceduralSounds` on a background thread, so play mode never stalls: rain as a soft patter of
  drops on an umbrella, the river as a smooth rush with hundreds of tiny bubbles a second, wind as
  a dull roar, and thunder that cracks when close and only rumbles from afar. The loops are
  seamless, and tests hold the rain to almost no hiss and the river to a steady, unchoppy level.

## Fellside Coffee

A small management game in the village's coffee shop; [COFFEE_SHOP.md](COFFEE_SHOP.md) covers the
design, the rules and the balance numbers. In outline:

- **`Townscape.CoffeeShop`** is the whole game, engine-free: balance data, a seeded random number
  generator, the prep plan, the trading day, the reducer, the JSON save, and `NewsDesk`, which
  writes the morning paper's front page. It has its own
  `Store<CoffeeShopState>`, separate from the town's.
- Unlike the town's store, this one holds the whole game, because the game *is* state: trading a
  day is a pure calculation from the state and the seed, so the reducer stays pure and every day can
  be replayed exactly.
- **`CoffeeShopGame`** (Runtime) creates the store, loads and saves `fellside-coffee.json` in
  `Application.persistentDataPath`, and glides the camera to the shopfront with
  `FreeFlyCamera.FlyTo`. When the camera arrives, **`CoffeeShopPaper`** fills the screen with the
  game as a newspaper (IMGUI, on `PaperSkin`: newsprint, an OS serif, boxed adverts), and holds the
  camera still while it's read. Like the panel, it only dispatches actions.
- `ShopLocator` (Generation) finds the building a shop is in.

## Editor

- `TownscapeProjectSetup` runs on editor load and is idempotent. It creates and assigns the URP
  pipeline asset (`Assets/Townscape/Settings/`, Forward+, MSAA, soft shadows), switches to linear
  colour and adds the scene to the build.
- `ShaderVariantKeeper` (part of the setup) saves one small material per shader and keyword
  combination the town switches on at runtime in `Assets/Townscape/Resources/Shader Variants/`. All
  the town's materials are made in code, so without these a player build would leave out
  transparent glass, glow, ripples, the weather particles and the bolt shader.
- The **Townscape** menu has Open Town Scene, Rebuild Town, Set Up Project and Create Town Scene (which
  rebuilds the scene from code if it is ever lost), and **Fellside Coffee > Show Save File / Delete
  Save File**.

## Checking work without Unity

| Tool | What it does |
|---|---|
| `dotnet test tools/verify/CoreTests` | Runs every EditMode test (State, Generation, Simulation and CoffeeShop) under .NET 8 |
| `dotnet build tools/verify/UnityCompile/Editor.csproj` | Compiles every assembly the way Unity splits them, against Unity reference assemblies and URP/Input System signature stubs, and the EditMode tests against NUnit 3.5, the older NUnit that Unity's Test Framework ships |
| `tools/preview` | Runs the real generator, exports glTF, the night lights and the storm, and renders PNGs with three.js in headless Chromium |
| `dotnet run -c Release --project tools/coffee-sim` | Plays 200 seeded games of Fellside Coffee with simple players and reports profit, waste, misses, the queue and what upgrades earn |
| `python3 tools/generate_meta.py` | Creates `.meta` files with GUIDs derived from the path, so references can be written by hand |

The compile check uses an older Unity's reference assemblies, so code behind newer version checks
compiles through its `#else` branch. The editor remains the final word.

## Conventions

- New files under `Assets/` need a `.meta`: run `python3 tools/generate_meta.py`. Move a file's
  `.meta` along with it.
- Keep generation and simulation engine-free. If it needs `UnityEngine`, it belongs in Runtime.
- Prefer a new strategy over a new branch in an existing generator.
