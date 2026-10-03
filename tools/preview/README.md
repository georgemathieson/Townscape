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

Views and lighting presets (`day`, `dusk`, `night`, and `flash` for night at the peak of a
lightning strike) are defined in `page.html`. The exporter also writes `lights.json` and
`storm.json`, worked out by the same `Townscape.Simulation` code Unity uses: glowing materials and
lamps, wet surfaces, rain, ripples, chimney smoke and bolts. Rain and smoke are frozen in a single
moment and only approximate the particles in Unity: judge geometry, layout and colours here, and
lighting and motion in Unity.

`render.mjs` expects Chromium at `/opt/pw-browsers/chromium`; set `CHROMIUM_PATH` to use another.
