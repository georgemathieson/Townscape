namespace Townscape.Generation
{
    /// <summary>Resolution and extent knobs for generation. The defaults suit the shipped layout.</summary>
    public sealed class GenerationSettings
    {
        /// <summary>Half the width of the detailed square around the town centre, in metres.</summary>
        public float CoreHalfExtent { get; init; } = 100f;

        /// <summary>Ground grid spacing inside the core, in metres.</summary>
        public float CellSize { get; init; } = 1f;

        /// <summary>Core ground is split into square chunks of this many cells for culling.</summary>
        public int ChunkCells { get; init; } = 50;

        /// <summary>Half the width of the surrounding fells.</summary>
        public float FarHalfExtent { get; init; } = 700f;

        public float FarCellSize { get; init; } = 10f;

        /// <summary>Half the width of the water plane that fills the river and the lake.</summary>
        public float WaterHalfExtent { get; init; } = 800f;

        public static GenerationSettings Default { get; } = new GenerationSettings();
    }
}
