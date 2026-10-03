using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Simulation.Weather;
using Townscape.Tests.Generation;

namespace Townscape.Tests.Simulation
{
    public sealed class ThunderstormProfileTests
    {
        private static readonly ThunderstormProfile Storm = new ThunderstormProfile();

        [Test]
        public void Settings_AtZero_MeanNoRainWindOrLightning()
        {
            var calm = Storm.Sample(new WeatherSettings(0f, 0f, 0f), 12f);

            Assert.That(calm.Rain, Is.EqualTo(0f));
            Assert.That(calm.StrikesPerMinute, Is.EqualTo(0f));
            Assert.That(calm.WindSpeed, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Settings_AtFull_GiveADownpourAGaleAndFrequentLightning()
        {
            var samples = Enumerable.Range(0, 200).Select(i => Storm.Sample(new WeatherSettings(1f, 1f, 1f), i * 3.1f)).ToList();

            Assert.That(samples.Average(s => s.Rain), Is.GreaterThan(0.7f));
            Assert.That(samples.Average(s => s.WindSpeed), Is.GreaterThan(ThunderstormProfile.GaleSpeed * 0.8f));
            Assert.That(samples.Average(s => s.StrikesPerMinute), Is.GreaterThan(4f));
            Assert.That(samples.All(s => s.Rain <= 1f && s.Mist <= 1f && s.Mist >= 0f));
        }

        [Test]
        public void Conditions_Vary_OverTime()
        {
            var settings = new WeatherSettings(0.75f, 0.5f, 0.45f);
            var rain = Enumerable.Range(0, 100).Select(i => Storm.Sample(settings, i * 7f).Rain).ToList();
            var strikes = Enumerable.Range(0, 100).Select(i => Storm.Sample(settings, i * 7f).StrikesPerMinute).ToList();

            Assert.That(rain.Max() - rain.Min(), Is.GreaterThan(0.1f), "rain comes in surges");
            Assert.That(strikes.Max() - strikes.Min(), Is.GreaterThan(1f), "storm cells come and go");
        }

        [Test]
        public void Wind_BlowsFromTheSouthWest()
        {
            var settings = new WeatherSettings(0.5f, 0.5f, 0.6f);
            var mean = Enumerable.Range(0, 300).Aggregate(Vector2.Zero, (sum, i) => sum + Storm.Sample(settings, i * 4f).Wind);

            Assert.That(mean.X, Is.GreaterThan(0f), "towards the east");
            Assert.That(mean.Y, Is.GreaterThan(0f), "towards the north");
        }
    }

    public sealed class WindSwayTests
    {
        private static readonly Vector2 Gale = new Vector2(ThunderstormProfile.GaleSpeed, 0f);

        [Test]
        public void NoWind_NoSway()
        {
            Assert.That(WindSway.Offset(new Vector3(3f, 2f, 1f), 6f, Vector2.Zero, 4f), Is.EqualTo(Vector3.Zero));
        }

        [Test]
        public void TheBaseOfAPlant_StaysPut()
        {
            Assert.That(WindSway.Offset(new Vector3(3f, 0f, 1f), 0f, Gale, 4f), Is.EqualTo(Vector3.Zero));
        }

        [Test]
        public void TreeTops_SwayMoreThanLowBranches_AndStayBounded()
        {
            for (var t = 0f; t < 20f; t += 0.37f)
            {
                var low = WindSway.Offset(new Vector3(5f, 2f, 5f), 2f, Gale, t).Length();
                var top = WindSway.Offset(new Vector3(5f, 9f, 5f), 9f, Gale, t).Length();
                Assert.That(top, Is.GreaterThan(low), $"t = {t}");
                Assert.That(top, Is.LessThan(1.2f), $"t = {t}");
            }
        }

        [Test]
        public void Plants_LeanDownwind_OnAverage()
        {
            var mean = Enumerable.Range(0, 200)
                .Select(i => WindSway.Offset(new Vector3(i * 1.3f, 6f, i * 0.7f), 6f, Gale, i * 0.21f))
                .Aggregate(Vector3.Zero, (sum, o) => sum + o) / 200f;

            Assert.That(mean.X, Is.GreaterThan(0.1f));
            Assert.That(MathF.Abs(mean.Z), Is.LessThan(mean.X * 0.3f));
        }

        [Test]
        public void Grass_BarelyMoves_ComparedWithTrees()
        {
            var grass = WindSway.Offset(new Vector3(1f, 0.3f, 1f), 0.3f, Gale, 1f).Length();
            Assert.That(grass, Is.LessThan(0.05f));
        }
    }

    public sealed class LightningStormTests
    {
        [Test]
        public void Strikes_ArriveAtRoughlyTheRequestedRate()
        {
            var storm = new LightningStorm(3);
            var strikes = new List<Strike>();
            var thunder = new List<Thunder>();
            for (var t = 0f; t < 600f; t += 0.05f)
            {
                storm.Step(t, 0.05f, 6f, Vector2.Zero, Vector2.UnitY, strikes, thunder);
            }

            Assert.That(strikes.Count, Is.InRange(40, 80), "about 6 a minute for 10 minutes");
            Assert.That(strikes.Zip(strikes.Skip(1), (a, b) => b.Time - a.Time).Min(), Is.GreaterThanOrEqualTo(1.49f), "flashes stay distinct");
        }

        [Test]
        public void NoLightning_WhenTheRateIsZero()
        {
            var storm = new LightningStorm();
            var strikes = new List<Strike>();
            var thunder = new List<Thunder>();
            for (var t = 0f; t < 300f; t += 0.1f)
            {
                storm.Step(t, 0.1f, 0f, Vector2.Zero, Vector2.UnitY, strikes, thunder);
            }

            Assert.That(strikes, Is.Empty);
            Assert.That(storm.Flash(300f), Is.EqualTo(0f));
        }

        [Test]
        public void Thunder_ArrivesAfterTheFlash_DelayedByDistance()
        {
            var storm = new LightningStorm(11);
            var strikes = new List<Strike>();
            var thunder = new List<Thunder>();
            var strike = storm.StrikeNow(10f, Vector2.Zero, Vector2.UnitY);
            for (var t = 10f; t < 30f; t += 0.02f)
            {
                storm.Step(t, 0.02f, 0f, Vector2.Zero, Vector2.UnitY, strikes, thunder);
            }

            Assert.That(thunder, Has.Count.EqualTo(1));
            Assert.That(thunder[0].StrikeId, Is.EqualTo(strike.Id));
            Assert.That(thunder[0].Time, Is.EqualTo(10f + (strike.Distance / 343f)).Within(1e-3f));
            Assert.That(strike.Distance, Is.InRange(LightningStorm.MinDistance, LightningStorm.MaxDistance));
            Assert.That(Vector2.Distance(strike.Position, Vector2.Zero), Is.EqualTo(strike.Distance).Within(0.1f));
        }

        [Test]
        public void CloseThunder_IsLouderThanDistantThunder()
        {
            var close = new Thunder(0, 0f, 300f);
            var far = new Thunder(1, 0f, 2000f);

            Assert.That(close.Loudness, Is.GreaterThan(far.Loudness));
            Assert.That(close.IsCrack, Is.True);
            Assert.That(far.IsCrack, Is.False);
            Assert.That(far.Duration, Is.GreaterThan(close.Duration));
        }

        [Test]
        public void Flash_Flickers_ThenFades()
        {
            var storm = new LightningStorm(5);
            var strike = Enumerable.Range(0, 50).Select(i => storm.StrikeNow(i * 10f, Vector2.Zero, Vector2.UnitY)).First(s => s.EndTime - s.Time > 0.4f);
            var samples = Enumerable.Range(0, 40).Select(i => strike.FlashAt(strike.Time + (i * 0.01f))).ToList();

            Assert.That(strike.FlashAt(strike.Time - 0.01f), Is.EqualTo(0f));
            Assert.That(samples[0], Is.EqualTo(strike.Brightness).Within(1e-4f), "the first stroke is the brightest");
            Assert.That(samples.Zip(samples.Skip(1), (a, b) => b > a + 0.05f).Any(rises => rises), "a later stroke flickers it back up");
            Assert.That(strike.FlashAt(strike.EndTime + 0.01f), Is.LessThan(0.01f));
        }

        [Test]
        public void VisibleBolts_MostlyAppearInFrontOfTheListener()
        {
            var storm = new LightningStorm(9);
            var bolts = Enumerable.Range(0, 400).Select(i => storm.StrikeNow(i * 2f, Vector2.Zero, Vector2.UnitX)).Where(s => s.HasBolt).ToList();
            var inFront = bolts.Count(s => Vector2.Dot(Vector2.Normalize(s.Position), Vector2.UnitX) > 0.5f);

            Assert.That(bolts.Count, Is.GreaterThan(60));
            Assert.That(inFront, Is.GreaterThan(bolts.Count / 2));
        }
    }

    public sealed class LightningBoltTests
    {
        private static readonly Vector3 Cloud = new Vector3(400f, 380f, 900f);
        private static readonly Vector3 Ground = new Vector3(430f, 0f, 880f);

        [Test]
        public void MainChannel_RunsFromTheCloudToTheGround_Unbroken()
        {
            var main = LightningBolt.Generate(42, Cloud, Ground).Where(s => s.IsMain).ToList();

            Assert.That(main.First().Start, Is.EqualTo(Cloud));
            Assert.That(main.Last().End, Is.EqualTo(Ground));
            for (var i = 1; i < main.Count; i++)
            {
                Assert.That(main[i].Start, Is.EqualTo(main[i - 1].End));
            }
        }

        [Test]
        public void Bolt_HasBranches_ThatStayAboveTheGround()
        {
            var segments = LightningBolt.Generate(7, Cloud, Ground);
            var branches = segments.Where(s => !s.IsMain).ToList();

            Assert.That(branches, Is.Not.Empty);
            Assert.That(branches.All(s => s.Width < 1f));
            Assert.That(segments.All(s => s.Start.Y >= Ground.Y - 1e-3f && s.End.Y >= Ground.Y - 1e-3f));
        }

        [Test]
        public void Bolts_AreRepeatable_AndVaryBySeed()
        {
            var a = LightningBolt.Generate(1, Cloud, Ground);
            var b = LightningBolt.Generate(1, Cloud, Ground);
            var c = LightningBolt.Generate(2, Cloud, Ground);

            Assert.That(a.Select(s => s.End), Is.EqualTo(b.Select(s => s.End)));
            Assert.That(a[a.Count / 3].End, Is.Not.EqualTo(c[c.Count / 3].End));
        }
    }

    public sealed class RippleFieldTests
    {
        [Test]
        public void StillWater_IsFlat()
        {
            var pixels = new RippleField(16, 4, 0, 0f, 1).NormalMap(0, 4f);

            for (var i = 0; i < pixels.Length; i += 4)
            {
                Assert.That(pixels[i], Is.EqualTo(128));
                Assert.That(pixels[i + 1], Is.EqualTo(128));
                Assert.That(pixels[i + 2], Is.EqualTo(255));
                Assert.That(pixels[i + 3], Is.EqualTo(255));
            }
        }

        [Test]
        public void Raindrops_MakeRings_ThatMoveFromFrameToFrame()
        {
            var field = new RippleField(64, 12, 20, 0f, 3);
            var first = field.Heights(0);
            var second = field.Heights(1);

            Assert.That(first.Max(), Is.GreaterThan(0.1f));
            Assert.That(first.Zip(second, (a, b) => MathF.Abs(a - b)).Max(), Is.GreaterThan(0.05f));
        }

        [Test]
        public void Field_TilesSeamlessly()
        {
            var field = new RippleField(64, 8, 30, 1f, 5);
            for (var frame = 0; frame < field.Frames; frame++)
            {
                var heights = field.Heights(frame);
                var interior = 0f;
                var seam = 0f;
                for (var y = 0; y < 64; y++)
                {
                    for (var x = 1; x < 64; x++)
                    {
                        interior = MathF.Max(interior, MathF.Abs(heights[(y * 64) + x] - heights[(y * 64) + x - 1]));
                    }

                    seam = MathF.Max(seam, MathF.Abs(heights[y * 64] - heights[(y * 64) + 63]));
                }

                Assert.That(seam, Is.LessThanOrEqualTo(interior + 1e-4f), $"frame {frame}");
            }
        }

        [Test]
        public void Swell_Loops()
        {
            var field = new RippleField(32, 10, 0, 1f, 1);
            var beforeLoop = field.Heights(9);
            var start = field.Heights(0);
            var step = field.Heights(1);

            var loopJump = beforeLoop.Zip(start, (a, b) => MathF.Abs(a - b)).Max();
            var frameStep = start.Zip(step, (a, b) => MathF.Abs(a - b)).Max();
            Assert.That(loopJump, Is.LessThanOrEqualTo(frameStep * 1.5f), "the last frame leads smoothly into the first");
        }
    }

    public sealed class WetnessTests
    {
        [Test]
        public void DrySurfaces_AreUnchanged()
        {
            var dry = SurfacePalette.Get(SurfaceMaterial.Road);
            var same = Wetness.Apply(SurfaceMaterial.Road, dry, 0f);

            Assert.That(same.R, Is.EqualTo(dry.R));
            Assert.That(same.Smoothness, Is.EqualTo(dry.Smoothness));
        }

        [Test]
        public void WetRoads_AreDarkerAndGlossier()
        {
            var dry = SurfacePalette.Get(SurfaceMaterial.Road);
            var wet = Wetness.Apply(SurfaceMaterial.Road, dry, 1f);

            Assert.That(wet.R, Is.LessThan(dry.R * 0.7f));
            Assert.That(wet.Smoothness, Is.GreaterThan(0.8f));
        }

        [Test]
        public void Tarmac_ShinesMoreThanRender_WhenWet()
        {
            var road = Wetness.Apply(SurfaceMaterial.Road, SurfacePalette.Get(SurfaceMaterial.Road), 1f);
            var render = Wetness.Apply(SurfaceMaterial.RenderWhite, SurfacePalette.Get(SurfaceMaterial.RenderWhite), 1f);

            Assert.That(road.Smoothness, Is.GreaterThan(render.Smoothness));
        }

        [Test]
        public void GlassAndWater_AreUnaffected()
        {
            foreach (var material in new[] { SurfaceMaterial.ShopGlass, SurfaceMaterial.Water, SurfaceMaterial.Window3, SurfaceMaterial.LampGlass })
            {
                var dry = SurfacePalette.Get(material);
                var wet = Wetness.Apply(material, dry, 1f);
                Assert.That(wet.R, Is.EqualTo(dry.R), material.ToString());
                Assert.That(wet.Smoothness, Is.EqualTo(dry.Smoothness), material.ToString());
                Assert.That(Wetness.Affected, Does.Not.Contain(material));
            }
        }

        [Test]
        public void Surfaces_SoakUpQuickly_AndDrySlowly()
        {
            var wetness = 0f;
            for (var t = 0f; t < 60f; t += 0.1f)
            {
                wetness = Wetness.Step(wetness, 1f, 0.1f);
            }

            Assert.That(wetness, Is.EqualTo(1f).Within(1e-4f), "soaked after a minute of downpour");

            for (var t = 0f; t < 60f; t += 0.1f)
            {
                wetness = Wetness.Step(wetness, 0f, 0.1f);
            }

            Assert.That(wetness, Is.InRange(0.6f, 0.9f), "still damp a minute after the rain stops");
        }
    }

    public sealed class RainCatchMapTests
    {
        private static RainCatchMap _village;

        private static RainCatchMap Village => _village ??= RainCatchMap.Build(GeneratedVillage.Town, 160f, 0.5f);

        [Test]
        public void Rain_LandsOnATriangleSeenFromAbove()
        {
            var map = new RainCatchMap(Vector2.Zero, 1f, 10, 10);
            map.AddTriangle(new Vector3(0f, 2f, 0f), new Vector3(0f, 2f, 10f), new Vector3(10f, 4f, 0f));

            Assert.That(map.HeightAt(1.5f, 1.5f), Is.EqualTo(2f + (0.2f * 1.5f)).Within(1e-3f));
            Assert.That(map.HeightAt(9.5f, 9.5f), Is.EqualTo(float.NegativeInfinity), "outside the triangle");
            Assert.That(map.HeightAt(-1f, 1f), Is.EqualTo(float.NegativeInfinity), "outside the map");
        }

        [Test]
        public void Walls_CatchNothing()
        {
            var map = new RainCatchMap(Vector2.Zero, 1f, 4, 4);
            map.AddTriangle(new Vector3(1f, 0f, 1f), new Vector3(1f, 5f, 1f), new Vector3(3f, 0f, 1f));

            Assert.That(map.HeightAt(1.5f, 1f), Is.EqualTo(float.NegativeInfinity));
        }

        [Test]
        public void Rain_LandsOnRoofs_NotTheFloorsBeneath()
        {
            foreach (var chimney in GeneratedVillage.Town.Anchors.Where(a => a.Kind == AnchorKind.Chimney).Take(30))
            {
                // The pot is narrower than a cell, so the cell may hold the top of the stack instead,
                // but never a floor several metres further down.
                var height = Village.HeightAt(chimney.Position.X, chimney.Position.Z);
                Assert.That(height, Is.GreaterThan(chimney.Position.Y - 3f));
            }
        }

        [Test]
        public void Rain_LandsOnTheRiver_AtWaterLevel()
        {
            var height = Village.HeightAt(0.3f, 26.2f);

            Assert.That(height, Is.EqualTo(LakeDistrictVillageLayout.WaterLevel).Within(0.05f));
        }

        [Test]
        public void Rain_LandsOnTheBridge_AboveTheWater()
        {
            Assert.That(Village.HeightAt(0.2f, 0.3f), Is.GreaterThan(LakeDistrictVillageLayout.WaterLevel + 2f));
        }
    }

    public sealed class ChimneySmokeTests
    {
        [Test]
        public void MoreFires_BurnInTheEvening_ThanAtMidday()
        {
            var evening = Enumerable.Range(0, 400).Count(seed => ChimneySmoke.IsBurning(seed, 20f));
            var midday = Enumerable.Range(0, 400).Count(seed => ChimneySmoke.IsBurning(seed, 13f));

            Assert.That(evening, Is.GreaterThan(midday));
            Assert.That(midday, Is.GreaterThan(80), "some fires are always lit in a storm");
        }
    }
}

namespace Townscape.Tests.Simulation
{
    public sealed class WindDirectionTests
    {
        [TestCase(1f, 1f, "SW")]
        [TestCase(0f, 1f, "S")]
        [TestCase(-1f, 0f, "E")]
        [TestCase(0f, -1f, "N")]
        [TestCase(0f, 0f, "calm")]
        public void Winds_AreNamedAfterWhereTheyComeFrom(float x, float y, string expected)
        {
            Assert.That(WindDirection.From(new Vector2(x, y)), Is.EqualTo(expected));
        }
    }
}
