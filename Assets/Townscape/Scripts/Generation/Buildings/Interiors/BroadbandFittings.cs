using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Interiors
{
    /// <summary>
    /// A customer's fibre broadband kit on a wall: the white optical network terminal (ONT) the
    /// fibre ends at, with its lights down one side, and the black router beside it with a row of
    /// lights along the bottom, joined by a short network cable. The lights are dark spots until
    /// Unity lights them from the street cabinet's state.
    /// </summary>
    public static class BroadbandFittings
    {
        public const float OntWidth = 0.15f;
        public const float OntHeight = 0.2f;
        public const float OntDepth = 0.04f;

        public const float RouterWidth = 0.24f;
        public const float RouterHeight = 0.17f;
        public const float RouterDepth = 0.035f;

        /// <summary>
        /// The ONT and router on a wall running back through the building at <paramref name="wallX"/>,
        /// standing out towards <paramref name="outX"/> (1 to the right, -1 to the left), centred
        /// at height <paramref name="y"/>: the ONT at depth <paramref name="ontD"/> and the router at
        /// <paramref name="routerD"/>, with the fibre running down the wall to the floor.
        /// <paramref name="planMbps"/> is the speed the customer pays for.
        /// </summary>
        public static TownBroadband Fit(BuildContext context, UnitSpace space, string customer, float planMbps, float wallX, float outX, float floor, float y, float ontD, float routerD)
        {
            var builder = context.Builder;
            void Slab(float from, float to, float y0, float d0, float y1, float d1, SurfaceMaterial material) =>
                space.Box(builder, System.Math.Min(from, to), y0, System.Math.Min(d0, d1), System.Math.Max(from, to), y1, System.Math.Max(d0, d1), material);

            var outward = space.Right * outX;

            // The ONT: a white case with a grey label, its four lights in a column on the router's side.
            var ontFace = wallX + (outX * OntDepth);
            var toRouter = routerD > ontD ? 1f : -1f;
            Slab(wallX, ontFace, y - (OntHeight * 0.5f), ontD - (OntWidth * 0.5f), y + (OntHeight * 0.5f), ontD + (OntWidth * 0.5f), SurfaceMaterial.Porcelain);
            Slab(ontFace, ontFace + (outX * 0.002f), y - 0.07f, ontD - (toRouter * 0.05f), y - 0.02f, ontD + (toRouter * 0.01f), SurfaceMaterial.Chrome);
            var ontLedD = ontD + (toRouter * 0.045f);
            var ontLeds = new Vector3[4];
            for (var i = 0; i < ontLeds.Length; i++)
            {
                var ledY = y + 0.07f - (i * 0.028f);
                Slab(ontFace, ontFace + (outX * 0.003f), ledY - 0.004f, ontLedD - 0.004f, ledY + 0.004f, ontLedD + 0.004f, SurfaceMaterial.Iron);
                ontLeds[i] = space.At(ontFace + (outX * 0.003f), ledY, ontLedD);
            }

            // The router: black, with its three lights (pale until lit) along the bottom.
            var routerFace = wallX + (outX * RouterDepth);
            Slab(wallX, routerFace, y - (RouterHeight * 0.5f), routerD - (RouterWidth * 0.5f), y + (RouterHeight * 0.5f), routerD + (RouterWidth * 0.5f), SurfaceMaterial.PaintBlack);
            var routerLeds = new Vector3[3];
            for (var i = 0; i < routerLeds.Length; i++)
            {
                var ledD = routerD + ((i - 1) * 0.04f * toRouter);
                var ledY = y - (RouterHeight * 0.5f) + 0.025f;
                Slab(routerFace, routerFace + (outX * 0.003f), ledY - 0.004f, ledD - 0.006f, ledY + 0.004f, ledD + 0.006f, SurfaceMaterial.Chrome);
                routerLeds[i] = space.At(routerFace + (outX * 0.003f), ledY, ledD);
            }

            // A network cable between them, and the fibre (in white) running down to the skirting.
            var cable = wallX + (outX * 0.012f);
            var cableY = y - (OntHeight * 0.5f) - 0.02f;
            Slab(wallX, cable, cableY - 0.004f, ontD, cableY + 0.004f, routerD, SurfaceMaterial.PaintBlue);
            Slab(wallX, cable, cableY - 0.004f, ontD - 0.004f, y - (OntHeight * 0.5f), ontD + 0.004f, SurfaceMaterial.PaintBlue);
            Slab(wallX, cable, cableY - 0.004f, routerD - 0.004f, y - (RouterHeight * 0.5f), routerD + 0.004f, SurfaceMaterial.PaintBlue);
            var fibreD = ontD - (toRouter * 0.04f);
            Slab(wallX, cable, floor, fibreD - 0.004f, y - (OntHeight * 0.5f), fibreD + 0.004f, SurfaceMaterial.PaintWhite);

            var broadband = new TownBroadband(
                customer,
                planMbps,
                new WallMount(space.At(ontFace, y, ontD), outward, OntWidth, OntHeight),
                ontLeds,
                new WallMount(space.At(routerFace, y, routerD), outward, RouterWidth, RouterHeight),
                routerLeds);
            context.Broadband.Add(broadband);
            return broadband;
        }
    }
}
