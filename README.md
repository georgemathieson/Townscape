# Townscape

A cosy, small, low poly Lake District village in a never-ending thunderstorm, built in Unity 6 (URP)
with C#. Everything you see is generated from code: the ground, roads and markings, the river and
the stone humpback bridge, the terraces and shops, street furniture and trees, the fells around the
village, the lamps and windows that light it after dark, the storm (rain, lightning, wind and
mist), and even its sounds.

- [Requirements](docs/REQUIREMENTS.md): what we're building, and the milestone plan
- [Architecture](docs/ARCHITECTURE.md): how the code is organised and why

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
clock you can run at any speed), the storm (rain, lightning and wind sliders, and a button for a
lightning strike) and the volume. Your settings are remembered between sessions; **Reset all**
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
| R | More rain (cycles back to light rain) |
| L | More lightning (cycles back to none) |
| G | More wind (cycles back to calm) |
| B | A lightning strike now |
| M | Mute or unmute |
| F | Frame rate readout: off, frame rate, or a breakdown of where the time goes |
| H | Hide or show the panel |

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
- **New files under `Assets/`** need a `.meta`: run `python3 tools/generate_meta.py`.
- The **Townscape** menu has Open Town Scene, Rebuild Town, Set Up Project and Create Town Scene.
