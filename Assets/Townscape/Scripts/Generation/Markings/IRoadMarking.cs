using Townscape.Generation.Layout;

namespace Townscape.Generation.Markings
{
    /// <summary>
    /// Strategy for painting one kind of road marking. Roads carry a list of these, so adding a
    /// new marking (a bus stop box, a "SLOW") never touches the road or ground generators.
    /// </summary>
    public interface IRoadMarking
    {
        void Paint(RoadSpec road, MarkingCanvas canvas);
    }
}
