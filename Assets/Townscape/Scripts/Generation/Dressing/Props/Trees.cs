using System;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Dressing.Props
{
    public enum TreeKind
    {
        /// <summary>Oak, ash or sycamore: a stout trunk under a cluster of rounded crowns.</summary>
        Broadleaf,

        /// <summary>Larch or spruce: stacked cones.</summary>
        Conifer,

        /// <summary>Silver birch: a slim white trunk and a light, narrow crown.</summary>
        Birch,

        /// <summary>Churchyard yew: low, broad and very dark.</summary>
        Yew,
    }

    /// <summary>A low poly tree. Size and shape vary with the random stream it is built with.</summary>
    public sealed class Tree : IProp
    {
        private static readonly SurfaceMaterial[] BroadleafColours =
        {
            SurfaceMaterial.LeafGreen, SurfaceMaterial.LeafGreen, SurfaceMaterial.LeafDark, SurfaceMaterial.LeafLight,
            SurfaceMaterial.LeafGreen, SurfaceMaterial.LeafDark, SurfaceMaterial.LeafLight, SurfaceMaterial.LeafAutumn,
        };

        // Wide enough for every kind's trunk, so a walker never brushes through the bark.
        private const float TrunkRadius = 0.3f;

        private readonly TreeKind _kind;
        private readonly float _scale;

        public Tree(TreeKind kind, float scale = 1f)
        {
            _kind = kind;
            _scale = scale;
        }

        public TreeKind Kind => _kind;

        public void Build(PropFrame f)
        {
            f.Anchors.Add(new TownAnchor(AnchorKind.TreeTrunk, f.Origin, System.Numerics.Vector3.UnitY, TrunkRadius * _scale, 0));
            switch (_kind)
            {
                case TreeKind.Conifer:
                    Conifer(f);
                    break;
                case TreeKind.Birch:
                    Birch(f);
                    break;
                case TreeKind.Yew:
                    Yew(f);
                    break;
                default:
                    Broadleaf(f);
                    break;
            }
        }

        private void Broadleaf(PropFrame f)
        {
            var height = f.Range(5.5f, 8.5f) * _scale;
            var trunk = height * 0.45f;
            var radius = height * 0.3f;
            f.Prism(0f, -0.3f, 0f, f.Range(0.2f, 0.3f) * _scale, trunk + 0.6f, 6, SurfaceMaterial.Bark);
            var main = BroadleafColours[f.Random.Next(BroadleafColours.Length)];
            f.Blob(0f, trunk + (radius * 0.75f), 0f, radius, radius * 0.85f, radius, main, 0.18f);

            var extra = 2 + f.Random.Next(2);
            for (var i = 0; i < extra; i++)
            {
                var angle = (MathF.PI * 2f * i / extra) + f.Range(0f, 1f);
                var offset = radius * f.Range(0.55f, 0.8f);
                var size = radius * f.Range(0.55f, 0.75f);
                var colour = f.Random.NextDouble() < 0.7 ? main : BroadleafColours[f.Random.Next(BroadleafColours.Length)];
                f.Blob(MathF.Cos(angle) * offset, trunk + (radius * f.Range(0.4f, 1.1f)), MathF.Sin(angle) * offset, size, size * 0.85f, size, colour, 0.2f);
            }
        }

        private void Conifer(PropFrame f)
        {
            var height = f.Range(7f, 12f) * _scale;
            f.Prism(0f, -0.3f, 0f, 0.2f * _scale, 1.6f * _scale, 6, SurfaceMaterial.Bark);
            var tiers = 3 + f.Random.Next(2);
            var colour = f.Random.NextDouble() < 0.75 ? SurfaceMaterial.PineGreen : SurfaceMaterial.LeafDark;
            for (var i = 0; i < tiers; i++)
            {
                var t = i / (float)tiers;
                var radius = height * 0.27f * (1f - (t * 0.75f));
                var baseY = (1.1f * _scale) + (height * 0.78f * t);
                f.Cone(0f, baseY, 0f, radius, height * 0.36f, 7, colour, capBase: true);
            }
        }

        private void Birch(PropFrame f)
        {
            var height = f.Range(5f, 7.5f) * _scale;
            f.Prism(0f, -0.3f, 0f, 0.12f * _scale, height * 0.8f, 6, SurfaceMaterial.BirchBark);
            var crowns = 2 + f.Random.Next(2);
            for (var i = 0; i < crowns; i++)
            {
                var y = height * (0.55f + (0.2f * i));
                f.Blob(f.Range(-0.5f, 0.5f), y, f.Range(-0.5f, 0.5f), 1.1f * _scale, 1.6f * _scale, 1.1f * _scale, i == 0 ? SurfaceMaterial.LeafLight : SurfaceMaterial.LeafGreen, 0.2f);
            }
        }

        private void Yew(PropFrame f)
        {
            var scale = _scale * f.Range(0.85f, 1.15f);
            f.Prism(0f, -0.3f, 0f, 0.35f * scale, 1.4f * scale, 6, SurfaceMaterial.Bark);
            f.Blob(0f, 2.2f * scale, 0f, 2.3f * scale, 1.7f * scale, 2.3f * scale, SurfaceMaterial.LeafDark, 0.15f);
            f.Blob(0f, 3.6f * scale, 0f, 1.5f * scale, 1.3f * scale, 1.5f * scale, SurfaceMaterial.PineGreen, 0.15f);
        }
    }
}
