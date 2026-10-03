namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>Strategy for dressing a shop window: books for a bookshop, monitors for the computer shop, and so on.</summary>
    public interface IShopDisplay
    {
        void Furnish(DisplayBox box);
    }
}
