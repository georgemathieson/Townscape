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
- **Petrol station:** a little village garage on the high street: a whitewashed shop and workshop
  with a pantile roof, two pumps on an island parallel to it under a small flat canopy, a price sign,
  string lights in the shop windows (red, green, orange, yellow and blue) and bunting.
- **Green space:** trees, grass and flowers, with green spaces at the edges of the village. Riverside
  footpaths, a small park and a church spire as a landmark.
- **Out of town:** the river runs out into a valley with a lake, and the fells surround the village.

## Roads and street furniture

- Road markings: centre lines, double yellow lines, give-way lines at junctions, and a zebra crossing
  with zig-zags and flashing orange (Belisha) beacons.
- Street furniture: old (Victorian-style) street lights, red K6 telephone boxes, a red post box,
  benches, a bus stop and hanging flower baskets.

## Weather

- The headline feature. It **always rains**, with **frequent thunderstorms**: cosy and stormy. Or
  switch to a **snowstorm**.
- Effects:
  - rain that follows the camera, with splashes
  - wet, reflective roads with puddle ripples
  - low mist
  - lightning that lights the whole scene, with an occasional visible bolt
  - thunder that arrives after the flash, delayed by distance
  - wind that sways the trees and slants the rain
  - the river flowing
- The snowstorm:
  - snow that drifts down round the camera, carried and swirled by the wind
  - snow settling over a minute or two: roofs and fields first, then pavements, while the road
    stays a grey slush; it thaws when the snow stops, and the rain washes it away
  - whiter, thicker fog in heavy snow, closing right in during a blizzard, and blowing snow
  - more chimney fires lit in the cold, and the odd rumble of thundersnow
- Controls to switch between the thunderstorm and the snowstorm, and for rain or snow intensity,
  lightning frequency and wind strength.
- Built as swappable weather profiles, so clear skies or fog can be added later.

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
   morning newspaper: yesterday's results on the front page, buying in the classifieds. *(done)*
7. **Fell View Garage:** a village petrol station at the east end of the high street, on a concrete
   forecourt: the garage, pumps, canopy lights, price sign, string lights that twinkle after dark,
   and bunting. *(done)*
8. **Snowstorm:** a second weather profile alongside the thunderstorm: falling snow, snow that
   settles and thaws, whiteout fog, blowing snow, a weather switch on the panel and the N key.
   *(done)*
9. **Walking mode:** walk the village at eye level (V), bumping into buildings, walls, street
   furniture and tree trunks, with a crosshair and E to use things. *(done)*
10. **The Copper Kettle:** go inside the café and the flat above it through doors that open. The café
    has a counter and till, a cake cabinet, an espresso machine, tables and a storeroom; the flat has
    a living room and kitchen, a bedroom and bathroom, and an attic study and snug under the dormer, up a
    dog-leg stair. Its windows are clear glass you can see in and out of. *(done)*
    - **Follow-ups:** roof windows over the attic snug that pivot open, window frames that read from
      inside and faintly tinted glass, doorways kept clear of furniture, and ceiling lights you can't
      catch your head on. *(done)*
11. **Burglar alarm:** the Copper Kettle gets an alarm: a motion sensor high in the corner of every
    room, a small keypad inside each front door (the flat's hall and the café), and a white bell box with a
    blue strobe at its foot, high on the front between two second-floor windows. Use either keypad to set
    it with the code (a 30 second exit time) and unset it (a 30 second entry time once a sensor sees
    you or a door opens); if nobody does, its piezo sounder wails and the strobe flashes. Also straightens
    the back windows, which sat off to one side of their openings inside. *(done)*
12. **Two alarms, and their control boxes:** the café and the flat get separate alarms, each with
    its own keypad, sensors, door contacts and bell box (the café's red, on its sign; the flat's
    white). Each alarm's zones are wired back to a white control box (the café's in its storeroom,
    the flat's in the attic). With the engineer code put in first, its lid comes off without
    setting off the tamper, to show the board: cut zones, disconnect the battery (a fault on the
    keypad), pull the mains (ten minutes on the battery, then the panel dies and the bell box sounds
    for two on its own), unplug the network, and wire the bell box's power backwards to blow its fuse
    and silence it until it's put right. The strobe flashes once a cycle. *(done)*
13. **Fibre broadband, and inside the phone box:** a dark green street cabinet next to the phone
    box carries fibre to the two buildings we can go inside (the café and the flat). Behind its
    double doors is a rack: a power strip, a UPS and its battery, a fibre switch, an edge router and
    a patch tray, with a little computer and screen beside it. Each building has an ONT (where the
    fibre ends) and a router on the wall. Unplug a customer at the patch tray and their ONT's
    loss-of-signal light and their router's internet light blink red (and their burglar alarm shows
    a comms fault); plug them back in and the lights blink while it gets back in sync. Faults turn up
    now and then, one at a time (a dirty connector, a fibre bent too tight, a failing switch port, an
    overloaded uplink): speed tests from the screen show the light level, speeds, ping, loss and port
    errors, and naming the fault rightly gets it fixed. The phone box's door opens and you can step
    inside, where there's a payphone, a 999 card and the directories, lit at night. *(this pull request)*

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

- More weather profiles: clear skies, fog (a new `IWeatherProfile` each).
- Snow on the tops of walls, fences and branches, and footprints in it: these need geometry or a
  shader rather than a change of material colour.
- Wind sway on the GPU (a vertex shader) if the CPU version shows up in the profiler.
- People and traffic: a few walkers with umbrellas, a bus that stops at the bus stop.
- A pelican crossing with lights on another road.
- Fellside Coffee beyond the slice: more upgrades (a faster machine, cold drinks, seating and music, a
  second milk, a pop-up at another site), syrups, more segments, regulars who remember their usual
  order, and footfall that follows the town's weather. See [COFFEE_SHOP.md](COFFEE_SHOP.md).
