using System.Collections.Generic;

namespace Townscape.CoffeeShop
{
    public enum ItemKind
    {
        /// <summary>Made to order from ingredients.</summary>
        Drink,

        /// <summary>Ready to sell: baked in the morning, one per customer, wasted if unsold.</summary>
        Pastry,
    }

    /// <summary>Something on the menu.</summary>
    public sealed record ItemDef
    {
        private readonly string _pluralName;

        public string Id { get; init; }

        public string Name { get; init; }

        /// <summary>The name for more than one ("Pots of tea"). Defaults to the name with an s.</summary>
        public string PluralName
        {
            get => _pluralName ?? Name + "s";
            init => _pluralName = value;
        }

        public ItemKind Kind { get; init; }

        public int PricePence { get; init; }

        /// <summary>For pastries, what each one costs to buy in. Drinks cost their ingredients instead.</summary>
        public int UnitCostPence { get; init; }

        /// <summary>Seconds at the counter to serve one.</summary>
        public int ServeSeconds { get; init; }

        /// <summary>For drinks: what one serving uses.</summary>
        public IReadOnlyList<RecipePart> Recipe { get; init; } = new RecipePart[0];

        /// <summary>
        /// For pastries: free of dairy. Drinks are dairy-free unless they use milk, and then only
        /// with a dairy-free milk.
        /// </summary>
        public bool DairyFree { get; init; }

        /// <summary>For pastries: who bakes them. Drinks' ingredients have their own suppliers.</summary>
        public SupplierDef Supplier { get; init; }
    }

    /// <summary>Who sells something to the shop, as they advertise in the paper's classifieds.</summary>
    public sealed record SupplierDef(string Name, string Advert);

    /// <summary>A number of one item: how many to bake, or servings to stock for.</summary>
    public sealed record ItemCount(string ItemId, int Count);

    /// <summary>Portions of one ingredient in one serving of a drink.</summary>
    public sealed record RecipePart(string IngredientId, int Portions);

    /// <summary>Something drinks are made from, bought in portions.</summary>
    public sealed record IngredientDef
    {
        public string Id { get; init; }

        public string Name { get; init; }

        /// <summary>What one portion costs. Milk's cost comes from the chosen <see cref="MilkOption"/> instead.</summary>
        public int CostPerPortionPence { get; init; }

        /// <summary>Thrown away at closing if unused (milk). Anything else keeps for tomorrow.</summary>
        public bool SpoilsOvernight { get; init; }

        /// <summary>The one ingredient whose kind the player chooses: dairy or an alternative.</summary>
        public bool IsMilk { get; init; }

        /// <summary>Who sells it. Milk's supplier depends on the chosen <see cref="MilkOption"/>.</summary>
        public SupplierDef Supplier { get; init; }
    }

    /// <summary>The milk the shop stocks for every milky drink: one choice, not one per drink.</summary>
    public sealed record MilkOption
    {
        public string Id { get; init; }

        public string Name { get; init; }

        public int CostPerPortionPence { get; init; }

        public bool DairyFree { get; init; }

        /// <summary>
        /// Share of customers (who can have dairy) happy with a milky drink made with it.
        /// Dairy suits everyone; some people won't take oat milk in their latte.
        /// </summary>
        public double Appeal { get; init; } = 1.0;

        public SupplierDef Supplier { get; init; }
    }
}
