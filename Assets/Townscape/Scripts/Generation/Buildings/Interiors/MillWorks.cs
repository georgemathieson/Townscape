using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Interiors
{
    /// <summary>
    /// The old mill turned into Mill Works, a co-working office, with the village's alarm
    /// receiving centre upstairs. The ground floor has a reception desk by the door, a long bench
    /// of hot desks, a sofa and a kitchenette. The first floor is the alarm receiving centre: two
    /// rows of operators' desks facing a wall of screens. The second floor has a meeting room
    /// table and quiet desks by the windows. A dog-leg stair climbs a core at the right-hand end,
    /// behind a wall with a doorway on each floor (a door on the alarm receiving centre's).
    /// </summary>
    /// <remarks>
    /// Laid out in <see cref="UnitSpace"/> metres for a building about 12 m wide and 8 m deep,
    /// which is what <see cref="Fits"/> checks. The stone walls are <see cref="Shell"/> thick,
    /// every window and the door are cut right through them, and the floors are 3.1 m apart.
    /// </remarks>
    public static class MillWorks
    {
        /// <summary>The building's name, its burglar alarm's and its broadband customer's.</summary>
        public const string Name = "Mill Works";

        /// <summary>The alarm receiving centre upstairs.</summary>
        public const string CentreName = "Mill Works ARC";

        public const string DoorName = "Mill Works door";
        public const string CentreDoorName = "ARC door";

        public const float MinimumWidth = 11.5f;
        public const float MinimumDepth = 7.5f;
        public const float FloorHeight = 3.1f;
        public const int Bays = 5;

        /// <summary>How thick the stone walls are, and so how deep every window and the door are set.</summary>
        public const float Shell = 0.2f;

        private const float Thin = 0.1f;
        private const float CoreWidth = 2.2f;
        private const float FootD = 1.4f;
        private const float TurnD = 6.3f;
        private const int Steps = 18;
        private const float DoorwayHeight = 2.1f;
        private const float KeypadHeight = 1.5f;

        // The doorways through the core's wall, back from the front: at the front on the ground
        // and top floors, and at the back (with a door) into the alarm receiving centre.
        private static readonly (float From, float To) FrontDoorway = (0.35f, 1.3f);
        private static readonly (float From, float To) CentreDoorway = (6.45f, 7.4f);

        public static bool Fits(Footprint footprint) => footprint.FrontWidth >= MinimumWidth && footprint.Depth >= MinimumDepth;

        /// <summary>The ground, first and second floors.</summary>
        public static float[] Floors()
        {
            var ground = BuildingLevels.Floor;
            return new[] { ground, ground + FloorHeight, ground + (2f * FloorHeight) };
        }

        /// <summary>The top of the walls, where the roof starts.</summary>
        public static float Eaves => BuildingLevels.Floor + (3f * FloorHeight);

        /// <summary>The office side of the stair core's wall.</summary>
        public static float CoreX(UnitSpace space) => space.Width - Shell - CoreWidth;

        /// <summary>The two flights: up the right-hand wall to the back of the first floor, then forward to the front of the second.</summary>
        public static StairFlight[] Flights(UnitSpace space)
        {
            var f = Floors();
            var right = space.Width - Shell;
            var middle = Middle(space);
            return new[]
            {
                new StairFlight(middle, right, FootD, TurnD, f[0], f[1], Steps),
                new StairFlight(CoreX(space) + Thin, middle, TurnD, FootD, f[1], f[2], Steps),
            };
        }

        // ---- The shell's openings, in UnitSpace terms --------------------------------------------

        private static float BayCentre(UnitSpace space, int bay) => space.Width * (bay + 0.5f) / Bays;

        /// <summary>The front door, in the middle bay, across (x) and up.</summary>
        public static Hole Door(UnitSpace space)
        {
            var centre = BayCentre(space, Bays / 2);
            return new Hole(centre - 0.5f, BuildingLevels.Floor, centre + 0.5f, BuildingLevels.Floor + 2.3f);
        }

        /// <summary>The front windows (across and up): either side of the door, and five on each floor above.</summary>
        public static IEnumerable<Hole> FrontWindows(UnitSpace space)
        {
            var f = Floors();
            for (var bay = 0; bay < Bays; bay++)
            {
                var centre = BayCentre(space, bay);
                if (bay != Bays / 2)
                {
                    yield return new Hole(centre - 0.5f, f[0] + 0.85f, centre + 0.5f, f[0] + 2.3f);
                }

                yield return new Hole(centre - 0.45f, f[1] + 0.85f, centre + 0.45f, f[1] + 2.35f);
                yield return new Hole(centre - 0.45f, f[2] + 0.85f, centre + 0.45f, f[2] + 2.35f);
            }
        }

        /// <summary>The back windows (across and up), five on each floor, high enough for the kitchen counter.</summary>
        public static IEnumerable<Hole> BackWindows(UnitSpace space)
        {
            foreach (var floor in Floors())
            {
                for (var bay = 0; bay < Bays; bay++)
                {
                    var centre = BayCentre(space, bay);
                    yield return new Hole(centre - 0.45f, floor + 1.0f, centre + 0.45f, floor + 2.35f);
                }
            }
        }

        /// <summary>The left gable's windows (back and up): two on each floor, either side of the screens upstairs.</summary>
        public static IEnumerable<Hole> LeftWindows(UnitSpace space)
        {
            foreach (var floor in Floors())
            {
                foreach (var d in new[] { 1.6f, space.Depth - 1.6f })
                {
                    yield return new Hole(d - 0.45f, floor + 0.85f, d + 0.45f, floor + 2.35f);
                }
            }
        }

        /// <summary>The right gable's windows (back and up), lighting the stairs on the two upper floors.</summary>
        public static IEnumerable<Hole> RightWindows(UnitSpace space)
        {
            var f = Floors();
            var d = space.Depth * 0.5f;
            yield return new Hole(d - 0.45f, f[1] + 0.85f, d + 0.45f, f[1] + 2.35f);
            yield return new Hole(d - 0.45f, f[2] + 0.85f, d + 0.45f, f[2] + 2.35f);
        }

        /// <summary>
        /// The ways through the building that furniture must keep clear, in <see cref="UnitSpace"/>
        /// metres on the floor at <c>Floor</c>.
        /// </summary>
        public static IEnumerable<(string Name, float Floor, float X0, float D0, float X1, float D1)> Walkways(Footprint footprint)
        {
            var space = new UnitSpace(footprint);
            var f = Floors();
            var door = Door(space);
            var core = CoreX(space);
            var middle = Middle(space);
            var right = space.Width - Shell;
            yield return ("in from the front door", f[0], door.From, Shell, door.To, Shell + 1.3f);
            yield return ("across to the stairs", f[0], door.From, FrontDoorway.From, core, FrontDoorway.To);
            yield return ("into the stair core", f[0], core - 0.9f, FrontDoorway.From, middle, FrontDoorway.To);
            yield return ("to the foot of the stairs", f[0], middle, Shell + 0.12f, right, FootD - 0.05f);
            yield return ("past the comms wall", f[0], core + Thin + 0.12f, FootD, middle, TurnD - 0.1f);
            yield return ("from the landing into the alarm receiving centre", f[1], core - 0.9f, CentreDoorway.From, middle, CentreDoorway.To);
            yield return ("along the back of the alarm receiving centre", f[1], Shell + 0.8f, 6.75f, core, space.Depth - Shell - 0.1f);
            yield return ("off the top of the stairs", f[2], core + Thin, Shell + 0.12f, middle, FootD - 0.1f);
            yield return ("onto the top floor", f[2], core - 0.9f, FrontDoorway.From, core + Thin, FrontDoorway.To);
        }

        // ---- Building it --------------------------------------------------------------------------

        public static void Build(BuildContext context, Footprint footprint)
        {
            var space = new UnitSpace(footprint);
            var builder = context.Builder;
            var f = Floors();
            var flights = Flights(space);
            var left = Shell;
            var right = space.Width - Shell;
            var back = space.Depth - Shell;
            var ceiling = Eaves - 0.05f;
            var plaster = SurfaceMaterial.Interior;

            // Floors: boards on the ground, and two upper floors, each with a hole for its flight.
            CafeAndFlat.Floor(space, builder, f[0], left, Shell, right, back, null, SurfaceMaterial.InteriorFloor);
            CafeAndFlat.Slabs(space, builder, f[1], left, Shell, right, back, Hole(flights[0]));
            CafeAndFlat.Slabs(space, builder, f[2], left, Shell, right, back, Hole(flights[1]));
            space.Ceiling(builder, left, Shell, right, back, ceiling, plaster);

            // The inside of the stone walls, with every window and the door cut out.
            var front = FrontWindows(space).Append(Door(space)).ToArray();
            space.WallAcross(builder, Shell, left, right, f[0], ceiling, true, plaster, front);
            space.WallAcross(builder, back, left, right, f[0], ceiling, false, plaster, BackWindows(space).ToArray());
            space.WallAlong(builder, left, Shell, back, f[0], ceiling, true, plaster, LeftWindows(space).ToArray());
            space.WallAlong(builder, right, Shell, back, f[0], ceiling, false, plaster, RightWindows(space).ToArray());
            foreach (var window in FrontWindows(space))
            {
                CafeAndFlat.WindowBoard(space, builder, window, Shell, 1f);
            }

            foreach (var window in BackWindows(space))
            {
                CafeAndFlat.WindowBoard(space, builder, window, back, -1f);
            }

            foreach (var window in LeftWindows(space))
            {
                Board(space, builder, window, left, 1f);
            }

            foreach (var window in RightWindows(space))
            {
                Board(space, builder, window, right, -1f);
            }

            Core(context, space, flights, f, ceiling);
            GroundFloor(context, space, f[0], f[1] - CafeAndFlat.Slab);
            var centre = CentreFloor(context, space, f[1], f[2] - CafeAndFlat.Slab);
            TopFloor(context, space, f[2], ceiling);
            Alarm(context, space, footprint, f, ceiling);
            context.AlarmCentres.Add(centre);
        }

        private static float Middle(UnitSpace space) => CoreX(space) + Thin + ((CoreWidth - Thin) * 0.5f);

        private static (float X0, float D0, float X1, float D1) Hole(StairFlight flight) => (flight.X0, flight.NearD, flight.X1, flight.FarD);

        // A wooden board along the bottom of a window in a side wall at x, standing into the room.
        private static void Board(UnitSpace space, MeshBuilder builder, Hole window, float wallX, float into)
        {
            var x0 = wallX - (0.012f * into);
            var x1 = wallX + (0.1f * into);
            space.Box(builder, Math.Min(x0, x1), window.Bottom - 0.035f, window.From - 0.06f, Math.Max(x0, x1), window.Bottom, window.To + 0.06f, SurfaceMaterial.Timber);
        }

        // ---- The stair core -------------------------------------------------------------------

        private static void Core(BuildContext context, UnitSpace space, StairFlight[] flights, float[] f, float ceiling)
        {
            var builder = context.Builder;
            var core = CoreX(space);
            var inside = core + Thin;
            var middle = Middle(space);
            var plaster = SurfaceMaterial.Interior;
            var doorways = new[]
            {
                new Hole(FrontDoorway.From, f[0], FrontDoorway.To, f[0] + DoorwayHeight),
                new Hole(CentreDoorway.From, f[1], CentreDoorway.To, f[1] + DoorwayHeight),
                new Hole(FrontDoorway.From, f[2], FrontDoorway.To, f[2] + DoorwayHeight),
            };

            space.WallAlong(builder, core, Shell, space.Depth - Shell, f[0], ceiling, false, plaster, doorways);
            space.WallAlong(builder, inside, Shell, space.Depth - Shell, f[0], ceiling, true, plaster, doorways);
            foreach (var doorway in doorways)
            {
                space.Ceiling(builder, core, doorway.From, inside, doorway.To, doorway.Top, plaster);
                space.WallAcross(builder, doorway.From, core, inside, doorway.Bottom, doorway.Top, true, plaster);
                space.WallAcross(builder, doorway.To, core, inside, doorway.Bottom, doorway.Top, false, plaster);
            }

            foreach (var flight in flights)
            {
                flight.Build(space, builder, SurfaceMaterial.Timber, SurfaceMaterial.PaintWhite);
            }

            // Banisters up the open side of each flight (the first over the ground floor beside
            // it, the second over the first's well): a slim black post on each step and a wooden
            // rail along the top.
            Banister(space, builder, flights[0], middle + 0.06f);
            Banister(space, builder, flights[1], middle);

            // Round the well on the top floor, and in front of the first flight's well on the first.
            CafeAndFlat.Rail(space, builder, middle, FootD, middle, TurnD, f[2]);
            CafeAndFlat.Rail(space, builder, inside, TurnD, middle, TurnD, f[2]);
            CafeAndFlat.Rail(space, builder, middle, FootD, space.Width - Shell, FootD, f[1]);

            // A light on each landing, on all night like the alarm receiving centre's.
            PanelLight(context, space, (inside + middle) * 0.5f, f[1] - CafeAndFlat.Slab, 0.8f, AnchorKind.OfficeLight);
            PanelLight(context, space, middle, f[2] - CafeAndFlat.Slab, (TurnD + space.Depth) * 0.5f, AnchorKind.OfficeLight);
            PanelLight(context, space, (inside + middle) * 0.5f, ceiling, 0.8f, AnchorKind.OfficeLight);

            // The door into the alarm receiving centre, opening into it.
            var hinge = space.At(core - 0.02f, f[1], CentreDoorway.From);
            var width = CentreDoorway.To - CentreDoorway.From;
            context.Door(CentreDoorName, hinge, space.Back, -space.Right, width, DoorwayHeight, door =>
            {
                var b = door.Builder;
                space.Box(b, core - 0.04f, f[1], CentreDoorway.From, core, f[1] + DoorwayHeight, CentreDoorway.To, SurfaceMaterial.PaintWhite);
                space.Box(b, core - 0.045f, f[1] + 1.2f, CentreDoorway.From + 0.3f, core + 0.005f, f[1] + 1.8f, CentreDoorway.From + 0.5f, SurfaceMaterial.ClearGlass);
                space.Box(b, core - 0.1f, f[1] + 0.95f, CentreDoorway.To - 0.16f, core - 0.04f, f[1] + 1.0f, CentreDoorway.To - 0.06f, SurfaceMaterial.Chrome);
                space.Box(b, core, f[1] + 0.95f, CentreDoorway.To - 0.16f, core + 0.06f, f[1] + 1.0f, CentreDoorway.To - 0.06f, SurfaceMaterial.Chrome);
            });

            // A sign on the door's side of the wall.
            Sign(space, builder, core - 0.005f, -1f, (CentreDoorway.From + CentreDoorway.To) * 0.5f, f[1] + DoorwayHeight + 0.3f, 0.9f, "ARC");
        }

        private static void Banister(UnitSpace space, MeshBuilder builder, StairFlight flight, float x)
        {
            var direction = Math.Sign(flight.EndD - flight.StartD);
            for (var i = 0; i < flight.Steps; i++)
            {
                var d0 = flight.StartD + (direction * i * flight.Going);
                var d1 = d0 + (direction * flight.Going);
                var step = flight.FromY + ((i + 1) * flight.Rise);
                var mid = (d0 + d1) * 0.5f;
                space.Box(builder, x - 0.018f, step, mid - 0.018f, x + 0.018f, step + 0.88f, mid + 0.018f, SurfaceMaterial.PaintBlack);
                space.Box(builder, x - 0.035f, step + 0.86f, Math.Min(d0, d1), x + 0.035f, step + 0.92f, Math.Max(d0, d1), SurfaceMaterial.Timber);
            }
        }

        // ---- Ground floor ---------------------------------------------------------------------

        private static void GroundFloor(BuildContext context, UnitSpace space, float floor, float top)
        {
            var builder = context.Builder;
            var core = CoreX(space);
            var back = space.Depth - Shell;

            // Reception: a counter facing the door, with its screen and chair.
            space.Box(builder, 6.9f, floor, 1.6f, 8.6f, floor + 1.02f, 1.68f, SurfaceMaterial.Timber);
            space.Box(builder, 6.9f, floor + 1.02f, 1.55f, 8.6f, floor + 1.06f, 1.75f, SurfaceMaterial.PaintWhite);
            Desk(space, builder, 6.9f, 1.68f, 8.6f, 2.25f, floor, SurfaceMaterial.PaintWhite);
            Monitor(space, builder, 7.75f, floor + 0.74f, 1.82f, 0f, 1f, 0.55f);
            CafeAndFlat.Chair(space, builder, 7.75f, floor, 2.75f, 0f, -1f, SurfaceMaterial.PaintBlack);

            // The hot desks: one long bench, three places a side, back to back screens.
            const float benchX0 = 2.4f;
            const float benchX1 = 3.2f;
            Desk(space, builder, benchX0, 1.6f, benchX1, 5.2f, floor, SurfaceMaterial.PaintWhite);
            foreach (var d in new[] { 2.2f, 3.4f, 4.6f })
            {
                Monitor(space, builder, 2.76f, floor + 0.74f, d, -1f, 0f, 0.55f);
                Monitor(space, builder, 2.84f, floor + 0.74f, d, 1f, 0f, 0.55f);
                CafeAndFlat.Chair(space, builder, 1.85f, floor, d, 1f, 0f, SurfaceMaterial.FabricNavy);
                CafeAndFlat.Chair(space, builder, 3.75f, floor, d, -1f, 0f, SurfaceMaterial.FabricNavy);
            }

            // A sofa and armchair round a low table.
            CafeAndFlat.Sofa(space, builder, 5.4f, 7.4f, floor, 4.6f, 5.5f, SurfaceMaterial.FabricRust);
            space.Box(builder, 5.9f, floor, 3.6f, 6.9f, floor + 0.4f, 4.1f, SurfaceMaterial.Timber);
            CafeAndFlat.Armchair(space, builder, 8.5f, floor, 3.9f, SurfaceMaterial.FabricSage);

            // The kitchenette along the back: a counter with a sink and a coffee machine, a fridge,
            // and a table for two.
            space.Box(builder, 0.4f, floor, back - 0.6f, 3.6f, floor + 0.88f, back, SurfaceMaterial.Timber);
            space.Box(builder, 0.38f, floor + 0.88f, back - 0.62f, 3.62f, floor + 0.92f, back, SurfaceMaterial.PaintWhite);
            space.Box(builder, 1.0f, floor + 0.9f, back - 0.5f, 1.5f, floor + 0.925f, back - 0.15f, SurfaceMaterial.Chrome);
            space.Box(builder, 2.6f, floor + 0.92f, back - 0.5f, 3.0f, floor + 1.35f, back - 0.1f, SurfaceMaterial.Chrome);
            space.Box(builder, 2.65f, floor + 1.0f, back - 0.52f, 2.95f, floor + 1.12f, back - 0.5f, SurfaceMaterial.PaintBlack);
            space.Box(builder, 3.7f, floor, back - 0.65f, 4.35f, floor + 1.85f, back, SurfaceMaterial.Chrome);
            CafeAndFlat.Table(space, builder, 5.0f, floor, 6.6f, 0.45f);
            CafeAndFlat.Chair(space, builder, 4.3f, floor, 6.6f, 1f, 0f, SurfaceMaterial.Timber);
            CafeAndFlat.Chair(space, builder, 5.7f, floor, 6.6f, -1f, 0f, SurfaceMaterial.Timber);

            Plant(context, space, 0.65f, floor, 0.75f);
            Plant(context, space, core - 0.5f, floor, back - 0.5f);

            foreach (var (x, d) in new[] { (2.8f, 3.4f), (6.2f, 2.6f), (6.2f, 5.8f), (2.4f, 6.4f) })
            {
                PanelLight(context, space, x, top, d, AnchorKind.RoomLight);
            }
        }

        // ---- First floor: the alarm receiving centre ------------------------------------------

        private static TownAlarmCentre CentreFloor(BuildContext context, UnitSpace space, float floor, float top)
        {
            var builder = context.Builder;
            var left = Shell;
            var core = CoreX(space);
            var mid = space.Depth * 0.5f;

            // The wall of screens: two rows of three on the left wall, one for each site.
            var screens = new List<WallMount>();
            space.Box(builder, left, floor + 1.08f, mid - 1.75f, left + 0.05f, floor + 2.47f, mid + 1.75f, SurfaceMaterial.PaintBlack);
            foreach (var y in new[] { floor + 2.12f, floor + 1.45f })
            {
                for (var column = -1; column <= 1; column++)
                {
                    var d = mid + (column * 1.15f);
                    space.Box(builder, left + 0.05f, y - 0.3f, d - 0.54f, left + 0.052f, y + 0.3f, d + 0.54f, SurfaceMaterial.Screen);
                    screens.Add(new WallMount(space.At(left + 0.053f, y, d), space.Right, 1.08f, 0.6f));
                }
            }

            Sign(space, builder, left + 0.05f, 1f, mid, floor + 2.68f, 3.2f, "ALARM RECEIVING CENTRE");

            // Two rows of three operators' desks facing the screens, each with two monitors,
            // and the supervisor's desk at the side.
            var consoles = new List<WallMount>();
            foreach (var row in new[] { 3.0f, 5.6f })
            {
                foreach (var d in new[] { 2.25f, 4.05f, 5.85f })
                {
                    consoles.Add(Operator(space, builder, row, d, floor));
                }
            }

            consoles.Add(Operator(space, builder, 8.0f, 3.35f, floor));

            Plant(context, space, core - 0.45f, floor, Shell + 0.5f);
            foreach (var (x, d) in new[] { (2.2f, 2.5f), (2.2f, 5.5f), (6.4f, 2.5f), (6.4f, 5.5f) })
            {
                PanelLight(context, space, x, top, d, AnchorKind.OfficeLight);
            }

            return new TownAlarmCentre(CentreName, consoles, screens, space.At((left + core) * 0.5f, floor + 1.6f, mid));
        }

        // A desk facing the screens (towards -x) at x0, its two monitors at the front, a phone and
        // keyboard, and the chair behind it. Returns the monitors' faces, the console.
        private static WallMount Operator(UnitSpace space, MeshBuilder builder, float x0, float d, float floor)
        {
            var x1 = x0 + 0.75f;
            Desk(space, builder, x0, d - 0.75f, x1, d + 0.75f, floor, SurfaceMaterial.PaintWhite);
            space.Box(builder, x0, floor + 0.1f, d - 0.72f, x0 + 0.02f, floor + 0.7f, d + 0.72f, SurfaceMaterial.PaintWhite);
            Monitor(space, builder, x0 + 0.16f, floor + 0.74f, d - 0.31f, 1f, 0f, 0.58f);
            Monitor(space, builder, x0 + 0.16f, floor + 0.74f, d + 0.31f, 1f, 0f, 0.58f);
            space.Box(builder, x0 + 0.38f, floor + 0.74f, d - 0.22f, x0 + 0.52f, floor + 0.755f, d + 0.22f, SurfaceMaterial.PaintBlack);
            space.Box(builder, x0 + 0.3f, floor + 0.74f, d + 0.48f, x0 + 0.48f, floor + 0.8f, d + 0.66f, SurfaceMaterial.PaintBlack);
            CafeAndFlat.Chair(space, builder, x1 + 0.5f, floor, d, -1f, 0f, SurfaceMaterial.PaintBlack);
            return new WallMount(space.At(x0 + 0.2f, floor + 1.13f, d), space.Right, 1.24f, 0.38f);
        }

        // ---- Second floor ---------------------------------------------------------------------

        private static void TopFloor(BuildContext context, UnitSpace space, float floor, float top)
        {
            var builder = context.Builder;
            var core = CoreX(space);
            var back = space.Depth - Shell;
            var mid = space.Depth * 0.5f;

            // The meeting table, its chairs and the screen on the end wall.
            space.Box(builder, 2.0f, floor + 0.72f, mid - 0.8f, 5.0f, floor + 0.76f, mid + 0.8f, SurfaceMaterial.Timber);
            foreach (var x in new[] { 2.6f, 4.4f })
            {
                space.Box(builder, x - 0.06f, floor, mid - 0.5f, x + 0.06f, floor + 0.72f, mid + 0.5f, SurfaceMaterial.Iron);
            }

            foreach (var x in new[] { 2.6f, 3.5f, 4.4f })
            {
                CafeAndFlat.Chair(space, builder, x, floor, mid - 1.3f, 0f, 1f, SurfaceMaterial.FabricSage);
                CafeAndFlat.Chair(space, builder, x, floor, mid + 1.3f, 0f, -1f, SurfaceMaterial.FabricSage);
            }

            CafeAndFlat.Chair(space, builder, 1.45f, floor, mid, 1f, 0f, SurfaceMaterial.FabricSage);
            CafeAndFlat.Chair(space, builder, 5.55f, floor, mid, -1f, 0f, SurfaceMaterial.FabricSage);
            space.Box(builder, Shell, floor + 0.95f, mid - 0.65f, Shell + 0.05f, floor + 1.75f, mid + 0.65f, SurfaceMaterial.PaintBlack);
            space.Box(builder, Shell + 0.05f, floor + 0.99f, mid - 0.61f, Shell + 0.052f, floor + 1.71f, mid + 0.61f, SurfaceMaterial.Screen);

            // Quiet desks under the first two front windows.
            foreach (var bay in new[] { 0, 1 })
            {
                var x = BayCentre(space, bay);
                Desk(space, builder, x - 0.6f, Shell, x + 0.6f, 0.85f, floor, SurfaceMaterial.Timber);
                Monitor(space, builder, x, floor + 0.74f, Shell + 0.22f, 0f, 1f, 0.55f);
                CafeAndFlat.Chair(space, builder, x, floor, 1.35f, 0f, -1f, SurfaceMaterial.FabricNavy);
            }

            // A sofa at the back, a low table and an armchair.
            CafeAndFlat.Sofa(space, builder, 6.2f, 8.4f, floor, back - 0.85f, back - 0.05f, SurfaceMaterial.FabricNavy);
            space.Box(builder, 6.8f, floor, back - 1.9f, 7.8f, floor + 0.4f, back - 1.4f, SurfaceMaterial.Timber);
            CafeAndFlat.Armchair(space, builder, core - 0.6f, floor, back - 1.7f, SurfaceMaterial.FabricRust);
            Plant(context, space, 0.65f, floor, back - 0.5f);

            foreach (var (x, d) in new[] { (3.5f, 2.2f), (3.5f, 5.8f), (7.2f, 4.0f) })
            {
                PanelLight(context, space, x, top, d, AnchorKind.RoomLight);
            }
        }

        // ---- The burglar alarm and the broadband ---------------------------------------------

        // Its own alarm: a contact on the front door and a sensor on each floor (and the stairs),
        // a keypad inside the door, the control box and the fibre kit on the stair core's wall,
        // and a yellow bell box on the front.
        private static void Alarm(BuildContext context, UnitSpace space, Footprint footprint, float[] f, float ceiling)
        {
            var door = context.Doors.LastOrDefault(d => d.Name == DoorName);
            if (door == null)
            {
                return;
            }

            var core = CoreX(space);
            var inside = core + Thin;
            var right = space.Width - Shell;
            var back = space.Depth - Shell;
            var zones = new List<AlarmZone>
            {
                AlarmFittings.DoorContact("Front door", door),
                AlarmFittings.CornerSensor(context, space, "Reception sensor", core, f[1] - CafeAndFlat.Slab, Shell, -1f, 1f),
                AlarmFittings.CornerSensor(context, space, "Kitchen sensor", Shell, f[1] - CafeAndFlat.Slab, back, 1f, -1f),
                AlarmFittings.CornerSensor(context, space, "Stairs sensor", right, f[1] - CafeAndFlat.Slab, back, -1f, -1f),
                AlarmFittings.CornerSensor(context, space, "ARC sensor", Shell, f[2] - CafeAndFlat.Slab, back, 1f, -1f),
                AlarmFittings.CornerSensor(context, space, "Meeting room sensor", core, ceiling, back, -1f, -1f),
            };

            // The keypad is on the inside of the front wall, left of the door: on the building
            // turned a quarter, that wall runs back like the walls keypads usually go on.
            var turned = new UnitSpace(new Footprint(footprint.BackLeft, footprint.FrontLeft, footprint.FrontRight, footprint.BackRight));
            var keypad = AlarmFittings.Keypad(context, turned, space.Depth - Shell, -1f, Door(space).From - 0.4f, f[0] + KeypadHeight);

            var y = f[0] + 1.55f;
            var box = AlarmFittings.ControlBox(context, space, inside, 1f, 5.6f, y);
            var broadband = BroadbandFittings.Fit(context, space, Name, 1000f, inside, 1f, f[0], y, 4.3f, 4.7f);
            var routerEdge = 4.7f + (BroadbandFittings.RouterWidth * 0.5f);
            space.Box(context.Builder, inside, broadband.Router.Position.Y - 0.054f, routerEdge, inside + 0.012f, broadband.Router.Position.Y - 0.046f, 5.6f - (AlarmFittings.ControlBoxSize * 0.5f), SurfaceMaterial.PaintBlue);

            var front = footprint.FrontWall;
            var windowTop = f[2] + 2.35f;
            var strobe = AlarmFittings.BellBox(context, front, space.Width * 0.8f, windowTop - 0.02f, 0f, SurfaceMaterial.PaintButter);
            context.Alarms.Add(new TownAlarm(Name, zones, new[] { keypad }, box, strobe));
        }

        // ---- Furniture ------------------------------------------------------------------------

        // A desk: a top at 0.74 m on four legs.
        private static void Desk(UnitSpace space, MeshBuilder builder, float x0, float d0, float x1, float d1, float floor, SurfaceMaterial top)
        {
            space.Box(builder, x0, floor + 0.71f, d0, x1, floor + 0.74f, d1, top);
            foreach (var (x, d) in new[] { (x0 + 0.05f, d0 + 0.05f), (x1 - 0.05f, d0 + 0.05f), (x0 + 0.05f, d1 - 0.05f), (x1 - 0.05f, d1 - 0.05f) })
            {
                space.Box(builder, x - 0.025f, floor, d - 0.025f, x + 0.025f, floor + 0.71f, d + 0.025f, SurfaceMaterial.Iron);
            }
        }

        // A monitor on a stand on a desk at height y, its screen facing (faceX, faceD).
        private static void Monitor(UnitSpace space, MeshBuilder builder, float x, float y, float d, float faceX, float faceD, float width)
        {
            var across = Math.Abs(faceX) > 0.5f;
            var halfX = across ? 0.015f : width * 0.5f;
            var halfD = across ? width * 0.5f : 0.015f;
            var baseX = across ? 0.035f : 0.09f;
            var baseD = across ? 0.09f : 0.035f;
            space.Box(builder, x - baseX, y, d - baseD, x + baseX, y + 0.012f, d + baseD, SurfaceMaterial.PaintBlack);
            space.Box(builder, x - 0.015f, y, d - 0.015f, x + 0.015f, y + 0.2f, d + 0.015f, SurfaceMaterial.PaintBlack);
            space.Box(builder, x - halfX, y + 0.2f, d - halfD, x + halfX, y + 0.56f, d + halfD, SurfaceMaterial.PaintBlack);
            var fx = x + (faceX * 0.016f);
            var fd = d + (faceD * 0.016f);
            var sx = across ? 0.001f : halfX - 0.015f;
            var sd = across ? halfD - 0.015f : 0.001f;
            space.Box(builder, fx - sx, y + 0.215f, fd - sd, fx + sx, y + 0.545f, fd + sd, SurfaceMaterial.Screen);
        }

        // A potted plant.
        private static void Plant(BuildContext context, UnitSpace space, float x, float floor, float d)
        {
            space.Round(context.Builder, x, floor, d, 0.22f, 0.45f, SurfaceMaterial.PaintOrange, 10);
            context.Builder.AddBlob(space.At(x, floor + 0.85f, d), new Vector3(0.32f, 0.45f, 0.32f), SurfaceMaterial.LeafDark, context.Random, 0.2f);
        }

        // A square panel light flush with the ceiling, in the fittings mesh.
        private static void PanelLight(BuildContext context, UnitSpace space, float x, float ceiling, float d, AnchorKind kind)
        {
            var builder = context.Fittings;
            space.Box(builder, x - 0.32f, ceiling - 0.03f, d - 0.32f, x + 0.32f, ceiling, d + 0.32f, SurfaceMaterial.PaintWhite);
            space.Ceiling(builder, x - 0.28f, d - 0.28f, x + 0.28f, d + 0.28f, ceiling - 0.032f, SurfaceMaterial.LampGlass);
            context.Anchor(kind, space.At(x, ceiling - 0.05f, d), -Vector3.UnitY, 3f);
        }

        // A dark board with cream lettering on a wall running back at x, facing out along +x
        // (facing 1) or -x (facing -1), centred at depth d and height y.
        private static void Sign(UnitSpace space, MeshBuilder builder, float x, float facing, float d, float y, float width, string text)
        {
            var x1 = x + (facing * 0.03f);
            space.Box(builder, Math.Min(x, x1), y - 0.13f, d - (width * 0.5f), Math.Max(x, x1), y + 0.13f, d + (width * 0.5f), SurfaceMaterial.PaintBlack);
            var a = space.At(x1, 0f, d - (width * 0.5f));
            var b = space.At(x1, 0f, d + (width * 0.5f));
            var wall = WallFrame.FromBase(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));
            if (Vector3.Dot(wall.Out, space.Right * facing) < 0f)
            {
                wall = WallFrame.FromBase(new Vector2(b.X, b.Z), new Vector2(a.X, a.Z));
            }

            PixelFont.Write(builder, wall, text, width * 0.5f, y, 0.03f, width - 0.15f, 0.003f, SurfaceMaterial.PaintCream);
        }
    }
}
