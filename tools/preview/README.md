# Preview renders

Renders the generated town without Unity, so changes to the generators can be checked quickly (and
in environments where Unity isn't available). It runs the real `Townscape.Generation` code, exports
the meshes to glTF and renders PNGs with three.js in headless Chromium.

```sh
dotnet run -c Release --project tools/preview/Exporter -- tools/preview/out
cd tools/preview && npm install && node render.mjs out
# renders land in tools/preview/out/renders/
node render.mjs out bridge:dusk street:night   # specific view:preset pairs
```

Views and lighting presets are defined in `page.html`. The lighting is only an approximation of
the Unity look: judge geometry, layout and colours here, and lighting in Unity.

`render.mjs` expects Chromium at `/opt/pw-browsers/chromium`; set `CHROMIUM_PATH` to use another.
