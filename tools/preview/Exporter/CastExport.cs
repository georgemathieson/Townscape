using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation;
using Townscape.Generation.Buildings.Interiors;
using Townscape.Generation.Geometry;
using Townscape.Generation.People;

/// <summary>
/// The people and the police car, posed for the previews in little scenes ("cast" in the
/// glTF's node extras) that page.html shows only in the views that ask for them: a call-out at
/// the Copper Kettle, the three of them in a row, the guard at their post and a burglar at the till.
/// </summary>
internal static class CastExport
{
    public static IEnumerable<(MeshData Mesh, string Scene)> Meshes(GeneratedTown town)
    {
        var walking = town.Walking;
        var driving = town.Driving;
        var guard = PeopleModels.Figure(FigureKind.Guard);
        var police = PeopleModels.Figure(FigureKind.Police);
        var burglar = PeopleModels.Figure(FigureKind.Burglar);
        var car = PeopleModels.PoliceCar();
        Vector3 Stop(string name) => walking.Point(walking.Find(name));
        IEnumerable<(MeshData, string)> In(string scene, IEnumerable<MeshData> meshes) => meshes.Select(m => (m, scene));

        var outside = Stop($"{CafeAndFlat.ShopAlarm}: outside");
        var towardsDoor = Inward(walking, $"{CafeAndFlat.ShopAlarm}: outside");
        var along = new Vector3(towardsDoor.Z, 0f, -towardsDoor.X);

        // The call-out: the car stopped in the road outside, the officers on their way to the
        // door, the guard waiting on the pavement, and the burglar making off with the swag.
        var parking = driving.Nearest(outside);
        var parked = driving.Point(parking);
        var heading = Level(driving.Point(driving.Links(parking).First().To) - parked);
        var kerb = parked + (new Vector3(-heading.Z, 0f, heading.X) * 1.3f);
        foreach (var mesh in In("callout", PeopleModels.Posed(car, parked, heading)))
        {
            yield return mesh;
        }

        foreach (var mesh in In("callout", PeopleModels.Posed(police, kerb + (heading * 0.4f), Level(outside - kerb), 0.35f, false))
            .Concat(In("callout", PeopleModels.Posed(police, kerb - (heading * 1.1f), Level(outside - kerb), -0.3f, false)))
            .Concat(In("callout", PeopleModels.Posed(guard, outside - (along * 1.4f), towardsDoor, 0f, false)))
            .Concat(In("callout", PeopleModels.Posed(burglar, outside + (along * 9f) - (towardsDoor * 0.4f), along, 0.55f, true))))
        {
            yield return mesh;
        }

        // The three of them in a row on the pavement, facing the road.
        var row = outside - (along * 6f);
        var y = row.Y;
        foreach (var (model, step, carrying) in new[] { (guard, -1.1f, false), (police, 0f, false), (burglar, 1.1f, true) })
        {
            var feet = row + (along * step);
            foreach (var mesh in In("lineup", PeopleModels.Posed(model, new Vector3(feet.X, y, feet.Z), -towardsDoor, 0f, carrying)))
            {
                yield return mesh;
            }
        }

        // The guard at their post in the alarm receiving centre.
        var centre = town.AlarmCentres.FirstOrDefault();
        if (centre?.GuardPost != null)
        {
            var post = Stop(centre.GuardPost);
            foreach (var mesh in In("post", PeopleModels.Posed(guard, post, Level(centre.Room - post), 0f, false)))
            {
                yield return mesh;
            }
        }

        // A burglar at the till.
        var till = Stop($"{CafeAndFlat.ShopAlarm}: till");
        foreach (var mesh in In("till", PeopleModels.Posed(burglar, till, -towardsDoor, 0.15f, true)))
        {
            yield return mesh;
        }
    }

    private static Vector3 Level(Vector3 v) => Vector3.Normalize(new Vector3(v.X, 0f, v.Z));

    /// <summary>Straight in through the door from a site's outside stop: towards the stop indoors it leads to.</summary>
    public static Vector3 Inward(Townscape.Generation.Routes.RouteNetwork walking, string outside)
    {
        var stop = walking.Find(outside);
        var inside = walking.Links(stop).First(l => walking.Indoors(l.To)).To;
        return Level(walking.Point(inside) - walking.Point(stop));
    }
}
