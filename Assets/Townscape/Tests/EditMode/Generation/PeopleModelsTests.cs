using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation.Geometry;
using Townscape.Generation.People;

namespace Townscape.Tests.Generation
{
    public sealed class PeopleModelsTests
    {
        private static (Vector3 Min, Vector3 Max) Bounds(params MeshData[] meshes)
        {
            var points = meshes.SelectMany(m => m.Positions).ToList();
            return (new Vector3(points.Min(p => p.X), points.Min(p => p.Y), points.Min(p => p.Z)), new Vector3(points.Max(p => p.X), points.Max(p => p.Y), points.Max(p => p.Z)));
        }

        [Test]
        public void EachFigure_StandsOnItsFeet_AtAboutAPersonsHeight()
        {
            foreach (FigureKind kind in Enum.GetValues(typeof(FigureKind)))
            {
                var model = PeopleModels.Figure(kind);
                var (min, max) = Bounds(PeopleModels.Posed(model, Vector3.Zero, Vector3.UnitZ, 0f, carrying: false).ToArray());

                Assert.That(min.Y, Is.EqualTo(0f).Within(0.005f), $"{kind}'s feet are on the ground");
                Assert.That(max.Y, Is.InRange(1.72f, 1.95f), $"{kind}'s height");
                Assert.That(max.X - min.X, Is.InRange(0.5f, 0.7f), $"{kind} is a person's width");
                foreach (var part in new[] { model.Body, model.LeftLeg, model.RightLeg, model.LeftArm, model.RightArm })
                {
                    Assert.That(part.TriangleCount, Is.GreaterThan(0));
                }

                Assert.That(model.Bag != null, Is.EqualTo(kind == FigureKind.Burglar), "only the burglar has a sack");
            }
        }

        [Test]
        public void TheGuardAndThePolice_WearHiVis_AndThePoliceTheirHelmets()
        {
            bool Wears(FigureKind kind, SurfaceMaterial material) =>
                PeopleModels.Figure(kind).Body.Submeshes.Any(s => s.Material == material);

            Assert.That(Wears(FigureKind.Guard, SurfaceMaterial.HiVis), Is.True);
            Assert.That(Wears(FigureKind.Police, SurfaceMaterial.HiVis), Is.True);
            Assert.That(Wears(FigureKind.Burglar, SurfaceMaterial.HiVis), Is.False);
            var police = Bounds(PeopleModels.Figure(FigureKind.Police).Body).Max.Y;
            var guard = Bounds(PeopleModels.Figure(FigureKind.Guard).Body).Max.Y;
            Assert.That(police, Is.GreaterThan(guard + 0.1f), "a custodian helmet stands tall");
        }

        [Test]
        public void Walking_SwingsTheLegsOneWay_AndTheArmsTheOther()
        {
            var model = PeopleModels.Figure(FigureKind.Guard);
            var posed = PeopleModels.Posed(model, Vector3.Zero, Vector3.UnitZ, 0.4f, carrying: false).ToArray();
            var leftFoot = posed[1].Positions.OrderBy(p => p.Y).First();
            var rightFoot = posed[2].Positions.OrderBy(p => p.Y).First();
            var leftHand = posed[3].Positions.OrderBy(p => p.Y).First();

            Assert.That(leftFoot.Z, Is.GreaterThan(0.2f), "the left foot forward");
            Assert.That(rightFoot.Z, Is.LessThan(-0.15f), "the right one back");
            Assert.That(leftHand.Z, Is.LessThan(-0.1f), "and the left hand back");
        }

        [Test]
        public void ThePoliceCar_IsCarSized_OnItsWheels_WithBlueLightsOnTheRoof()
        {
            var car = PeopleModels.PoliceCar();
            var (min, max) = Bounds(PeopleModels.Posed(car, Vector3.Zero, Vector3.UnitZ).ToArray());

            Assert.That(min.Y, Is.EqualTo(0f).Within(0.005f), "the tyres touch the road");
            Assert.That(max.Z - min.Z, Is.InRange(4.2f, 4.8f));
            Assert.That(max.X - min.X, Is.InRange(1.7f, 1.9f));
            Assert.That(car.Wheels, Has.Count.EqualTo(4));
            Assert.That(car.BlueLights, Has.Count.EqualTo(2));
            var roof = Bounds(car.Body).Max.Y;
            foreach (var (centre, half) in car.BlueLights)
            {
                Assert.That(centre.Y + half.Y, Is.EqualTo(roof).Within(0.01f), "the lenses are the top of the car");
            }

            Assert.That(car.Body.Submeshes.Any(s => s.Material == SurfaceMaterial.PaintBlue), Is.True, "Battenburg");
            Assert.That(car.Body.Submeshes.Any(s => s.Material == SurfaceMaterial.HiVis), Is.True);
        }
    }
}
