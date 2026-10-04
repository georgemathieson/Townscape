using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Townscape.Generation;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Buildings.Styles;
using Townscape.Generation.Dressing.Props;
using Townscape.Generation.Layout;

/// <summary>
/// Writes views.json: camera views worked out from the generated town rather than written into
/// page.html: "coffee", the view the Fellside Coffee game glides the camera to, and the petrol
/// station from the far pavement ("petrol"), from above, from its forecourt and at its shop window.
/// </summary>
internal static class ViewsExport
{
    // The game's camera (TownscapeBootstrap.CreateCamera).
    private const float UnityFieldOfView = 55f;

    public static void Write(GeneratedTown town, string path)
    {
        var views = new List<string>();

        var shop = ShopLocator.Find(town.Context.Buildings, VillageShops.FellsideCoffee);
        if (shop != null)
        {
            var view = ShopLocator.ViewOf(shop.Footprint, town.Context.Ground.HeightAt);
            views.Add(View("coffee", view.Eye, view.LookAt, UnityFieldOfView));
        }

        var station = town.Context.Buildings.FirstOrDefault(plan => plan.Style is PetrolStationStyle);
        if (station != null)
        {
            var site = station.Footprint;
            var along = Vector2.Normalize(site.FrontRight - site.FrontLeft);
            var front = (site.FrontLeft + site.FrontRight) * 0.5f;
            views.Add(View("petrol", At(front + (site.Outward * 10.2f) - (along * 7f), 2.1f), At(site.At(0.5f, 0.45f), 2.4f), 60f));
            views.Add(View("petrolAbove", At(front + (site.Outward * 11f) - (along * 13f), 9f), At(site.At(0.55f, 0.4f), 1f), 50f));
            views.Add(View("petrolNear", At(site.At(0.84f, 0.05f), 1.7f), At(site.At(0.3f, 0.6f), 2.2f), 60f));
            views.Add(View("petrolWindow", At(site.At(0.25f, 0.38f), 1.7f), At(site.At(0.27f, 0.6f), 1.75f), 50f));
        }

        // Inside the Copper Kettle: the café, the hall and stairs, and each room of the flat.
        var kettle = ShopLocator.Find(town.Context.Buildings, VillageShops.CopperKettle);
        if (kettle?.Style is TerracedUnitStyle unit && unit.Enterable)
        {
            var space = new UnitSpace(kettle.Footprint);
            var f = CafeAndFlat.Floors(unit.Design);
            void Inside(string name, float x, float y, float d, float lx, float ly, float ld) =>
                views.Add(View(name, space.At(x, y, d), space.At(lx, ly, ld), 70f, indoor: true));
            var outside = ShopLocator.ViewOf(kettle.Footprint, town.Context.Ground.HeightAt);
            views.Add(View("kettle", outside.Eye, outside.LookAt, 60f));
            Inside("kettleCafe", 6.3f, f[0] + 1.6f, 0.9f, 3.4f, f[0] + 1.1f, 5.2f);
            Inside("kettleCounter", 5.4f, f[0] + 1.6f, 5.6f, 2.6f, f[0] + 1.0f, 0.4f);
            Inside("kettleStore", 2.4f, f[0] + 1.6f, 5.4f, 5.5f, f[0] + 0.9f, 9.0f);
            Inside("kettleHall", 0.8f, f[0] + 1.6f, 0.45f, 0.8f, f[0] + 2.7f, 4.5f);
            Inside("kettleLanding", 1.9f, f[1] + 1.6f, 6.6f, 1.9f, f[2] + 0.8f, 1.6f);
            Inside("kettleLiving", 6.6f, f[1] + 1.6f, 0.6f, 3.4f, f[1] + 0.9f, 4.6f);
            Inside("kettleKitchen", 3.0f, f[1] + 1.6f, 5.4f, 5.4f, f[1] + 0.9f, 9.2f);
            Inside("kettleBedroom", 3.0f, f[2] + 1.6f, 0.7f, 5.2f, f[2] + 0.6f, 4.6f);
            Inside("kettleBathroom", 3.2f, f[2] + 1.6f, 5.3f, 5.6f, f[2] + 0.6f, 9.0f);
            Inside("kettleAttic", 2.2f, f[3] + 1.6f, 6.3f, 3.6f, f[3] + 1.3f, 1.2f);
            Inside("kettleSnug", 2.4f, f[3] + 1.6f, 2.6f, 5.4f, f[3] + 0.9f, 7.6f);
            Inside("kettleRoofWindows", 4.3f, f[3] + 1.5f, 5.4f, 4.3f, f[3] + 1.7f, 8.4f);
            Inside("kettleKeypad", 0.6f, f[0] + 1.62f, 1.3f, 1.45f, f[0] + 1.45f, 0.72f);
            Inside("kettleSensor", 5.2f, f[1] + 1.7f, 1.6f, 6.9f, f[1] + 2.4f, 0.2f);
            var lobbyLeft = TraditionalShopfront.Layout(space.Width).DoorLeft;
            Inside("kettleCafeKeypad", lobbyLeft - 1.3f, f[0] + 1.62f, 1.5f, lobbyLeft, f[0] + 1.4f, 0.35f);
            Inside("kettleShopControlBox", 4.0f, f[0] + 1.65f, 7.4f, 1.5f, f[0] + 1.5f, 8.05f);
            Inside("kettleFlatControlBox", 4.6f, f[3] + 1.5f, 3.2f, space.Width - 0.15f, f[3] + 1.1f, 2.0f);
            var sign = TraditionalShopfront.FasciaBellBox(space.Width);
            views.Add(View("kettleSign", space.At(sign.CentreX - 2.5f, f[0] + 1.7f, -5.5f), space.At(sign.CentreX - 0.8f, f[0] + 2.9f, 0f), 45f));
            views.Add(View("kettleBellBox", space.At((space.Width / 3f) + 2f, f[0] + 1.7f, -7f), space.At(space.Width / 3f, f[2] + 1.6f, 0f), 35f));
            Inside("kettleShopBroadband", 2.7f, f[0] + 1.65f, 7.35f, 1.5f, f[0] + 1.5f, 7.6f);
            Inside("kettleFlatBroadband", 3.7f, f[1] + 1.55f, 2.95f, 2.55f, f[1] + 1.3f, 2.95f);
            views.Add(View("kettleBack", space.At(space.Width * 0.5f, f[3] + 4f, space.Depth + 9f), space.At(space.Width * 0.5f, f[3] + 1.2f, space.Depth * 0.7f), 50f));
        }

        // Mill Works: from the lane, and inside on each floor.
        var mill = town.Context.Buildings.FirstOrDefault(plan => plan.Style is MillWorksStyle);
        if (mill != null)
        {
            var space = new UnitSpace(mill.Footprint);
            var f = MillWorks.Floors();
            void Inside(string name, float x, float y, float d, float lx, float ly, float ld) =>
                views.Add(View(name, space.At(x, y, d), space.At(lx, ly, ld), 70f, indoor: true));
            views.Add(View("millWorks", space.At(space.Width * 0.3f, f[0] + 2.2f, -15f), space.At(space.Width * 0.5f, f[0] + 4.6f, 0f), 60f));
            Inside("millReception", 6.0f, f[0] + 1.65f, 0.9f, 2.8f, f[0] + 0.9f, 4.8f);
            Inside("millDesks", 8.6f, f[0] + 1.7f, 6.9f, 2.6f, f[0] + 0.8f, 2.8f);
            Inside("millEntrance", 6.6f, f[0] + 1.65f, 4.2f, 7.4f, f[0] + 1.1f, 0.2f);
            Inside("millStairs", 10.1f, f[0] + 1.6f, 0.45f, 11.3f, f[0] + 2.4f, 5.0f);
            Inside("millComms", 10.65f, f[0] + 1.6f, 4.2f, 9.7f, f[0] + 1.45f, 5.1f);
            Inside("millLanding", 11.4f, f[1] + 1.65f, 7.5f, 9.4f, f[1] + 1.2f, 4.2f);
            Inside("millArc", 9.1f, f[1] + 1.75f, 7.0f, 0.4f, f[1] + 1.4f, 3.4f);
            Inside("millArcDesk", 4.75f, f[1] + 1.35f, 4.05f, 3.1f, f[1] + 1.08f, 4.05f);
            Inside("millMeeting", 9.0f, f[2] + 1.7f, 1.0f, 2.2f, f[2] + 0.9f, 4.6f);
        }

        // The phone box and the fibre cabinet beside it: from the pavement, inside the phone box,
        // and the cabinet with its doors open.
        var phoneDoor = town.Doors.FirstOrDefault(d => d.Name == PhoneBox.DoorName);
        if (phoneDoor != null)
        {
            var box = phoneDoor.Hinge + (phoneDoor.Along * (phoneDoor.Width * 0.5f)) - (phoneDoor.Inward * PhoneBox.Half);
            var ground = phoneDoor.Hinge.Y - 0.1f;
            var front = phoneDoor.Inward;
            var right = phoneDoor.Along;
            var level = new Vector3(box.X, ground, box.Z);
            views.Add(View("phoneBox", level + (front * 4.2f) + (right * 1.4f) + (Vector3.UnitY * 1.65f), level + (right * 0.75f) + (Vector3.UnitY * 1.1f), 55f));
            views.Add(View("phoneBoxOpen", level + (front * 2.4f) - (right * 1.3f) + (Vector3.UnitY * 1.7f), level + (Vector3.UnitY * 1.2f), 60f, open: new[] { PhoneBox.DoorName }));
            views.Add(View("phoneBoxInside", level + (front * 0.12f) + (Vector3.UnitY * 1.72f), level - (front * 0.5f) + (Vector3.UnitY * 1.2f), 75f, indoor: true, open: new[] { PhoneBox.DoorName }));
        }

        var cabinet = town.Cabinets.FirstOrDefault();
        if (cabinet != null)
        {
            var rack = cabinet.Rack.Position;
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, cabinet.Facing));
            var doors = new[] { "Cabinet left door", "Cabinet right door" };
            views.Add(View("cabinetOpen", rack + (cabinet.Facing * 1.5f) + (right * 0.5f) + (Vector3.UnitY * 0.95f), rack + (Vector3.UnitY * 0.05f), 55f, open: doors));
            views.Add(View("cabinetRack", rack + (cabinet.Facing * 0.7f) + (Vector3.UnitY * 0.35f), rack + (Vector3.UnitY * 0.15f), 55f, open: doors));
        }

        // The people: a call-out at the Copper Kettle, the three of them up close, the guard at
        // their post, and a burglar at the till.
        var walking = town.Walking;
        if (walking.Find($"{CafeAndFlat.ShopAlarm}: outside") >= 0)
        {
            Vector3 Stop(string name) => walking.Point(walking.Find(name));
            var outside = Stop($"{CafeAndFlat.ShopAlarm}: outside");
            var inward = CastExport.Inward(walking, $"{CafeAndFlat.ShopAlarm}: outside");
            var along = new Vector3(inward.Z, 0f, -inward.X);
            var up = Vector3.UnitY;
            views.Add(View("peopleCallout", outside - (inward * 8.2f) - (along * 9f) + (up * 4.5f), outside - (inward * 2.5f) + (along * 2f) + (up * 0.8f), 55f, open: new[] { "Shop door" }, cast: "callout"));
            views.Add(View("peopleLineup", outside - (along * 6f) - (inward * 4.2f) + (up * 1.4f), outside - (along * 6f) + (up * 1.0f), 40f, cast: "lineup"));
            views.Add(View("policeCar", outside - (inward * 7f) + (along * 5.5f) + (up * 2.2f), outside - (inward * 3.6f) + (up * 0.7f), 50f, cast: "callout"));
            views.Add(View("burglarTill", Stop($"{CafeAndFlat.ShopAlarm}: café") + (up * 1.65f), Stop($"{CafeAndFlat.ShopAlarm}: till") + (up * 1.0f), 65f, indoor: true, open: new[] { "Shop door" }, cast: "till"));
            var centre = town.AlarmCentres.FirstOrDefault();
            if (centre?.GuardPost != null)
            {
                var post = Stop(centre.GuardPost);
                var towards = Vector3.Normalize(new Vector3(centre.Room.X - post.X, 0f, centre.Room.Z - post.Z));
                views.Add(View("guardPost", post + (towards * 3.2f) + (up * 1.6f), post + (up * 1.0f), 60f, indoor: true, cast: "post"));
            }
        }

        File.WriteAllText(path, "{" + string.Join(",", views) + "}\n");
    }

    private static Vector3 At(Vector2 groundPoint, float y) => new Vector3(groundPoint.X, y, groundPoint.Y);

    private static string View(string name, Vector3 eye, Vector3 lookAt, float fov, bool indoor = false, string[] open = null, string cast = null) =>
        $"\"{name}\":{{\"position\":{Vector(eye)},\"target\":{Vector(lookAt)},\"fov\":{F(fov)}{(indoor ? ",\"indoor\":true" : string.Empty)}{(open != null ? ",\"open\":[" + string.Join(",", open.Select(d => $"\"{d}\"")) + "]" : string.Empty)}{(cast != null ? $",\"cast\":\"{cast}\"" : string.Empty)}}}";

    private static string Vector(Vector3 v) => $"[{F(v.X)},{F(v.Y)},{F(v.Z)}]";

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
}
