# Townscape requirements

A cosy, small, low poly town in Unity, in an endless thunderstorm. These are the requirements agreed
before work started; later changes should be recorded here too.

## Platform and tech

| | Decision |
|---|---|
| Engine | Unity 6.6 (`6000.6.x`), latest at the time of writing |
| Render pipeline | Universal Render Pipeline (URP 17.6), Forward+ for many small lights |
| Language | C# |
| Target | Desktop: macOS and Windows |
| Models | Generated from C# code. No imported models, so everything lives in the repository and is tweakable by parameters |
| Workflow | Feature branches and pull requests into `main` |

## The town

- **Size:** small, almost a village. The detailed core is roughly 200 × 200 m, with fells all around.
- **Style:** 3D low poly, flat shaded, UK architecture. **Lake District** village feel: grey stone,
  whitewashed cottages, dark slate roofs, with a few painted pastel shopfronts on the high street.
- **Centrepiece:** a small river crossed by a stone humpback bridge.
- **High street:** a main road through the village, lined with terraces.
  - Terraced buildings have a shop on the ground floor (shop windows and shopfront) and homes on the
    first and second floors, plus loft space.
  - Must-have shops: **bookshops**, **coffee shops**, a **computer shop**, a **cosy newsagent**.
    Other units can be a pub, bakery and chippy.
- **Smaller roads:** narrow lanes off the high street, like Lake District villages or Brighton's lanes.
- **Smaller buildings:** detached shops and detached houses.
- **Green space:** trees, grass and flowers, with green spaces at the edges of the village. Riverside
  footpaths, a small park and a church spire as a landmark.
- **Out of town:** the river runs out into a valley with a lake, and the fells surround the village.

## Roads and street furniture

- Road markings: centre lines, double yellow lines, give-way lines at junctions, and a zebra crossing
  with zig-zags and flashing orange (Belisha) beacons.
- Street furniture: old (Victorian-style) street lights, red K6 telephone boxes, a red post box,
  benches, a bus stop and hanging flower baskets.

## Weather

- The headline feature. For now it **always rains**, with **frequent thunderstorms**: cosy and stormy.
- Effects:
  - rain that follows the camera, with splashes
  - wet, reflective roads with puddle ripples
  - low mist
  - lightning that lights the whole scene, with an occasional visible bolt
  - thunder that arrives after the flash, delayed by distance
  - wind that sways the trees and slants the rain
  - the river flowing
- Controls for rain intensity, lightning frequency and wind strength.
- Built as swappable weather profiles, so clear skies, fog or snow can be added later.

## Time of day

- Dawn, day, dusk and night, selectable by the user.
- A continuous 24-hour clock underneath. Presets blend over a few seconds rather than snapping.
- An optional auto-cycle that runs the clock forward.
- Controls: an on-screen panel (preset buttons, a time slider), number keys 1–4 and an auto-cycle toggle.

## Lighting

- Buildings are lit: windows light up (one by one) at dusk, with the odd flicker.
- Street lamps and other light sources across the map.
- Ambience: chimney smoke, a swinging pub sign and a few parked cars. No people or moving traffic for now.

## Camera

- **Free-fly camera:** hold the right mouse button to look, WASD to move, Q/E for down/up, Shift to go
  faster, and the scroll wheel to change speed. It stays inside the map and above the ground and
  water, but does not collide with buildings.

## Audio

- Audio system with slots for a rain loop, thunder (delayed by distance) and the river. The sound
  files themselves are added later (for example free sounds from freesound.org).

## Code

- Use the **strategy pattern** where behaviour varies: layout, building types, shopfronts, roofs,
  road markings, weather profiles, input backends and camera modes.
- Use **data assets** where only values vary: palettes, lighting profiles, shop definitions.
- **Redux-style state** for user-facing settings (time of day, weather sliders): a single store with
  actions and pure reducers. Per-frame simulation values stay inside the systems that animate them.
  See [ARCHITECTURE.md](ARCHITECTURE.md).

## Milestones

1. **Foundation:** URP project setup, state store, generated ground, river, roads, markings and the
   bridge; basic time-of-day lighting; free-fly camera. *(done)*
2. **Buildings and streets**, in two parts:
   - **2a Buildings:** terraces with shops and flats, detached houses and shops, roofs, chimneys and
     dormers, the church and the old mill. *(done)*
   - **2b Street furniture and greenery:** lamps, phone and post boxes, benches, beacons, railings,
     trees, grass, flowers and dry-stone walls. *(done)*
3. **Lighting:** full time-of-day treatment, lit windows, street lamps and light sources.
   *(this pull request)*
4. **Storm:** rain, splashes, lightning, thunder timing, wet surfaces, puddles, mist and wind.
5. **Finish:** control panel UI, audio hooks and polish. Includes a player build that keeps the
   shader variants the runtime materials switch on (transparent glass, emission).
