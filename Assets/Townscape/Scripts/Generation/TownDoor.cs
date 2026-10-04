using System;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation
{
    /// <summary>
    /// A door that opens: its leaf is built as its own mesh instead of into the building, so it
    /// can swing on its hinges. The leaf's vertices are relative to <see cref="Hinge"/>, the
    /// bottom of the hinged edge.
    /// </summary>
    public sealed class TownDoor
    {
        public TownDoor(string name, Vector3 hinge, Vector3 along, Vector3 inward, float width, float height, MeshData leaf, float openDegrees = 100f)
        {
            Name = name;
            Hinge = hinge;
            Along = Vector3.Normalize(along);
            Inward = Vector3.Normalize(inward);
            Width = width;
            Height = height;
            Leaf = leaf;
            OpenDegrees = openDegrees;
        }

        public string Name { get; }

        /// <summary>The bottom of the hinged edge, in town coordinates.</summary>
        public Vector3 Hinge { get; }

        /// <summary>Level, from the hinge to the latch edge when the door is shut.</summary>
        public Vector3 Along { get; }

        /// <summary>Level, the side the door swings open towards (into the room).</summary>
        public Vector3 Inward { get; }

        public float Width { get; }

        public float Height { get; }

        /// <summary>How far the door swings open, in degrees.</summary>
        public float OpenDegrees { get; }

        /// <summary>The leaf, with its vertices relative to <see cref="Hinge"/>.</summary>
        public MeshData Leaf { get; }

        /// <summary>The bottom of the latch edge when the door is open by <paramref name="degrees"/>.</summary>
        public Vector3 LatchAt(float degrees)
        {
            var angle = degrees * MathF.PI / 180f;
            return Hinge + (((Along * MathF.Cos(angle)) + (Inward * MathF.Sin(angle))) * Width);
        }

        /// <summary>The leaf where it hangs shut, in town coordinates (for previews and tests).</summary>
        public MeshData ClosedLeaf() =>
            new MeshData(Name, Leaf.Positions.Select(p => p + Hinge).ToArray(), Leaf.Normals, Leaf.Submeshes) { Uvs = Leaf.Uvs };
    }
}
