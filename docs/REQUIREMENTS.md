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
  files themselves are added later (for example free sounds from freesound.org). Until then, the
  sounds are synthesised in code.
- Volume and mute controls.

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
3. **Lighting:** full time-of-day treatment, lit windows, street lamps and light sources. *(done)*
4. **Storm:** rain, splashes, lightning, thunder timing, wet surfaces, puddles, mist and wind, with
   keyboard controls for rain, lightning and wind until the control panel arrives. *(done)*
5. **Finish:** control panel UI, audio hooks and polish. Includes a player build that keeps the
   shaders and variants the runtime materials use (transparent glass, emission, normal maps, URP
   particles and the lightning bolt shader), and remembering settings between sessions. *(done)*
6. **Fellside Coffee, minimum playable slice:** a coffee shop management game in the village's coffee
   shop. One site, two customer segments, three menu items (a latte with a milk choice, a pot of tea,
   croissants), prep, a simulated day, a review, profit carried between days, a cash reserve, and one
   upgrade that changes a decision (a bigger display case). Saved as JSON. Played as a full-screen
   morning newspaper: yesterday's results on the front page, buying in the classifieds.
   *(this pull request)*

## Fellside Coffee

- **Two loops.** Daily: prep and stock, trade for the day, review. Progression: profit buys upgrades,
  which change the daily loop. Every decision is a trade-off under uncertainty: limited capacity,
  unknown demand, perishable stock. Upgrades unlock decisions, not just bigger numbers.
- **Stock by behaviour.** Ready-to-sell items (pastries) are perishable, one per customer and wasted if
  unsold. Drinks are made from ingredients worked out from the menu, with one milk choice (dairy or an
  alternative). Cups, lids and ice are topped up automatically. Unmakeable items are flagged.
- **Customers** come from segments with their own tastes, budgets and dietary needs, in a mix that
  changes with the time of day and the site. A queue and a serving rate mean busy periods lose
  customers. Demand is random but seeded, so a day always replays the same.
- **Review:** sold, wasted and missed (with reasons), money, how each segment was served, and what
  each upgrade earned compared with the same day without it.
- **Presented as a newspaper** filling the screen: each morning's paper reports the day before on
  its front page and carries classifieds where stock and upgrades are bought.
- **Upgrades** have a price, an effect and at least one trade-off (upkeep, space or opportunity). They
  can't spend the reserve, and work from the next day traded.
- **Instant results, fixed prices, Unity only.** Game logic is engine-free and tested; every balance
  number is data. Saved as JSON in the persistent data folder.
- **Out of scope:** multiplayer, real payments, art or animation polish, narrative.

## Ideas for later

- More weather profiles: clear skies, fog, snow (a new `IWeatherProfile` each).
- Wind sway on the GPU (a vertex shader) if the CPU version shows up in the profiler.
- People and traffic: a few walkers with umbrellas, a bus that stops at the bus stop.
- A pelican crossing with lights on another road.
- A first-person walking camera alongside the free-fly one.
- Fellside Coffee beyond the slice: more upgrades (a faster machine, cold drinks, seating and music, a
  second milk, a pop-up at another site), syrups, more segments, regulars who remember their usual
  order, and footfall that follows the town's weather. See [COFFEE_SHOP.md](COFFEE_SHOP.md).
