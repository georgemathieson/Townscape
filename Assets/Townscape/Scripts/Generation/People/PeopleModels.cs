using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.People
{
    public enum FigureKind
    {
        /// <summary>The alarm receiving centre's guard: navy trousers, a hi-vis jacket and a cap.</summary>
        Guard,

        /// <summary>A police constable: black, a hi-vis jacket and a custodian helmet.</summary>
        Police,

        /// <summary>A burglar from a picture book: striped jumper, mask, beanie, and a sack for the swag.</summary>
        Burglar,
    }

    /// <summary>
    /// A low-poly person in the parts that move as they walk. The body is in the figure's own
    /// frame (feet on the origin, facing +z, right along +x); each leg hangs from its hip and
    /// each arm from its shoulder, with its vertices relative to that pivot.
    /// </summary>
    public sealed class FigureModel
    {
        public FigureModel(MeshData body, MeshData leftLeg, MeshData rightLeg, MeshData leftArm, MeshData rightArm, MeshData bag)
        {
            Body = body;
            LeftLeg = leftLeg;
            RightLeg = rightLeg;
            LeftArm = leftArm;
            RightArm = rightArm;
            Bag = bag;
        }

        public MeshData Body { get; }

        public MeshData LeftLeg { get; }

        public MeshData RightLeg { get; }

        public MeshData LeftArm { get; }

        public MeshData RightArm { get; }

        /// <summary>A burglar's sack over their shoulder, in the body's frame, shown once they've taken something (null for anyone else).</summary>
        public MeshData Bag { get; }

        public static Vector3 LeftHip => new Vector3(-PeopleModels.HipApart, PeopleModels.HipHeight, 0f);

        public static Vector3 RightHip => new Vector3(PeopleModels.HipApart, PeopleModels.HipHeight, 0f);

        public static Vector3 LeftShoulder => new Vector3(-PeopleModels.ShoulderApart, PeopleModels.ShoulderHeight, 0f);

        public static Vector3 RightShoulder => new Vector3(PeopleModels.ShoulderApart, PeopleModels.ShoulderHeight, 0f);
    }

    /// <summary>
    /// The police car, in the car's own frame (the middle of it on the ground at the origin,
    /// facing +z): its body, one wheel (relative to its middle, its axle along x) and where each
    /// wheel goes, and the two blue lenses of the light bar on the roof.
    /// </summary>
    public sealed class CarModel
    {
        public CarModel(MeshData body, MeshData wheel, IReadOnlyList<Vector3> wheels, IReadOnlyList<(Vector3 Centre, Vector3 HalfSize)> blueLights, IReadOnlyList<Vector3> headlights)
        {
            Body = body;
            Wheel = wheel;
            Wheels = wheels;
            BlueLights = blueLights;
            Headlights = headlights;
        }

        public MeshData Body { get; }

        public MeshData Wheel { get; }

        public IReadOnlyList<Vector3> Wheels { get; }

        /// <summary>The left and right lenses of the light bar: their middles and half sizes.</summary>
        public IReadOnlyList<(Vector3 Centre, Vector3 HalfSize)> BlueLights { get; }

        /// <summary>The front of each headlight.</summary>
        public IReadOnlyList<Vector3> Headlights { get; }
    }

    /// <summary>Builds the people and the police car out of boxes, prisms and blobs.</summary>
    public static class PeopleModels
    {
        public const float HipHeight = 0.9f;
        public const float HipApart = 0.09f;
        public const float ShoulderHeight = 1.43f;
        public const float ShoulderApart = 0.255f;

        public const float CarLength = 4.4f;
        public const float CarWidth = 1.78f;
        public const float WheelRadius = 0.32f;

        private static readonly Vector3 X = Vector3.UnitX;
        private static readonly Vector3 Y = Vector3.UnitY;
        private static readonly Vector3 Z = Vector3.UnitZ;

        public static FigureModel Figure(FigureKind kind)
        {
            var (jacket, trousers, hands) = kind switch
            {
                FigureKind.Guard => (SurfaceMaterial.HiVis, SurfaceMaterial.PaintNavy, SurfaceMaterial.Skin),
                FigureKind.Police => (SurfaceMaterial.HiVis, SurfaceMaterial.ClothDark, SurfaceMaterial.Skin),
                _ => (SurfaceMaterial.PaintBlack, SurfaceMaterial.ClothDark, SurfaceMaterial.PaintBlack),
            };

            // The body: hips, chest, neck, head and hat.
            var body = new MeshBuilder();
            Box(body, 0f, 0.94f, 0f, 0.17f, 0.07f, 0.11f, trousers);
            if (kind == FigureKind.Burglar)
            {
                // Stripes all the way up.
                for (var i = 0; i < 5; i++)
                {
                    var y0 = 0.97f + (i * 0.1f);
                    Box(body, 0f, y0 + 0.05f, 0f, 0.19f, 0.05f, 0.12f, i % 2 == 0 ? SurfaceMaterial.PaintWhite : SurfaceMaterial.PaintBlack);
                }
            }
            else
            {
                Box(body, 0f, 1.21f, 0f, 0.19f, 0.25f, 0.12f, jacket);

                // Two silver bands round the jacket, as hi-vis jackets have.
                foreach (var y in new[] { 1.06f, 1.16f })
                {
                    Box(body, 0f, y, 0f, 0.195f, 0.014f, 0.125f, SurfaceMaterial.Chrome);
                }

                Box(body, 0f, 0.99f, 0f, 0.18f, 0.025f, 0.115f, SurfaceMaterial.PaintBlack);
                Box(body, 0.08f, 1.36f, 0.122f, 0.035f, 0.05f, 0.012f, SurfaceMaterial.PaintBlack);
            }

            Box(body, 0f, 1.5f, 0f, 0.05f, 0.04f, 0.05f, SurfaceMaterial.Skin);
            body.AddBlob(new Vector3(0f, 1.635f, 0.01f), new Vector3(0.1f, 0.115f, 0.105f), SurfaceMaterial.Skin);
            if (kind != FigureKind.Burglar)
            {
                // Eyes, so you can tell which way they're looking (a burglar's are behind the mask).
                foreach (var side in new[] { -1f, 1f })
                {
                    Box(body, side * 0.038f, 1.655f, 0.103f, 0.014f, 0.012f, 0.008f, SurfaceMaterial.PaintBlack);
                }
            }

            switch (kind)
            {
                case FigureKind.Guard:
                    body.AddPrism(new Vector3(0f, 1.69f, 0f), 0.112f, 0.07f, 8, SurfaceMaterial.PaintNavy, capBottom: true);
                    Box(body, 0f, 1.7f, 0.135f, 0.08f, 0.008f, 0.05f, SurfaceMaterial.PaintBlack);
                    break;
                case FigureKind.Police:
                    // The custodian helmet: tall, rounded on top, with its silver star at the front.
                    body.AddPrism(new Vector3(0f, 1.68f, 0f), 0.118f, 0.13f, 8, SurfaceMaterial.PaintBlack, capBottom: true);
                    body.AddCone(new Vector3(0f, 1.81f, 0f), 0.118f, 0.08f, 8, SurfaceMaterial.PaintBlack);
                    body.AddPrism(new Vector3(0f, 1.885f, 0f), 0.02f, 0.03f, 6, SurfaceMaterial.Chrome);
                    Box(body, 0f, 1.76f, 0.115f, 0.03f, 0.035f, 0.01f, SurfaceMaterial.Chrome);
                    Box(body, 0f, 1.69f, 0.11f, 0.09f, 0.008f, 0.03f, SurfaceMaterial.PaintBlack);
                    break;
                default:
                    body.AddBlob(new Vector3(0f, 1.7f, 0f), new Vector3(0.108f, 0.075f, 0.11f), SurfaceMaterial.ClothDark);
                    Box(body, 0f, 1.65f, 0.1f, 0.1f, 0.025f, 0.015f, SurfaceMaterial.PaintBlack);
                    break;
            }

            // Each leg from its hip, with a shoe.
            MeshData Leg(string name)
            {
                var leg = new MeshBuilder();
                Box(leg, 0f, -0.425f, 0f, 0.072f, 0.425f, 0.085f, trousers);
                Box(leg, 0f, -0.86f, 0.04f, 0.065f, 0.04f, 0.13f, SurfaceMaterial.PaintBlack);
                return leg.Build(name);
            }

            // Each arm from its shoulder, with a hand (gloved, for a burglar).
            MeshData Arm(string name)
            {
                var arm = new MeshBuilder();
                if (kind == FigureKind.Burglar)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        Box(arm, 0f, -0.056f - (i * 0.112f), 0f, 0.055f, 0.056f, 0.06f, i % 2 == 0 ? SurfaceMaterial.PaintBlack : SurfaceMaterial.PaintWhite);
                    }
                }
                else
                {
                    Box(arm, 0f, -0.28f, 0f, 0.055f, 0.28f, 0.06f, jacket);
                }

                Box(arm, 0f, -0.62f, 0f, 0.045f, 0.06f, 0.05f, hands);
                return arm.Build(name);
            }

            MeshData bag = null;
            if (kind == FigureKind.Burglar)
            {
                var sack = new MeshBuilder();
                sack.AddBlob(new Vector3(0.2f, 1.42f, -0.26f), new Vector3(0.2f, 0.25f, 0.17f), SurfaceMaterial.Linen);
                sack.AddPrism(new Vector3(0.22f, 1.62f, -0.2f), 0.05f, 0.08f, 6, SurfaceMaterial.Linen);
                bag = sack.Build("Swag bag");
            }

            return new FigureModel(body.Build($"{kind} body"), Leg("Left leg"), Leg("Right leg"), Arm("Left arm"), Arm("Right arm"), bag);
        }

        public static CarModel PoliceCar()
        {
            var b = new MeshBuilder();
            const float halfWidth = CarWidth * 0.5f;
            const float halfLength = CarLength * 0.5f;
            const float sill = 0.31f;
            const float waist = 0.86f;

            // The body: white, with black bumpers, and the cabin's glass under a white roof.
            Box(b, 0f, (sill + waist) * 0.5f, 0f, halfWidth, (waist - sill) * 0.5f, halfLength, SurfaceMaterial.PaintWhite);
            foreach (var end in new[] { -1f, 1f })
            {
                Box(b, 0f, 0.42f, end * (halfLength + 0.04f), halfWidth - 0.02f, 0.1f, 0.06f, SurfaceMaterial.PaintBlack);
                Box(b, 0f, 0.55f, end * (halfLength + 0.105f), 0.26f, 0.055f, 0.005f, end > 0f ? SurfaceMaterial.PaintWhite : SurfaceMaterial.PaintButter);
            }

            const float cabinZ = -0.25f;
            const float cabinHalf = 1.1f;
            const float roof = 1.36f;
            Box(b, 0f, (waist + roof) * 0.5f, cabinZ, halfWidth - 0.1f, (roof - waist) * 0.5f, cabinHalf, SurfaceMaterial.WindowGlass);
            Box(b, 0f, roof + 0.02f, cabinZ, halfWidth - 0.08f, 0.025f, cabinHalf + 0.02f, SurfaceMaterial.PaintWhite);
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var z in new[] { cabinZ - cabinHalf, cabinZ - 0.05f, cabinZ + cabinHalf })
                {
                    Box(b, side * (halfWidth - 0.1f), (waist + roof) * 0.5f, z, 0.012f, (roof - waist) * 0.5f, 0.05f, SurfaceMaterial.PaintWhite);
                }
            }

            // Battenburg down each side: two rows of blue and yellow squares, and POLICE above.
            const float square = 0.26f;
            for (var i = 0; i < 16; i++)
            {
                var z = -halfLength + 0.12f + ((i + 0.5f) * square);
                for (var row = 0; row < 2; row++)
                {
                    var material = (i + row) % 2 == 0 ? SurfaceMaterial.PaintBlue : SurfaceMaterial.HiVis;
                    foreach (var side in new[] { -1f, 1f })
                    {
                        Box(b, side * (halfWidth + 0.004f), 0.5f + (row * square * 0.5f), z, 0.006f, square * 0.25f, square * 0.5f, material);
                    }
                }
            }

            foreach (var side in new[] { -1f, 1f })
            {
                var a = new Vector2(side * (halfWidth + 0.012f), side * 1.2f);
                var c = new Vector2(side * (halfWidth + 0.012f), -side * 1.2f);
                var wall = WallFrame.FromBase(a, c);
                if (Vector3.Dot(wall.Out, X * side) < 0f)
                {
                    wall = WallFrame.FromBase(c, a);
                }

                PixelFont.Write(b, wall, "POLICE", wall.Width * 0.5f, 0.765f, 0.03f, 1.6f, 0.002f, SurfaceMaterial.PaintBlue);
            }

            // Lamps: headlights, tail lights, and the light bar with its two blue lenses.
            var headlights = new List<Vector3>();
            foreach (var side in new[] { -1f, 1f })
            {
                Box(b, side * 0.62f, 0.7f, halfLength + 0.005f, 0.17f, 0.055f, 0.008f, SurfaceMaterial.LampGlass);
                Box(b, side * 0.66f, 0.71f, -halfLength - 0.005f, 0.14f, 0.05f, 0.008f, SurfaceMaterial.BulbRed);
                headlights.Add(new Vector3(side * 0.62f, 0.7f, halfLength + 0.015f));
            }

            const float barZ = cabinZ + 0.15f;
            Box(b, 0f, roof + 0.07f, barZ, 0.62f, 0.025f, 0.13f, SurfaceMaterial.PaintBlack);
            var lights = new List<(Vector3, Vector3)>();
            foreach (var side in new[] { -1f, 1f })
            {
                var centre = new Vector3(side * 0.31f, roof + 0.135f, barZ);
                var half = new Vector3(0.27f, 0.04f, 0.11f);
                Box(b, centre.X, centre.Y, centre.Z, half.X, half.Y, half.Z, SurfaceMaterial.AlarmStrobe);
                lights.Add((centre, half));
            }

            // One wheel, turning on its axle along x, with a silver hub on each face.
            var w = new MeshBuilder();
            const int sides = 12;
            const float halfTread = 0.11f;
            for (var i = 0; i < sides; i++)
            {
                var a0 = MathF.PI * 2f * i / sides;
                var a1 = MathF.PI * 2f * (i + 1) / sides;
                var d0 = new Vector3(0f, MathF.Cos(a0), MathF.Sin(a0)) * WheelRadius;
                var d1 = new Vector3(0f, MathF.Cos(a1), MathF.Sin(a1)) * WheelRadius;
                var left = -X * halfTread;
                var right = X * halfTread;
                w.AddQuadFacing(left + d0, left + d1, right + d1, right + d0, (d0 + d1) * 0.5f, SurfaceMaterial.Tyre);
                foreach (var face in new[] { left, right })
                {
                    w.AddTriangleFacing(face, face + (d0 * 0.6f), face + (d1 * 0.6f), face, SurfaceMaterial.Chrome);
                    w.AddQuadFacing(face + (d0 * 0.6f), face + d0, face + d1, face + (d1 * 0.6f), face, SurfaceMaterial.Tyre);
                }
            }

            var wheels = new List<Vector3>();
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1.4f, 1.4f })
                {
                    wheels.Add(new Vector3(side * (halfWidth - 0.1f), WheelRadius, z));
                }
            }

            return new CarModel(b.Build("Police car"), w.Build("Wheel"), wheels, lights, headlights);
        }

        // ---- Posing them, for the previews ------------------------------------------------------

        /// <summary>
        /// The whole figure stood at <paramref name="feet"/> facing <paramref name="facing"/>, mid
        /// stride by <paramref name="stride"/> radians (positive with the left foot forward and the
        /// left arm back), in town coordinates.
        /// </summary>
        public static IEnumerable<MeshData> Posed(FigureModel model, Vector3 feet, Vector3 facing, float stride, bool carrying)
        {
            var frame = Frame(feet, facing);
            yield return Place(model.Body, frame, Vector3.Zero, 0f);
            yield return Place(model.LeftLeg, frame, FigureModel.LeftHip, -stride);
            yield return Place(model.RightLeg, frame, FigureModel.RightHip, stride);
            yield return Place(model.LeftArm, frame, FigureModel.LeftShoulder, stride * 0.8f);
            yield return Place(model.RightArm, frame, FigureModel.RightShoulder, -stride * 0.8f);
            if (carrying && model.Bag != null)
            {
                yield return Place(model.Bag, frame, Vector3.Zero, 0f);
            }
        }

        /// <summary>The car parked at <paramref name="position"/> facing <paramref name="facing"/>, in town coordinates.</summary>
        public static IEnumerable<MeshData> Posed(CarModel model, Vector3 position, Vector3 facing)
        {
            var frame = Frame(position, facing);
            yield return Place(model.Body, frame, Vector3.Zero, 0f);
            foreach (var wheel in model.Wheels)
            {
                yield return Place(model.Wheel, frame, wheel, 0f);
            }
        }

        private static (Vector3 Origin, Vector3 Right, Vector3 Forward) Frame(Vector3 origin, Vector3 facing)
        {
            var forward = Vector3.Normalize(new Vector3(facing.X, 0f, facing.Z));
            return (origin, new Vector3(forward.Z, 0f, -forward.X), forward);
        }

        // A part about its pivot, turned about its x axis, then into the frame.
        private static MeshData Place(MeshData part, (Vector3 Origin, Vector3 Right, Vector3 Forward) frame, Vector3 pivot, float swing)
        {
            var (cos, sin) = (MathF.Cos(swing), MathF.Sin(swing));
            Vector3 Turn(Vector3 v) => new Vector3(v.X, (v.Y * cos) - (v.Z * sin), (v.Y * sin) + (v.Z * cos));
            Vector3 Out(Vector3 v) => (frame.Right * v.X) + (Y * v.Y) + (frame.Forward * v.Z);
            var positions = part.Positions.Select(p => frame.Origin + Out(pivot + Turn(p))).ToArray();
            var normals = part.Normals.Select(n => Out(Turn(n))).ToArray();
            return new MeshData(part.Name, positions, normals, part.Submeshes) { Uvs = part.Uvs };
        }

        private static void Box(MeshBuilder builder, float x, float y, float z, float hx, float hy, float hz, SurfaceMaterial material) =>
            builder.AddBox(new Vector3(x, y, z), X, Y, Z, new Vector3(hx, hy, hz), material, includeBottom: true);
    }
}
