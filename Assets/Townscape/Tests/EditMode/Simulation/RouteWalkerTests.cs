using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Townscape.Generation;
using Townscape.Generation.Routes;
using Townscape.Simulation.People;

namespace Townscape.Tests.Simulation
{
    public sealed class RouteWalkerTests
    {
        private static RoutePath Path(params RouteStop[] stops) => new RoutePath(stops);

        private static void Run(RouteWalker walker, IDoors doors, float seconds)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += 0.05f)
            {
                walker.Tick(0.05f, doors);
            }
        }

        [Test]
        public void ItWalksTheWay_AtItsPace()
        {
            var walker = new RouteWalker(Vector3.Zero, 2f);
            walker.Follow(Path(new RouteStop(Vector3.Zero), new RouteStop(new Vector3(4f, 0f, 0f)), new RouteStop(new Vector3(4f, 0f, 4f))));

            Assert.That(walker.Remaining, Is.EqualTo(8f).Within(1e-4f));
            Run(walker, null, 3f);
            Assert.That(Vector3.Distance(walker.Position, new Vector3(4f, 0f, 2f)), Is.LessThan(0.01f));
            Assert.That(walker.Facing.Z, Is.EqualTo(1f).Within(1e-4f));
            Run(walker, null, 1.1f);
            Assert.That(walker.Arrived, Is.True);
            Assert.That(walker.Walked, Is.EqualTo(8f).Within(0.01f));
        }

        [Test]
        public void AtAShutDoor_ItOpensIt_WaitsForIt_AndShutsItBehind()
        {
            var doors = new Doors();
            var walker = new RouteWalker(Vector3.Zero, 1f) { ShutsDoors = true };
            walker.Follow(Path(new RouteStop(Vector3.Zero), new RouteStop(new Vector3(2f, 0f, 0f), door: "Shop door"), new RouteStop(new Vector3(3f, 0f, 0f))));

            Run(walker, doors, RouteWalker.DoorSeconds - 0.1f);
            Assert.That(doors.IsOpen("Shop door"), Is.True);
            Assert.That(walker.Position.X, Is.EqualTo(0f), "waiting for it to swing open");

            Run(walker, doors, 2.2f);
            Assert.That(walker.Position.X, Is.GreaterThan(2f));
            Assert.That(doors.IsOpen("Shop door"), Is.False, "shut behind them");
            Assert.That(doors.Opened, Is.EqualTo(1));
        }

        [Test]
        public void ADoorAlreadyOpen_IsLeftOpen_BySomeoneWhoDoesntShutDoors()
        {
            var doors = new Doors();
            doors.Open("Flat door");
            var walker = new RouteWalker(Vector3.Zero, 1f);
            walker.Follow(Path(new RouteStop(Vector3.Zero), new RouteStop(new Vector3(1f, 0f, 0f), door: "Flat door")));

            Run(walker, doors, 1.1f);
            Assert.That(walker.Arrived, Is.True, "no waiting for an open door");
            Assert.That(doors.IsOpen("Flat door"), Is.True);
        }

        [Test]
        public void HeldUp_ItStandsStill()
        {
            var walker = new RouteWalker(Vector3.Zero, 1f);
            walker.Follow(Path(new RouteStop(Vector3.Zero), new RouteStop(new Vector3(5f, 0f, 0f))));
            walker.Held = true;

            Run(walker, null, 2f);
            Assert.That(walker.Position, Is.EqualTo(Vector3.Zero));
            Assert.That(walker.Moving, Is.False);
        }

        [Test]
        public void ACar_PicksUpSpeed_AndSlowsToAStopAtTheEnd()
        {
            var car = new RouteWalker(Vector3.Zero, 13f) { Acceleration = 3.5f };
            car.Follow(Path(new RouteStop(Vector3.Zero), new RouteStop(new Vector3(100f, 0f, 0f))));

            Run(car, null, 1f);
            Assert.That(car.CurrentSpeed, Is.InRange(3f, 4f));
            Run(car, null, 6f);
            Assert.That(car.CurrentSpeed, Is.EqualTo(13f).Within(0.01f));
            var fastest = 0f;
            while (!car.Arrived)
            {
                car.Tick(0.05f, null);
                if (car.Remaining < 3f)
                {
                    fastest = System.Math.Max(fastest, car.CurrentSpeed);
                }
            }

            Assert.That(fastest, Is.LessThan(5.5f), "slowing in the last few metres");
            Assert.That(car.Position.X, Is.EqualTo(100f).Within(1e-3f));
        }

        private sealed class Doors : IDoors
        {
            private readonly HashSet<string> _open = new HashSet<string>();

            public int Opened { get; private set; }

            public bool IsOpen(string door) => _open.Contains(door);

            public void Open(string door)
            {
                if (_open.Add(door))
                {
                    Opened++;
                }
            }

            public void Shut(string door) => _open.Remove(door);
        }
    }
}
