# Townscape

![The high street at dusk in the rain: wet tarmac, glowing shop windows and lit homes above](docs/images/gallery/high-street-dusk.jpg)

A cosy, small, low poly Lake District village in a never-ending thunderstorm (or, at the press of a
key, a snowstorm), built in Unity 6 (URP) with C#. Everything you see is generated from code: the
ground, roads and markings, the river and the stone humpback bridge, the terraces and shops, street
furniture and trees, the fells around the village, the lamps and windows that light it after dark,
the weather (rain, snow, lightning, wind and mist), and even its sounds.

- [Requirements](docs/REQUIREMENTS.md): what we're building, and the milestone plan
- [Architecture](docs/ARCHITECTURE.md): how the code is organised and why

## Gallery

### Day and night

| | |
|---|---|
| ![The high street on a wet afternoon](docs/images/gallery/high-street-day.jpg) | ![The same street at night, lit by lamps, shop windows and homes](docs/images/gallery/high-street-night.jpg) |
| A wet afternoon on the high street | The same street at night: street lamps, shop windows, and homes lit one by one |

### The storm

| | |
|---|---|
| ![A lightning bolt at the far end of the high street](docs/images/gallery/lightning-high-street.jpg) | ![A strike lighting up the whole village and the river](docs/images/gallery/lightning-village.jpg) |
| Lightning at the end of the high street | A strike lights up the whole village |
| ![Wet tarmac and puddles reflecting the lamps at dusk](docs/images/gallery/wet-street-dusk.jpg) | ![Chimney smoke streaming downwind over the rooftops](docs/images/gallery/chimney-smoke-dusk.jpg) |
| Wet tarmac at dusk, with puddles in the gutters | Smoke from the chimneys streams away downwind |

### The snowstorm

| | |
|---|---|
| ![Snow lying on the roofs and fields of the village, falling thickly](docs/images/gallery/snowstorm-village-day.jpg) | ![The snowy village at dusk with the lamps and windows lit](docs/images/gallery/snowstorm-village-dusk.jpg) |
| Snow lying deep on the roofs and the fields | The snowy village as the lamps come on |

### The village

| | |
|---|---|
| ![Fellside Coffee and Lantern Books after dark](docs/images/gallery/bookshop-night.jpg) | ![The village across the valley at dusk](docs/images/gallery/village-across-the-valley.jpg) |
| Fellside Coffee and Lantern Books after dark | The village from across the valley at dusk |
| ![Fell View Garage at dusk, its canopy lit over the pumps](docs/images/gallery/petrol-station-dusk.jpg) | ![String lights and bunting in the garage's shop window at night](docs/images/gallery/petrol-station-window-night.jpg) |
| Fell View Garage at dusk, with bunting from the canopy | String lights in the shop window after dark |

### The Copper Kettle

You can go inside the Copper Kettle and the flat above it: press V to walk, then E at either door.
The café door is on the right, in the glazed lobby; the door on the left opens onto the stairs.

| | |
|---|---|
| ![Inside the Copper Kettle café at night](docs/images/gallery/kettle-cafe-night.jpg) | ![The counter, cake cabinet and espresso machine](docs/images/gallery/kettle-counter-day.jpg) |
| The café after dark | The counter, the cakes and the espresso machine |
| ![The flat's living room at night, lit by a pendant lamp](docs/images/gallery/kettle-living-night.jpg) | ![The attic snug at night, with pictures on the walls](docs/images/gallery/kettle-snug-night.jpg) |
| The flat's living room in the evening | The attic: a snug and a study under the slates |
| ![Two roof windows over the attic's sofa](docs/images/gallery/kettle-roof-windows-day.jpg) | ![The roof windows in the back slope, from outside](docs/images/gallery/kettle-back-dusk.jpg) |
| Roof windows over the snug: press E to tip one open | The same windows from behind the terrace at dusk |
| ![The white bell box with its blue strobe, between two second-floor windows](docs/images/gallery/kettle-bell-box-day.jpg) | ![The alarm keypad on the hall wall inside the flat's door](docs/images/gallery/kettle-keypad-day.jpg) |
| The burglar alarm's bell box, high between two windows | Its keypad, just inside the flat's front door |

It has a burglar alarm too. Press E on the keypad in the hall, type the code (**1234**) and press
**Set**, then you have 30 seconds to get out. Once it's set, a sensor seeing you (or a door
opening) gives you 30 seconds to get back to the keypad and **Unset** it, or the bell box's piezo
sounder wails and its strobe flashes.

*Rendered by [`tools/preview`](tools/preview/README.md), which draws the generated town with three.js
outside Unity. In Unity the rain, snow and smoke move, the water ripples and the lightning flickers.*

## Getting started (macOS or Windows)

1. Install [Unity Hub](https://unity.com/download), then install **Unity 6.6** (`6000.6.3f1`, or any
   later `6000.6.x`). No extra modules are needed to run in the editor.
2. In Unity Hub choose **Add > Add project from disk** and pick this folder. If Hub asks about the
   editor version, choose your installed 6.6.
3. On first open Unity imports the packages. If the Input System asks to **enable the new input
   backends**, click **Yes**; the editor restarts.
4. The project sets itself up: it creates and assigns the URP pipeline asset, switches to linear colour
   and opens `Assets/Scenes/Town.unity`. The Console shows a `[Townscape] Project setup: ...` line.
   You can rerun this at any time with **Townscape > Set Up Project**.
5. Press **Play**.

After the first open, Unity writes its own settings files (`ProjectSettings/*.asset`,
`Packages/packages-lock.json`, `Assets/Townscape/Settings/` and `Assets/Townscape/Resources/`, which
keeps the shaders the town needs in player builds). Commit them so everyone gets the same setup.

To make a standalone app, use **File > Build Profiles** with the town scene (already in the build
list) for macOS or Windows.

## Controls

The panel in the top-left corner has everything: the time of day (presets, a time slider, and a
clock you can run at any speed), the weather (a thunderstorm or a snowstorm; rain or snow, lightning
and wind sliders; and a button for a lightning strike) and the volume. Your settings are remembered between sessions; **Reset all**
puts them back. Everything also has a key:

| Input | Action |
|---|---|
| Hold right mouse | Look around |
| W A S D | Move |
| Q / E | Down / up |
| Shift | Move faster |
| Scroll wheel | Change speed |
| 1 / 2 / 3 / 4 | Dawn / Day / Dusk / Night |
| [ / ] | An hour earlier / later |
| T | Start or stop the clock |
| N | Switch between the thunderstorm and the snowstorm |
| R | More rain, or more snow in the snowstorm (cycles back to light) |
| L | More lightning (cycles back to none) |
| G | More wind (cycles back to calm) |
| B | A lightning strike now |
| M | Mute or unmute |
| F | Frame rate readout: off, frame rate, or a breakdown of where the time goes |
| H | Hide or show the panel |
| C | Open or close Fellside Coffee |
| V | Walk about at eye level, or go back to flying |
| Space | Jump (walking) |
| E or left click | Open or close what the dot in the middle is on (walking), or bring up the alarm's keypad |
| 0–9, Enter, Esc | On the alarm keypad: type the code, set or unset, close |

## Fellside Coffee

There's a small management game in the village's coffee shop. Press **C** (or **Run the coffee
shop** on the panel) and the camera glides to Fellside Coffee on the high street, where the morning
paper, *The Fellside Herald*, fills the screen. The front page reports how yesterday went: what
sold, what was wasted and what customers couldn't get. In the classifieds you order the day's
stock from the suppliers, choose dairy or oat milk, buy upgrades and keep some cash back for bad
days. Then open for the day, and read about it in tomorrow's paper. The game saves itself to
`fellside-coffee.json` in Unity's persistent data folder (**Townscape > Fellside Coffee > Show Save
File** finds it). See [docs/COFFEE_SHOP.md](docs/COFFEE_SHOP.md) for how it works and how to tune
it.

## Sounds

Rain, wind, the river and thunder are synthesised in code, so the village is never silent. To use
real recordings instead (for example from [freesound.org](https://freesound.org)), select the
**Townscape** object in the scene and drop clips into **Sounds**: seamless loops for rain, wind and
the river, and any number of close cracks and distant rumbles for thunder. Empty slots keep the
generated sound. Thunder plays from the direction of each strike, and arrives late the further away
it was.

## Working on it

- **Tests:** in Unity, **Window > General > Test Runner > EditMode > Run All**. The same tests run
  without Unity with `dotnet test tools/verify/CoreTests`.
- **Compile check without Unity:** `dotnet build tools/verify/UnityCompile/Editor.csproj`.
- **Preview renders without Unity:** see [tools/preview](tools/preview/README.md).
- **Coffee shop balance:** `dotnet run -c Release --project tools/coffee-sim` plays many games and
  shows what the numbers in `DefaultBalance` add up to.
- **New files under `Assets/`** need a `.meta`: run `python3 tools/generate_meta.py`.
- The **Townscape** menu has Open Town Scene, Rebuild Town, Set Up Project and Create Town Scene, and
  shows or deletes the Fellside Coffee save.
