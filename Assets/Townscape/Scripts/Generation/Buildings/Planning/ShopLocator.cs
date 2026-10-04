using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Buildings.Styles;

namespace Townscape.Generation.Buildings.Planning
{
    /// <summary>Where the camera stands to look at a shop: positions are (x, height, z).</summary>
    public readonly struct ShopView
    {
        public ShopView(Vector3 eye, Vector3 lookAt)
        {
            Eye = eye;
            LookAt = lookAt;
        }

        public Vector3 Eye { get; }

        public Vector3 LookAt { get; }
    }

    /// <summary>Finds where a shop is in the village, so the camera can go to it.</summary>
    public static class ShopLocator
    {
        /// <summary>How far across the street from the shopfront the camera stands.</summary>
        public const float ViewDistance = 8.5f;

        /// <summary>How far along the street, to see the shopfront at a slight angle.</summary>
        public const float ViewSideways = 2.5f;

        public const float EyeHeight = 2.1f;

        /// <summary>About the height of the shop's fascia sign.</summary>
        public const float SignHeight = 2.6f;

        /// <summary>The building with <paramref name="shop"/> on its ground floor, or null if the village doesn't have it.</summary>
        public static BuildingPlan Find(IEnumerable<BuildingPlan> buildings, ShopDefinition shop) =>
            buildings.FirstOrDefault(plan => plan.Style is TerracedUnitStyle terraced && terraced.Design.Shop == shop);

        /// <summary>
        /// Across the street from the shop, a little to one side, at eye height, looking at the sign.
        /// Used by the game's camera and by the preview renders, so both see the same thing.
        /// </summary>
        /// <param name="groundHeight">Ground height at a ground-plane position (x, z).</param>
        public static ShopView ViewOf(Footprint shop, Func<Vector2, float> groundHeight)
        {
            var front = (shop.FrontLeft + shop.FrontRight) * 0.5f;
            var along = Vector2.Normalize(shop.FrontRight - shop.FrontLeft);
            var eye = front + (shop.Outward * ViewDistance) + (along * ViewSideways);
            return new ShopView(
                new Vector3(eye.X, groundHeight(eye) + EyeHeight, eye.Y),
                new Vector3(front.X, groundHeight(front) + SignHeight, front.Y));
        }
    }
}
