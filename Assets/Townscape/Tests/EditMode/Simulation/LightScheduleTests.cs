using System.Linq;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Simulation;

namespace Townscape.Tests.Simulation
{
    public sealed class LightScheduleTests
    {
        private const float Night = 1f;
        private const float Dusk = 0.55f;
        private const float Day = 0.08f;

        private static float LitWindows(float hour, float darkness) =>
            Enumerable.Range(0, LightSchedule.HomeWindowGroups).Sum(g => LightSchedule.HomeWindow(g, hour, darkness));

        [Test]
        public void HomeWindows_AreMostlyDarkAtMidday()
        {
            Assert.That(LitWindows(12.5f, Day), Is.LessThanOrEqualTo(1f), "only the gloomy-day group at most");
        }

        [Test]
        public void HomeWindows_AreMostlyLitInTheEvening()
        {
            Assert.That(LitWindows(20.5f, Night), Is.GreaterThan(LightSchedule.HomeWindowGroups * 0.75f));
        }

        [Test]
        public void HomeWindows_ComeOnOneByOneAsDuskDeepens()
        {
            var lit = new[] { 0.1f, 0.25f, 0.35f, 0.5f }.Select(d => LitWindows(19f, d)).ToList();

            for (var i = 1; i < lit.Count; i++)
            {
                Assert.That(lit[i], Is.GreaterThanOrEqualTo(lit[i - 1]));
            }

            Assert.That(lit.First(), Is.LessThan(lit.Last()));
        }

        [Test]
        public void HomeWindows_GoDarkAtBedtime()
        {
            Assert.That(LitWindows(3.5f, Night), Is.LessThan(LitWindows(21f, Night) * 0.25f));
        }

        [Test]
        public void StreetLamps_AreOnAtNightAndOffByDay()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                Assert.That(LightSchedule.StreetLamp(seed, Night), Is.EqualTo(1f).Within(1e-4f));
                Assert.That(LightSchedule.StreetLamp(seed, Day), Is.EqualTo(0f).Within(1e-4f));
            }
        }

        [Test]
        public void StreetLamps_ComeOnAtSlightlyDifferentTimes()
        {
            var levels = Enumerable.Range(0, 50).Select(seed => LightSchedule.StreetLamp(seed, 0.36f)).ToList();

            Assert.That(levels.Any(l => l > 0.99f), Is.True);
            Assert.That(levels.Any(l => l < 0.01f), Is.True);
        }

        [Test]
        public void Beacon_FlashesOnAndOff()
        {
            var samples = Enumerable.Range(0, 80).Select(i => LightSchedule.BeaconPulse(i * 0.01f)).ToList();

            Assert.That(samples.Max(), Is.GreaterThan(0.95f));
            Assert.That(samples.Min(), Is.LessThan(0.05f));
        }

        [TestCase(0f, 1f, 0.5f, 1f)]
        [TestCase(23f, 2f, 0.5f, 1f)]
        [TestCase(23f, 2f, 22f, 0f)]
        [TestCase(8f, 16f, 20f, 0f)]
        public void Window_WrapsPastMidnight(float start, float end, float hour, float expected)
        {
            Assert.That(LightSchedule.Window(hour, start, end), Is.EqualTo(expected).Within(1e-3f));
        }

        [Test]
        public void SwitchOnFlicker_SettlesToFull()
        {
            Assert.That(LightSchedule.SwitchOnFlicker(2f, 5), Is.EqualTo(1f));
            var early = Enumerable.Range(0, 12).Select(i => LightSchedule.SwitchOnFlicker(i * 0.07f, 5)).ToList();
            Assert.That(early.Any(v => v < 1f), Is.True, "flickers while warming up");
        }

        [Test]
        public void Emission_GlowsAtNightNotAtNoon()
        {
            foreach (var material in NightLights.EmissiveMaterials)
            {
                if (material == SurfaceMaterial.Beacon || material == SurfaceMaterial.Screen)
                {
                    continue;
                }

                var noon = NightLights.Emission(material, 12.5f, Day, 0f);
                var night = NightLights.Emission(material, 20.5f, Night, 0f);
                Assert.That(Brightness(night), Is.GreaterThanOrEqualTo(Brightness(noon)), material.ToString());
            }

            // The bloom threshold on the post-processing volume is 1.05.
            Assert.That(NightLights.Emission(SurfaceMaterial.LampGlass, 21f, Night, 0f).R, Is.GreaterThan(1.1f), "lamps glow enough to bloom");
            Assert.That(Brightness(NightLights.Emission(SurfaceMaterial.Grass, 21f, Night, 0f)), Is.EqualTo(0f));
        }

        [Test]
        public void Beacon_FlashesOrange()
        {
            var flash = NightLights.Emission(SurfaceMaterial.Beacon, 21f, Night, 0.2f);

            Assert.That(LightSchedule.BeaconPulse(0.2f), Is.EqualTo(1f).Within(1e-3f), "0.2 s is mid-flash");
            Assert.That(flash.R, Is.GreaterThan(1.1f), "blooms");
            Assert.That(flash.G, Is.LessThan(flash.R * 0.2f), "orange, not yellow");
            Assert.That(flash.B, Is.LessThan(flash.G));
        }

        [Test]
        public void EveryLitAnchorKindHasALight()
        {
            foreach (var kind in new[] { AnchorKind.StreetLamp, AnchorKind.ShopWindow, AnchorKind.DoorLamp, AnchorKind.Beacon, AnchorKind.LitSign })
            {
                Assert.That(NightLights.TryGetLight(kind, out var spec), Is.True, kind.ToString());
                Assert.That(spec.Range, Is.GreaterThan(1f));
            }

            Assert.That(NightLights.TryGetLight(AnchorKind.Window, out _), Is.False, "windows glow through emission, not lights");
        }

        [Test]
        public void WindowGroups_MapToTheirIndex()
        {
            Assert.That(NightLights.WindowGroup(SurfaceMaterial.Window0), Is.EqualTo(0));
            Assert.That(NightLights.WindowGroup(SurfaceMaterial.Window7), Is.EqualTo(7));
            Assert.That(NightLights.WindowGroup(SurfaceMaterial.WindowGlass), Is.EqualTo(-1));
        }

        private static float Brightness(Rgb colour) => colour.R + colour.G + colour.B;
    }
}
