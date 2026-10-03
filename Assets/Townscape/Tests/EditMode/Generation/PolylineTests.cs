using System.Numerics;
using NUnit.Framework;
using Townscape.Generation.Maths;

namespace Townscape.Tests.Generation
{
    public sealed class PolylineTests
    {
        private static readonly Polyline LShape = new Polyline(new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 10f));

        [Test]
        public void Length_SumsSegments()
        {
            Assert.That(LShape.Length, Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void Closest_ProjectsOntoNearestSegment()
        {
            var hit = LShape.Closest(new Vector2(12f, 4f));

            Assert.That(hit.Point.X, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(hit.Point.Y, Is.EqualTo(4f).Within(1e-4f));
            Assert.That(hit.Distance, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(hit.Along, Is.EqualTo(14f).Within(1e-4f));
            Assert.That(hit.Tangent.Y, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void PointAt_ClampsToEnds()
        {
            Assert.That(LShape.PointAt(-5f), Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(LShape.PointAt(15f), Is.EqualTo(new Vector2(10f, 5f)));
            Assert.That(LShape.PointAt(99f), Is.EqualTo(new Vector2(10f, 10f)));
        }

        [Test]
        public void DuplicatePoints_AreIgnored()
        {
            var line = new Polyline(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(3f, 4f));

            Assert.That(line.Points, Has.Count.EqualTo(2));
            Assert.That(line.Length, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void Smooth_PassesThroughControlPoints()
        {
            var controls = new[] { new Vector2(0f, 0f), new Vector2(10f, 5f), new Vector2(20f, 0f), new Vector2(30f, 8f) };

            var smooth = Polyline.Smooth(controls);

            foreach (var control in controls)
            {
                Assert.That(smooth.Closest(control).Distance, Is.LessThan(1e-3f));
            }

            Assert.That(smooth.Points.Count, Is.GreaterThan(controls.Length * 4));
        }

        [Test]
        public void Smooth_KeepsCollinearStretchesStraight()
        {
            var smooth = Polyline.Smooth(new[]
            {
                new Vector2(-40f, 10f), new Vector2(-20f, 0f), new Vector2(-10f, 0f), new Vector2(10f, 0f), new Vector2(20f, 0f), new Vector2(40f, -10f),
            });

            for (var x = -10f; x <= 10f; x += 0.5f)
            {
                Assert.That(smooth.Closest(new Vector2(x, 0f)).Distance, Is.LessThan(1e-3f), $"x={x}");
            }
        }
    }
}
