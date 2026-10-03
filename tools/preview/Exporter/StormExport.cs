using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Simulation;
using Townscape.Simulation.Weather;

/// <summary>
/// Writes storm.json: how the storm looks in the preview, worked out by the same simulation code
/// Unity uses. Wet materials, the wind and rain, ripple normal maps, which chimneys smoke at each
/// preset's hour, and a few lightning bolts. Positions are converted to glTF (z negated).
/// </summary>
internal static class StormExport
{
    private static readonly (string Name, float Hour)[] Presets = { ("day", 12.5f), ("dusk", 19.25f), ("night", 21f) };

    // Default settings from the store (rain 0.75, lightning 0.5, wind 0.45), sampled at a fixed moment.
    private static readonly WeatherSettings Settings = new WeatherSettings(0.75f, 0.5f, 0.45f);

    public static void Write(GeneratedTown town, string path)
    {
        string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
        string Point(Vector3 p) => $"[{F(p.X)},{F(p.Y)},{F(-p.Z)}]";
        float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

        var conditions = new ThunderstormProfile().Sample(Settings, 10f);
        var json = new StringBuilder("{");

        json.Append("\"wet\":{");
        json.Append(string.Join(",", Wetness.Affected.Select(material =>
        {
            var wet = Wetness.Apply(material, SurfacePalette.Get(material), 1f);
            return $"\"{material}\":[{F(ToLinear(wet.R))},{F(ToLinear(wet.G))},{F(ToLinear(wet.B))},{F(1f - wet.Smoothness)}]";
        })));
        json.Append("},");

        var velocity = RainFall.Velocity(conditions.Wind);
        json.Append($"\"rain\":{{\"amount\":{F(conditions.Rain)},\"velocity\":{Point(velocity)},\"mist\":{F(conditions.Mist)}}},");

        json.Append($"\"ripples\":{{\"size\":128,\"puddle\":\"{Convert.ToBase64String(new RippleField(128, 24, 70, 0f, 31).NormalMap(5, 2.5f))}\",");
        json.Append($"\"water\":\"{Convert.ToBase64String(new RippleField(128, 24, 45, 1f, 37).NormalMap(5, 2f))}\"}},");

        var chimneys = town.Anchors.Where(a => a.Kind == AnchorKind.Chimney).ToList();
        json.Append("\"smoke\":{");
        json.Append(string.Join(",", Presets.Select(preset =>
            $"\"{preset.Name}\":[{string.Join(",", chimneys.Where(c => ChimneySmoke.IsBurning(c.Seed, preset.Hour)).Select(c => Point(c.Position)))}]")));
        json.Append($"}},\"wind\":{Point(new Vector3(conditions.Wind.X, 0f, conditions.Wind.Y))},");

        // Bolts in several directions, so most views have one in sight on the flash preset.
        var bolts = new List<string>();
        var spots = new[] { new Vector2(-150f, 520f), new Vector2(380f, 260f), new Vector2(-420f, -300f), new Vector2(160f, -560f), new Vector2(-520f, 120f) };
        for (var i = 0; i < spots.Length; i++)
        {
            var ground = new Vector3(spots[i].X, town.Context.Terrain.FarHeightAt(spots[i]), spots[i].Y);
            var cloud = new Vector3(ground.X + 60f, 450f, ground.Z - 40f);
            var segments = LightningBolt.Generate(100 + i, cloud, ground);
            bolts.Add($"[{string.Join(",", segments.Select(s => $"[{Point(s.Start)},{Point(s.End)},{F(s.Width)}]"))}]");
        }

        json.Append($"\"bolts\":[{string.Join(",", bolts)}]");
        json.Append("}");
        File.WriteAllText(path, json.ToString());
    }
}
