using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;

// Writes the generated village as town.gltf + town.bin. Unity is left-handed (z north) and
// glTF is right-handed, so z is negated and every triangle's winding is reversed.
var output = args.Length > 0 ? args[0] : "out";
Directory.CreateDirectory(output);

var stopwatch = Stopwatch.StartNew();
var town = new TownGenerator().Generate(new LakeDistrictVillageLayout().Create());
Console.WriteLine($"Generated {town.Meshes.Count} meshes in {stopwatch.ElapsedMilliseconds} ms");
Console.WriteLine($"Ground: {town.GroundStats.TopTriangles} tops, {town.GroundStats.StepFaces} steps, {town.GroundStats.SnappedVertices} snapped, {town.GroundStats.FlippedTriangles} flipped, {town.GroundStats.HoleTriangles} holes");

var binary = new MemoryStream();
var bufferViews = new List<string>();
var accessors = new List<string>();
var meshes = new List<string>();
var nodes = new List<string>();
var materialIndex = new Dictionary<SurfaceMaterial, int>();
var materials = new List<string>();

int AddView(byte[] bytes, int? target)
{
    while (binary.Length % 4 != 0)
    {
        binary.WriteByte(0);
    }

    var offset = binary.Length;
    binary.Write(bytes, 0, bytes.Length);
    var targetJson = target.HasValue ? $",\"target\":{target.Value}" : string.Empty;
    bufferViews.Add($"{{\"buffer\":0,\"byteOffset\":{offset},\"byteLength\":{bytes.Length}{targetJson}}}");
    return bufferViews.Count - 1;
}

string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

int MaterialFor(SurfaceMaterial material)
{
    if (materialIndex.TryGetValue(material, out var index))
    {
        return index;
    }

    var appearance = SurfacePalette.Get(material);
    var blend = appearance.IsTransparent ? ",\"alphaMode\":\"BLEND\"" : string.Empty;
    materials.Add(
        $"{{\"name\":\"{material}\",\"pbrMetallicRoughness\":{{\"baseColorFactor\":[{F(ToLinear(appearance.R))},{F(ToLinear(appearance.G))},{F(ToLinear(appearance.B))},{F(appearance.Alpha)}],\"metallicFactor\":0,\"roughnessFactor\":{F(1f - appearance.Smoothness)}}}{blend}}}");
    materialIndex[material] = materials.Count - 1;
    return materials.Count - 1;
}

foreach (var generated in town.Meshes)
{
    var mesh = generated.Mesh;
    var positions = new float[mesh.VertexCount * 3];
    var normals = new float[mesh.VertexCount * 3];
    var min = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
    var max = new[] { float.MinValue, float.MinValue, float.MinValue };
    for (var i = 0; i < mesh.VertexCount; i++)
    {
        var p = mesh.Positions[i];
        var n = mesh.Normals[i];
        var converted = new[] { p.X, p.Y, -p.Z };
        for (var k = 0; k < 3; k++)
        {
            positions[(i * 3) + k] = converted[k];
            min[k] = MathF.Min(min[k], converted[k]);
            max[k] = MathF.Max(max[k], converted[k]);
        }

        normals[(i * 3) + 0] = n.X;
        normals[(i * 3) + 1] = n.Y;
        normals[(i * 3) + 2] = -n.Z;
    }

    var positionBytes = new byte[positions.Length * 4];
    Buffer.BlockCopy(positions, 0, positionBytes, 0, positionBytes.Length);
    var normalBytes = new byte[normals.Length * 4];
    Buffer.BlockCopy(normals, 0, normalBytes, 0, normalBytes.Length);

    accessors.Add($"{{\"bufferView\":{AddView(positionBytes, 34962)},\"componentType\":5126,\"count\":{mesh.VertexCount},\"type\":\"VEC3\",\"min\":[{F(min[0])},{F(min[1])},{F(min[2])}],\"max\":[{F(max[0])},{F(max[1])},{F(max[2])}]}}");
    var positionAccessor = accessors.Count - 1;
    accessors.Add($"{{\"bufferView\":{AddView(normalBytes, 34962)},\"componentType\":5126,\"count\":{mesh.VertexCount},\"type\":\"VEC3\"}}");
    var normalAccessor = accessors.Count - 1;

    var primitives = new List<string>();
    foreach (var submesh in mesh.Submeshes)
    {
        var indices = new uint[submesh.Indices.Length];
        for (var i = 0; i < indices.Length; i += 3)
        {
            indices[i] = (uint)submesh.Indices[i];
            indices[i + 1] = (uint)submesh.Indices[i + 2];
            indices[i + 2] = (uint)submesh.Indices[i + 1];
        }

        var indexBytes = new byte[indices.Length * 4];
        Buffer.BlockCopy(indices, 0, indexBytes, 0, indexBytes.Length);
        accessors.Add($"{{\"bufferView\":{AddView(indexBytes, 34963)},\"componentType\":5125,\"count\":{indices.Length},\"type\":\"SCALAR\"}}");
        primitives.Add($"{{\"attributes\":{{\"POSITION\":{positionAccessor},\"NORMAL\":{normalAccessor}}},\"indices\":{accessors.Count - 1},\"material\":{MaterialFor(submesh.Material)}}}");
    }

    meshes.Add($"{{\"name\":\"{mesh.Name}\",\"primitives\":[{string.Join(",", primitives)}]}}");
    nodes.Add($"{{\"name\":\"{mesh.Name}\",\"mesh\":{meshes.Count - 1},\"extras\":{{\"category\":\"{generated.Category}\"}}}}");
}

var nodeIndices = new List<string>();
for (var i = 0; i < nodes.Count; i++)
{
    nodeIndices.Add(i.ToString(CultureInfo.InvariantCulture));
}

File.WriteAllBytes(Path.Combine(output, "town.bin"), binary.ToArray());
var json = new StringBuilder();
json.Append("{\"asset\":{\"version\":\"2.0\",\"generator\":\"Townscape preview exporter\"},");
json.Append($"\"buffers\":[{{\"uri\":\"town.bin\",\"byteLength\":{binary.Length}}}],");
json.Append($"\"bufferViews\":[{string.Join(",", bufferViews)}],");
json.Append($"\"accessors\":[{string.Join(",", accessors)}],");
json.Append($"\"materials\":[{string.Join(",", materials)}],");
json.Append($"\"meshes\":[{string.Join(",", meshes)}],");
json.Append($"\"nodes\":[{string.Join(",", nodes)}],");
json.Append($"\"scenes\":[{{\"nodes\":[{string.Join(",", nodeIndices)}]}}],\"scene\":0}}");
File.WriteAllText(Path.Combine(output, "town.gltf"), json.ToString());
LightsExport.Write(town, Path.Combine(output, "lights.json"));
Console.WriteLine($"Wrote {Path.Combine(output, "town.gltf")} ({binary.Length / 1024} KiB of geometry)");
