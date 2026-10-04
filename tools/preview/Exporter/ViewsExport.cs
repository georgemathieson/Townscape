using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Townscape.Generation;
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

        File.WriteAllText(path, "{" + string.Join(",", views) + "}\n");
    }

    private static Vector3 At(Vector2 groundPoint, float y) => new Vector3(groundPoint.X, y, groundPoint.Y);

    private static string View(string name, Vector3 eye, Vector3 lookAt, float fov) =>
        $"\"{name}\":{{\"position\":{Vector(eye)},\"target\":{Vector(lookAt)},\"fov\":{F(fov)}}}";

    private static string Vector(Vector3 v) => $"[{F(v.X)},{F(v.Y)},{F(v.Z)}]";

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
}
