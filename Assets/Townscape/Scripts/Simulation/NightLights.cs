using System.Collections.Generic;
using Townscape.Generation;
using Townscape.Generation.Geometry;

namespace Townscape.Simulation
{
    /// <summary>A linear RGB colour, possibly brighter than 1 (HDR) for emission.</summary>
    public readonly struct Rgb
    {
        public Rgb(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public float R { get; }

        public float G { get; }

        public float B { get; }

        public static Rgb Black => new Rgb(0f, 0f, 0f);

        public static Rgb operator *(Rgb colour, float scale) => new Rgb(colour.R * scale, colour.G * scale, colour.B * scale);
    }

    /// <summary>
    /// A real light to place at an anchor: colour, reach and brightness at full power. Colours and
    /// intensities are linear and in Unity's units, where diffuse light is not divided by π.
    /// </summary>
    public readonly struct LightSpec
    {
        public LightSpec(Rgb colour, float range, float intensity, float outward)
        {
            Colour = colour;
            Range = range;
            Intensity = intensity;
            Outward = outward;
        }

        public Rgb Colour { get; }

        public float Range { get; }

        public float Intensity { get; }

        /// <summary>How far to move the light out along the anchor's facing (a shop light sits outside the glass).</summary>
        public float Outward { get; }
    }

    /// <summary>
    /// What glows and how brightly at a given moment: emission for each glowing material and
    /// the brightness of each anchored light. The one place the look of the town at night is defined.
    /// </summary>
    public static class NightLights
    {
        // Street lamps are warm white, like an old filament lamp rather than orange sodium. The glass
        // is more saturated than the light because ACES tone mapping bleaches bright glows towards
        // white: this comes out as a creamy white.
        private static readonly Rgb Lamp = new Rgb(1.0f, 0.8f, 0.56f);
        private static readonly Rgb LampGlow = new Rgb(1.0f, 0.55f, 0.24f);
        private static readonly Rgb ShopWarm = new Rgb(1.0f, 0.66f, 0.36f);
        private static readonly Rgb InnWarm = new Rgb(1.0f, 0.55f, 0.24f);
        private static readonly Rgb SignWhite = new Rgb(1.0f, 0.95f, 0.85f);
        // Very saturated for the same reason, so the flash reads as amber-orange rather than yellow.
        private static readonly Rgb BeaconOrange = new Rgb(1.0f, 0.13f, 0.006f);
        private static readonly Rgb BeaconSpill = new Rgb(1.0f, 0.42f, 0.1f);
        private static readonly Rgb ScreenBlue = new Rgb(0.45f, 0.72f, 1.0f);
        private static readonly Rgb Television = new Rgb(0.55f, 0.7f, 1.0f);

        // The canopy over the pumps is lit a cool, clean white, unlike everything else in the village.
        private static readonly Rgb CanopyWhite = new Rgb(0.88f, 0.94f, 1.0f);

        // String-light bulbs: red, green, orange, yellow, blue. Nearly pure and not too bright, because ACES
        // tone mapping turns a bright colour with a little of the other channels in it into a pastel.
        private static readonly Rgb[] Bulbs =
        {
            new Rgb(1.0f, 0.01f, 0.01f), new Rgb(0.02f, 1.0f, 0.08f), new Rgb(1.0f, 0.25f, 0.0f), new Rgb(1.0f, 0.72f, 0.02f), new Rgb(0.04f, 0.16f, 1.0f),
        };

        private static readonly Rgb[] HomeWarmth =
        {
            new Rgb(1.0f, 0.5f, 0.17f), new Rgb(1.0f, 0.44f, 0.13f), new Rgb(1.0f, 0.58f, 0.26f), new Rgb(0.95f, 0.48f, 0.2f),
        };

        private static readonly SurfaceMaterial[] Emissive =
        {
            SurfaceMaterial.LampGlass, SurfaceMaterial.SignGlass, SurfaceMaterial.Beacon, SurfaceMaterial.Screen,
            SurfaceMaterial.Interior, SurfaceMaterial.WindowShop, SurfaceMaterial.InnWindow,
            SurfaceMaterial.Window0, SurfaceMaterial.Window1, SurfaceMaterial.Window2, SurfaceMaterial.Window3,
            SurfaceMaterial.Window4, SurfaceMaterial.Window5, SurfaceMaterial.Window6, SurfaceMaterial.Window7,
            SurfaceMaterial.CanopyLight, SurfaceMaterial.BulbRed, SurfaceMaterial.BulbGreen, SurfaceMaterial.BulbOrange,
            SurfaceMaterial.BulbYellow, SurfaceMaterial.BulbBlue,
        };

        /// <summary>Materials whose emission changes with the time of day.</summary>
        public static IReadOnlyList<SurfaceMaterial> EmissiveMaterials => Emissive;

        /// <summary>Emission for <paramref name="material"/> (black if it never glows).</summary>
        public static Rgb Emission(SurfaceMaterial material, float hour, float darkness, float time)
        {
            var lamps = LightSchedule.StreetLamp(0, darkness);
            switch (material)
            {
                case SurfaceMaterial.LampGlass:
                    return LampGlow * (0.12f + (1.5f * lamps));
                case SurfaceMaterial.SignGlass:
                    return SignWhite * (2.2f * LightSchedule.Sign(darkness));
                case SurfaceMaterial.Beacon:
                    return BeaconOrange * (0.15f + (1.15f * LightSchedule.BeaconPulse(time)));
                case SurfaceMaterial.Screen:
                    return ScreenBlue * (0.8f + (1.2f * darkness));
                case SurfaceMaterial.Interior:
                    return ShopWarm * (0.08f + (0.3f * LightSchedule.Shop(hour) * (0.4f + (0.6f * darkness))));
                case SurfaceMaterial.WindowShop:
                    return ShopWarm * (0.8f * LightSchedule.Shop(hour) * (0.25f + (0.75f * darkness)));
                case SurfaceMaterial.InnWindow:
                    return InnWarm * (1.4f * LightSchedule.Inn(hour) * (0.35f + (0.65f * darkness)));
                case SurfaceMaterial.CanopyLight:
                    return CanopyWhite * (2.4f * LightSchedule.Canopy(darkness));
            }

            var bulb = BulbColour(material);
            if (bulb >= 0)
            {
                return Bulbs[bulb] * (0.9f * LightSchedule.StringLights(bulb, darkness, time));
            }

            var group = WindowGroup(material);
            if (group < 0)
            {
                return Rgb.Black;
            }

            var level = LightSchedule.HomeWindow(group, hour, darkness);
            if (group == LightSchedule.TelevisionGroup)
            {
                return Television * (0.9f * level * LightSchedule.Television(time, group));
            }

            return HomeWarmth[group % HomeWarmth.Length] * (0.72f * level);
        }

        /// <summary>The light to place at an anchor of this kind, if any.</summary>
        public static bool TryGetLight(AnchorKind kind, out LightSpec spec)
        {
            switch (kind)
            {
                case AnchorKind.StreetLamp:
                    spec = new LightSpec(Lamp, 13f, 11.5f, 0f);
                    return true;
                case AnchorKind.ShopWindow:
                    spec = new LightSpec(ShopWarm, 7.2f, 6.7f, 1.25f);
                    return true;
                case AnchorKind.DoorLamp:
                    spec = new LightSpec(InnWarm, 5.4f, 5.8f, 0.25f);
                    return true;
                case AnchorKind.Beacon:
                    spec = new LightSpec(BeaconSpill, 4.5f, 3.5f, 0f);
                    return true;
                case AnchorKind.LitSign:
                    spec = new LightSpec(SignWhite, 3.6f, 4f, 0f);
                    return true;
                case AnchorKind.CanopyLight:
                    spec = new LightSpec(CanopyWhite, 9f, 7f, 0f);
                    return true;
                default:
                    spec = default;
                    return false;
            }
        }

        /// <summary>How brightly the light at an anchor shines right now, from 0 to 1.</summary>
        public static float LightLevel(AnchorKind kind, int seed, float hour, float darkness, float time)
        {
            switch (kind)
            {
                case AnchorKind.StreetLamp:
                    return LightSchedule.StreetLamp(seed, darkness);
                case AnchorKind.ShopWindow:
                    return LightSchedule.Shop(hour) * darkness;
                case AnchorKind.DoorLamp:
                    return LightSchedule.StreetLamp(seed, darkness);
                case AnchorKind.Beacon:
                    return LightSchedule.BeaconPulse(time) * (0.3f + (0.7f * darkness));
                case AnchorKind.LitSign:
                    return SmoothStepDarkness(darkness);
                case AnchorKind.CanopyLight:
                    return SmoothStepDarkness(darkness);
                default:
                    return 0f;
            }
        }

        /// <summary>Which string-light colour a material is (red, green, orange, yellow, blue), or -1.</summary>
        public static int BulbColour(SurfaceMaterial material)
        {
            var offset = material - SurfaceMaterial.BulbRed;
            return offset >= 0 && offset < 5 ? offset : -1;
        }

        /// <summary>Which home window group a material is, or -1.</summary>
        public static int WindowGroup(SurfaceMaterial material)
        {
            var offset = material - SurfaceMaterial.Window0;
            return offset >= 0 && offset < LightSchedule.HomeWindowGroups ? offset : -1;
        }

        private static float SmoothStepDarkness(float darkness) => darkness < 0.2f ? 0f : (darkness > 0.35f ? 1f : (darkness - 0.2f) / 0.15f);
    }
}
