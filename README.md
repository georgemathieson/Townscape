# Townscape

A cosy, small, low poly Lake District village in a never-ending thunderstorm, built in Unity 6 (URP)
with C#. Everything you see is generated from code: the ground, roads and markings, the river and
the stone humpback bridge, the terraces and shops, street furniture and trees, the fells around the
village, and the lamps and windows that light it after dark.

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
`Packages/packages-lock.json` and `Assets/Townscape/Settings/`). Commit them so everyone gets the
same setup.

## Controls

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
| H | Show or hide the help |

## Working on it

- **Tests:** in Unity, **Window > General > Test Runner > EditMode > Run All**. The same tests run
  without Unity with `dotnet test tools/verify/CoreTests`.
- **Compile check without Unity:** `dotnet build tools/verify/UnityCompile/Editor.csproj`.
- **Preview renders without Unity:** see [tools/preview](tools/preview/README.md).
- **New files under `Assets/`** need a `.meta`: run `python3 tools/generate_meta.py`.
- The **Townscape** menu has Open Town Scene, Rebuild Town, Set Up Project and Create Town Scene.
