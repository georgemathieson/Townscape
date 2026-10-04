# Preview renders

Renders the generated town without Unity, so changes to the generators can be checked quickly (and
in environments where Unity isn't available). It runs the real `Townscape.Generation` code, exports
the meshes to glTF and renders PNGs with three.js in headless Chromium.

```sh
dotnet run -c Release --project tools/preview/Exporter -- tools/preview/out
cd tools/preview && npm install && node render.mjs out
# renders land in tools/preview/out/renders/
node render.mjs out bridge:dusk street:night   # specific view:preset pairs
node render.mjs out coffee:dusk                # the Fellside Coffee game's camera
node render.mjs out petrol:dusk petrolWindow:night   # Fell View Garage, and its shop window
node render.mjs out village:day:snow street:night:snow  # in the snowstorm
node render.mjs out kettle:dusk kettleCafe:night kettleAttic:day  # the Copper Kettle, inside and out
node render.mjs out phoneBox:dusk phoneBoxInside:night cabinetOpen:day  # the phone box and the fibre cabinet
node render.mjs out millWorks:dusk millArc:night millReception:day  # Mill Works and its alarm receiving centre
```

Views and lighting presets (`day`, `dusk`, `night`, and `flash` for night at the peak of a
lightning strike) are defined in `page.html`, and a third part, `:snow`, renders the snowstorm with
the snow lying deep instead of the thunderstorm; views that depend on the town, such as `coffee` (where
the Fellside Coffee game takes the camera) and the petrol station's `petrol`, `petrolAbove`,
`petrolNear` and `petrolWindow`, are worked out by the exporter into `views.json`, as are the
Copper Kettle's (`kettle` from the street, then inside: `kettleCafe`, `kettleCounter`, `kettleStore`,
`kettleHall`, `kettleLanding`, `kettleLiving`, `kettleKitchen`, `kettleBedroom`, `kettleBathroom` and
`kettleAttic`, `kettleSnug` and `kettleRoofWindows`, the alarms' `kettleKeypad`, `kettleCafeKeypad`, `kettleSensor`, `kettleShopControlBox` and `kettleFlatControlBox`, `kettleSign` with the café's bell box, the ONT and router in the café's storeroom and the flat's living room, `kettleShopBroadband` and `kettleFlatBroadband`, and `kettleBack` from behind the terrace and `kettleBellBox` from the street), which are drawn without rain or snow, Mill Works (`millWorks` from the lane, then `millReception`, `millDesks`, `millStairs`, `millComms`, `millLanding`, the alarm receiving centre `millArc` and `millArcDesk`, and `millMeeting`), and the phone box and fibre cabinet (`phoneBox` from the street, `phoneBoxOpen` with its door open and `phoneBoxInside`, and the cabinet with its doors open, `cabinetOpen` and close up at the rack, `cabinetRack`). Doors are exported both shut and open, and a view lists the ones it wants open. The exporter also writes `lights.json` and
`storm.json`, worked out by the same `Townscape.Simulation` code Unity uses: glowing materials and
lamps, wet surfaces, rain, ripples, chimney smoke and bolts. Rain and smoke are frozen in a single
moment and only approximate the particles in Unity: judge geometry, layout and colours here, and
lighting and motion in Unity.

`render.mjs` expects Chromium at `/opt/pw-browsers/chromium`; set `CHROMIUM_PATH` to use another.
