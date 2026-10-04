using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Townscape.Generation;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Buildings.Styles;
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
            views.Add(View("kettleBack", space.At(space.Width * 0.5f, f[3] + 4f, space.Depth + 9f), space.At(space.Width * 0.5f, f[3] + 1.2f, space.Depth * 0.7f), 50f));
        }

        File.WriteAllText(path, "{" + string.Join(",", views) + "}\n");
    }

    private static Vector3 At(Vector2 groundPoint, float y) => new Vector3(groundPoint.X, y, groundPoint.Y);

    private static string View(string name, Vector3 eye, Vector3 lookAt, float fov, bool indoor = false) =>
        $"\"{name}\":{{\"position\":{Vector(eye)},\"target\":{Vector(lookAt)},\"fov\":{F(fov)}{(indoor ? ",\"indoor\":true" : string.Empty)}}}";

    private static string Vector(Vector3 v) => $"[{F(v.X)},{F(v.Y)},{F(v.Z)}]";

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
}
