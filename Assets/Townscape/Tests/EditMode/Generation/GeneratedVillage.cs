using Townscape.Generation;
using Townscape.Generation.Layout;

namespace Townscape.Tests.Generation
{
    /// <summary>Generating the whole village takes a moment, so tests share one copy.</summary>
    internal static class GeneratedVillage
    {
        private static TownLayout _layout;
        private static GeneratedTown _town;

        public static TownLayout Layout => _layout ??= new LakeDistrictVillageLayout().Create();

        public static GeneratedTown Town => _town ??= new TownGenerator().Generate(Layout);
    }
}
