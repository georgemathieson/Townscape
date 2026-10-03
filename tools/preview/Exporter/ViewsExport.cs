using System.Globalization;
using System.IO;
using System.Numerics;
using Townscape.Generation;
using Townscape.Generation.Buildings.Planning;
using Townscape.Generation.Layout;

/// <summary>
/// Writes views.json: camera views worked out from the generated town rather than written into
/// page.html. Today that's "coffee", the view the Fellside Coffee game glides the camera to.
/// </summary>
internal static class ViewsExport
{
    // The game's camera (TownscapeBootstrap.CreateCamera).
    private const float UnityFieldOfView = 55f;

    public static void Write(GeneratedTown town, string path)
    {
        var shop = ShopLocator.Find(town.Context.Buildings, VillageShops.FellsideCoffee);
        if (shop == null)
        {
            File.WriteAllText(path, "{}\n");
            return;
        }

        var view = ShopLocator.ViewOf(shop.Footprint, town.Context.Ground.HeightAt);
        File.WriteAllText(path, $"{{\"coffee\":{{\"position\":{Vector(view.Eye)},\"target\":{Vector(view.LookAt)},\"fov\":{F(UnityFieldOfView)}}}}}\n");
    }

    private static string Vector(Vector3 v) => $"[{F(v.X)},{F(v.Y)},{F(v.Z)}]";

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
}
