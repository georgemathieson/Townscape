using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation.Geometry;
using Townscape.Simulation.Weather;

namespace Townscape.Tests.Simulation
{
    public sealed class SnowstormProfileTests
    {
        private static readonly SnowstormProfile Snowstorm = new SnowstormProfile();

        [Test]
        public void Settings_AtZero_MeanNoSnowWindOrLightning()
        {
            var calm = Snowstorm.Sample(new WeatherSettings(0f, 0f, 0f, 0f), 12f);

            Assert.That(calm.Snow, Is.EqualTo(0f));
            Assert.That(calm.StrikesPerMinute, Is.EqualTo(0f));
            Assert.That(calm.WindSpeed, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Snowstorms_NeverRain_AndThunderstormsNeverSnow()
        {
            var everything = new WeatherSettings(1f, 1f, 1f, 1f);
            var storm = new ThunderstormProfile();

            for (var t = 0f; t < 300f; t += 7.3f)
            {
                Assert.That(Snowstorm.Sample(everything, t).Rain, Is.EqualTo(0f));
                Assert.That(storm.Sample(everything, t).Snow, Is.EqualTo(0f));
            }
        }

        [Test]
        public void Settings_AtFull_GiveHeavySnowInAGale()
        {
            var samples = Enumerable.Range(0, 200).Select(i => Snowstorm.Sample(new WeatherSettings(0f, 1f, 1f, 1f), i * 3.1f)).ToList();

            Assert.That(samples.Average(s => s.Snow), Is.GreaterThan(0.65f));
            Assert.That(samples.Average(s => s.WindSpeed), Is.GreaterThan(ThunderstormProfile.GaleSpeed * 0.8f));
            Assert.That(samples.All(s => s.Snow <= 1f && s.Mist >= 0f && s.Mist <= 1f));
        }

        [Test]
        public void Snow_ComesInSqualls()
        {
            var settings = new WeatherSettings(0f, 0.5f, 0.45f, 0.7f);
            var snow = Enumerable.Range(0, 100).Select(i => Snowstorm.Sample(settings, i * 7f).Snow).ToList();

            Assert.That(snow.Max() - snow.Min(), Is.GreaterThan(0.15f));
        }

        [Test]
        public void Wind_BlowsFromTheNorthEast()
        {
            var settings = new WeatherSettings(0f, 0f, 0.6f, 0.7f);
            var mean = Enumerable.Range(0, 300).Aggregate(Vector2.Zero, (sum, i) => sum + Snowstorm.Sample(settings, i * 4f).Wind);

            Assert.That(mean.X, Is.LessThan(0f), "towards the west");
            Assert.That(mean.Y, Is.LessThan(0f), "towards the south");
            Assert.That(WindDirection.From(mean), Is.EqualTo("NE"));
        }

        [Test]
        public void Thundersnow_IsRarerThanAThunderstorm()
        {
            var settings = new WeatherSettings(1f, 1f, 0.5f, 1f);
            var storm = new ThunderstormProfile();
            var snowStrikes = Enumerable.Range(0, 200).Average(i => Snowstorm.Sample(settings, i * 5f).StrikesPerMinute);
            var stormStrikes = Enumerable.Range(0, 200).Average(i => storm.Sample(settings, i * 5f).StrikesPerMinute);

            Assert.That(snowStrikes, Is.GreaterThan(0f));
            Assert.That(snowStrikes, Is.LessThan(stormStrikes / 3f));
        }
    }

    public sealed class SnowFallTests
    {
        [Test]
        public void Flakes_FallSlowly_AndDriftWithTheWind()
        {
            var wind = new Vector2(6f, -2f);
            var velocity = SnowFall.Velocity(wind);

            Assert.That(-velocity.Y, Is.LessThan(RainFall.FallSpeed / 5f));
            Assert.That(velocity.X, Is.GreaterThan(wind.X * 0.9f));
            Assert.That(velocity.Z, Is.LessThan(wind.Y * 0.9f));
        }

        [Test]
        public void HeavierSnow_HasMoreFlakes()
        {
            Assert.That(SnowFall.FlakesPerCubicMetre(0f), Is.EqualTo(0f));
            Assert.That(SnowFall.FlakesPerCubicMetre(0.3f), Is.LessThan(SnowFall.FlakesPerCubicMetre(0.8f)));
            Assert.That(SnowFall.FlakesPerCubicMetre(2f), Is.EqualTo(SnowFall.MaxFlakesPerCubicMetre));
        }

        [Test]
        public void Flakes_FlutterMoreInTheWind()
        {
            Assert.That(SnowFall.Flutter(12f), Is.GreaterThan(SnowFall.Flutter(0f)));
        }
    }

    public sealed class SnowCoverTests
    {
        private static float Whiteness(SurfaceMaterial material, float cover)
        {
            var bare = SurfacePalette.Get(material);
            var snowy = SnowCover.Apply(material, bare, cover);
            return (snowy.R + snowy.G + snowy.B) / 3f;
        }

        [Test]
        public void BareSurfaces_AreUnchanged()
        {
            var bare = SurfacePalette.Get(SurfaceMaterial.Grass);
            var same = SnowCover.Apply(SurfaceMaterial.Grass, bare, 0f);

            Assert.That(same.R, Is.EqualTo(bare.R));
            Assert.That(same.Smoothness, Is.EqualTo(bare.Smoothness));
        }

        [Test]
        public void DeepSnow_WhitensRoofsGrassAndPavements()
        {
            foreach (var material in new[] { SurfaceMaterial.Slate, SurfaceMaterial.Pantile, SurfaceMaterial.Grass, SurfaceMaterial.FellGrass, SurfaceMaterial.Pavement })
            {
                Assert.That(Whiteness(material, 1f), Is.GreaterThan(0.85f), material.ToString());
            }
        }

        [Test]
        public void Walls_GlassAndWater_TakeNoSnow()
        {
            foreach (var material in new[] { SurfaceMaterial.RenderWhite, SurfaceMaterial.StoneGreen, SurfaceMaterial.ShopGlass, SurfaceMaterial.Water, SurfaceMaterial.RiverWater, SurfaceMaterial.Window3, SurfaceMaterial.PaintRed })
            {
                var bare = SurfacePalette.Get(material);
                var snowy = SnowCover.Apply(material, bare, 1f);
                Assert.That(snowy.R, Is.EqualTo(bare.R), material.ToString());
                Assert.That(SnowCover.Affected, Does.Not.Contain(material));
            }
        }

        [Test]
        public void Roofs_WhitenBeforeTheRoad_WhichStaysAGreySlush()
        {
            Assert.That(Whiteness(SurfaceMaterial.Slate, 0.4f), Is.GreaterThan(0.7f));
            Assert.That(Whiteness(SurfaceMaterial.Road, 0.4f), Is.LessThan(Whiteness(SurfaceMaterial.Road, 0f) + 0.05f));
            Assert.That(Whiteness(SurfaceMaterial.Road, 1f), Is.LessThan(Whiteness(SurfaceMaterial.Pavement, 1f) - 0.2f));
            Assert.That(Whiteness(SurfaceMaterial.Road, 1f), Is.GreaterThan(Whiteness(SurfaceMaterial.Road, 0f) + 0.2f));
        }

        [Test]
        public void Snow_SettlesInAMinuteOrTwo_ThawsSlowly_AndRainWashesItAway()
        {
            var cover = 0f;
            for (var t = 0f; t < 60f; t += 0.1f)
            {
                cover = SnowCover.Step(cover, 0.75f, 0f, 0.1f);
            }

            Assert.That(cover, Is.InRange(0.5f, 0.85f), "settling after a minute of heavy snow");

            for (var t = 0f; t < 60f; t += 0.1f)
            {
                cover = SnowCover.Step(cover, 0.75f, 0f, 0.1f);
            }

            Assert.That(cover, Is.EqualTo(1f), "deep after two");

            for (var t = 0f; t < 60f; t += 0.1f)
            {
                cover = SnowCover.Step(cover, 0f, 0f, 0.1f);
            }

            Assert.That(cover, Is.InRange(0.75f, 0.9f), "still lying a minute after it stops");

            for (var t = 0f; t < 90f; t += 0.1f)
            {
                cover = SnowCover.Step(cover, 0f, 0.75f, 0.1f);
            }

            Assert.That(cover, Is.EqualTo(0f), "washed away by heavy rain");
        }

        [Test]
        public void LightSnow_StillSettles_InTheEnd()
        {
            var cover = 0f;
            for (var t = 0f; t < 600f; t += 0.5f)
            {
                cover = SnowCover.Step(cover, 0.15f, 0f, 0.5f);
            }

            Assert.That(cover, Is.EqualTo(1f));
        }

        [Test]
        public void Snow_LiesOnTopOfWetSurfaces()
        {
            var dry = SurfacePalette.Get(SurfaceMaterial.Pavement);
            var wet = SurfaceWeather.Apply(SurfaceMaterial.Pavement, dry, 1f, 0f);
            var snowy = SurfaceWeather.Apply(SurfaceMaterial.Pavement, dry, 1f, 1f);

            Assert.That(wet.R, Is.LessThan(dry.R));
            Assert.That(snowy.R, Is.GreaterThan(dry.R));
            Assert.That(SurfaceWeather.Affected, Is.SupersetOf(Wetness.Affected).And.SupersetOf(SnowCover.Affected));
        }
    }

    public sealed class WeatherFogTests
    {
        private static WeatherConditions Conditions(float snow, float windSpeed, float mist = 0.5f) =>
            new WeatherConditions(0f, new Vector2(windSpeed, 0f), 0f, mist, snow);

        [Test]
        public void WithoutSnow_TheFogOnlyThickensWithTheMist()
        {
            Assert.That(WeatherFog.Scale(Conditions(0f, 8f, 0.5f)), Is.EqualTo(1.05f).Within(1e-5f));
            Assert.That(WeatherFog.Whiteness(Conditions(0f, 8f)), Is.EqualTo(0f));
        }

        [Test]
        public void ABlizzard_ClosesTheValleyIn()
        {
            var light = WeatherFog.Scale(Conditions(0.2f, 2f));
            var heavy = WeatherFog.Scale(Conditions(1f, 2f));
            var blizzard = WeatherFog.Scale(Conditions(1f, ThunderstormProfile.GaleSpeed));

            Assert.That(light, Is.LessThan(heavy));
            Assert.That(heavy, Is.LessThan(blizzard));
            Assert.That(blizzard, Is.GreaterThan(2.5f), "visibility down to a third");
            Assert.That(WeatherFog.Whiteness(Conditions(0.8f, 6f)), Is.GreaterThan(0.6f));
        }
    }

    public sealed class ColdChimneyTests
    {
        [Test]
        public void MoreFires_BurnInTheCold()
        {
            var mild = Enumerable.Range(0, 400).Count(seed => ChimneySmoke.IsBurning(seed, 13f));
            var snowy = Enumerable.Range(0, 400).Count(seed => ChimneySmoke.IsBurning(seed, 13f, cold: 1f));

            Assert.That(snowy, Is.GreaterThan(mild + 60));
        }
    }
}
