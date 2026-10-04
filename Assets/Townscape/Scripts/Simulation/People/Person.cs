using System.Numerics;

namespace Townscape.Simulation.People
{
    public enum PersonRole
    {
        /// <summary>The alarm receiving centre's key-holding guard, in a hi-vis jacket.</summary>
        Guard,

        /// <summary>A police officer.</summary>
        Police,

        /// <summary>Someone breaking in.</summary>
        Burglar,
    }

    /// <summary>Someone walking about the town.</summary>
    public sealed class Person
    {
        /// <summary>Where their eyes are, above their feet.</summary>
        public const float EyeHeight = 1.6f;

        /// <summary>The middle of them, above their feet: what a sensor or a guard sees.</summary>
        public const float ChestHeight = 1.2f;

        public Person(PersonRole role, string name, Vector3 position, float speed)
        {
            Role = role;
            Name = name;
            Walker = new RouteWalker(position, speed);
        }

        public PersonRole Role { get; }

        public string Name { get; }

        public RouteWalker Walker { get; }

        public Vector3 Position => Walker.Position;

        public Vector3 Eye => Walker.Position + new Vector3(0f, EyeHeight, 0f);

        public Vector3 Chest => Walker.Position + new Vector3(0f, ChestHeight, 0f);

        /// <summary>Out and about, rather than sitting in a car or gone.</summary>
        public bool Visible { get; set; } = true;

        /// <summary>What they're up to, for the control panel: "forcing the door of The Copper Kettle".</summary>
        public string Doing { get; set; } = string.Empty;

        /// <summary>A burglar with a bag of what they've taken.</summary>
        public bool Carrying { get; set; }

        /// <summary>Someone being taken away by the police.</summary>
        public bool Arrested { get; set; }
    }

    /// <summary>The police car: where it is, and whether its blue lights and siren are on.</summary>
    public sealed class PoliceCar
    {
        /// <summary>About thirty miles an hour.</summary>
        public const float Speed = 13f;

        public const float Acceleration = 3.5f;

        public PoliceCar(Vector3 position)
        {
            Driver = new RouteWalker(position, Speed) { Acceleration = Acceleration };
        }

        /// <summary>Moves it along the roads.</summary>
        public RouteWalker Driver { get; }

        public Vector3 Position => Driver.Position;

        public Vector3 Facing => Driver.Facing;

        public bool Lights { get; set; }

        public bool Siren { get; set; }

        public bool Visible { get; set; } = true;
    }

    /// <summary>What the people walking about need to know of the town around them.</summary>
    public interface ITownWorld : IDoors
    {
        /// <summary>Whether there's nothing solid between <paramref name="eye"/> and <paramref name="target"/>.</summary>
        bool CanSee(Vector3 eye, Vector3 target);

        /// <summary>Whether someone (the player) is standing within <paramref name="reach"/> ahead of <paramref name="position"/>.</summary>
        bool InTheWay(Vector3 position, Vector3 facing, float reach);
    }
}
