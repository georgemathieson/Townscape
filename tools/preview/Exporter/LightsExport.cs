using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Townscape.Generation;
using Townscape.Simulation;

/// <summary>
/// Writes lights.json: for each preview preset, the emission of every glowing material and the
/// point lights at anchors, worked out by the same NightLights code Unity uses.
/// </summary>
internal static class LightsExport
{
    private static readonly (string Name, float Hour, float Darkness)[] Presets =
    {
        ("day", 12.5f, 0.08f),
        ("dusk", 19.25f, 0.55f),
        ("night", 21f, 1f),
    };

    public static void Write(GeneratedTown town, string path)
    {
        string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        var json = new StringBuilder("{");
        for (var p = 0; p < Presets.Length; p++)
        {
            var (name, hour, darkness) = Presets[p];
            const float time = 0.2f;
            json.Append(p == 0 ? string.Empty : ",").Append($"\"{name}\":{{\"emission\":{{");

            var first = true;
            foreach (var material in NightLights.EmissiveMaterials)
            {
                var emission = NightLights.Emission(material, hour, darkness, time);
                json.Append(first ? string.Empty : ",").Append($"\"{material}\":[{F(emission.R)},{F(emission.G)},{F(emission.B)}]");
                first = false;
            }

            json.Append("},\"lights\":[");
            first = true;
            foreach (var anchor in town.Anchors)
            {
                if (!NightLights.TryGetLight(anchor.Kind, out var spec))
                {
                    continue;
                }

                var level = NightLights.LightLevel(anchor.Kind, anchor.Seed, hour, darkness, time);
                if (level < 0.01f)
                {
                    continue;
                }

                var position = anchor.Position + (anchor.Facing * spec.Outward);
                json.Append(first ? string.Empty : ",")
                    .Append($"{{\"p\":[{F(position.X)},{F(position.Y)},{F(-position.Z)}],\"c\":[{F(spec.Colour.R)},{F(spec.Colour.G)},{F(spec.Colour.B)}],\"r\":{F(spec.Range)},\"i\":{F(spec.Intensity * level)}}}");
                first = false;
            }

            json.Append("]}");
        }

        json.Append("}");
        File.WriteAllText(path, json.ToString());
    }
}
