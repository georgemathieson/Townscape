using Townscape.State;

namespace Townscape.CoffeeShop
{
    /// <summary>Put an item on the menu or take it off.</summary>
    public sealed record SetOnMenu(string ItemId, bool OnMenu) : IAction;

    /// <summary>Choose the milk every milky drink is made with.</summary>
    public sealed record SetMilk(string MilkId) : IAction;

    /// <summary>
    /// How many of an item to stock for: pastries to bake, or servings of a drink to buy ingredients
    /// for. Limited to what fits in the display case or the fridge.
    /// </summary>
    public sealed record SetPlanned(string ItemId, int Count) : IAction;

    /// <summary>Keep this much cash back so upgrades can't spend it.</summary>
    public sealed record SetReserve(int Pence) : IAction;

    /// <summary>Buy the stock and trade the day. Does nothing unless prepping and the stock is affordable.</summary>
    public sealed record OpenForTheDay : IAction;

    /// <summary>Done reviewing: start prepping the next day.</summary>
    public sealed record ContinueToPrep : IAction;

    /// <summary>Buy an upgrade with cash above the reserve. It works from the next day traded.</summary>
    public sealed record BuyUpgrade(string UpgradeId) : IAction;

    /// <summary>Start again from day 1.</summary>
    public sealed record StartNewGame(int Seed) : IAction;
}
