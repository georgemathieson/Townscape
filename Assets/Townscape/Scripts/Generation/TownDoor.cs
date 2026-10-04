using System;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation
{
    /// <summary>
    /// A door (or a window) that opens: its leaf is built as its own mesh instead of into the
    /// building, so it can swing on its hinges. The leaf's vertices are relative to
    /// <see cref="Hinge"/>, a point on the line it turns about: the bottom of a door's hinged
    /// edge, or the end of a roof window's pivot.
    /// </summary>
    public sealed class TownDoor
    {
        public TownDoor(string name, Vector3 hinge, Vector3 along, Vector3 inward, float width, float height, MeshData leaf, float openDegrees = 100f, Vector3? axis = null, string noun = "door")
        {
            Name = name;
            Hinge = hinge;
            Along = Vector3.Normalize(along);
            Inward = Vector3.Normalize(inward);
            Width = width;
            Height = height;
            Leaf = leaf;
            OpenDegrees = openDegrees;
            Axis = Vector3.Normalize(axis ?? Vector3.UnitY);
            Noun = noun;
        }

        public string Name { get; }

        /// <summary>A point on the line the leaf turns about, in town coordinates.</summary>
        public Vector3 Hinge { get; }

        /// <summary>The line the leaf turns about: straight up for a door, across the slope for a roof window.</summary>
        public Vector3 Axis { get; }

        /// <summary>Square to <see cref="Axis"/>, from the hinge to the latch edge when shut.</summary>
        public Vector3 Along { get; }

        /// <summary>Square to <see cref="Axis"/>, the way the latch edge moves as it opens (into the room).</summary>
        public Vector3 Inward { get; }

        /// <summary>From the hinge to the latch edge.</summary>
        public float Width { get; }

        /// <summary>Along the hinge.</summary>
        public float Height { get; }

        /// <summary>What it is, for the prompt: "door" or "window".</summary>
        public string Noun { get; }

        /// <summary>How far the door swings open, in degrees.</summary>
        public float OpenDegrees { get; }

        /// <summary>The leaf, with its vertices relative to <see cref="Hinge"/>.</summary>
        public MeshData Leaf { get; }

        /// <summary>The latch edge (level with the hinge) when the door is open by <paramref name="degrees"/>.</summary>
        public Vector3 LatchAt(float degrees)
        {
            var angle = degrees * MathF.PI / 180f;
            return Hinge + (((Along * MathF.Cos(angle)) + (Inward * MathF.Sin(angle))) * Width);
        }

        /// <summary>The leaf where it hangs shut, in town coordinates (for previews and tests).</summary>
        public MeshData ClosedLeaf() =>
            new MeshData(Name, Leaf.Positions.Select(p => p + Hinge).ToArray(), Leaf.Normals, Leaf.Submeshes) { Uvs = Leaf.Uvs };

        /// <summary>The leaf swung open by <paramref name="degrees"/>, in town coordinates (for previews).</summary>
        public MeshData LeafAt(float degrees)
        {
            var angle = degrees * MathF.PI / 180f;
            var (cos, sin) = (MathF.Cos(angle), MathF.Sin(angle));
            Vector3 Turn(Vector3 v)
            {
                var along = Vector3.Dot(v, Along);
                var inward = Vector3.Dot(v, Inward);
                var rest = v - (Along * along) - (Inward * inward);
                return rest + (Along * ((along * cos) - (inward * sin))) + (Inward * ((along * sin) + (inward * cos)));
            }

            return new MeshData(Name, Leaf.Positions.Select(p => Turn(p) + Hinge).ToArray(), Leaf.Normals.Select(Turn).ToArray(), Leaf.Submeshes) { Uvs = Leaf.Uvs };
        }
    }
}
