using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Layout
{
    /// <summary>The shops of the village, with their signs, paintwork and window displays.</summary>
    public static class VillageShops
    {
        // The must-haves: two bookshops, two coffee shops, a computer shop and a cosy newsagent.
        public static readonly ShopDefinition LanternBooks = new ShopDefinition("LANTERN BOOKS", SurfaceMaterial.PaintDarkGreen, new BookshopDisplay()) { HangingSign = true };
        public static readonly ShopDefinition Inkwell = new ShopDefinition("THE INKWELL", SurfaceMaterial.PaintOxblood, new BookshopDisplay());
        public static readonly ShopDefinition FellsideCoffee = new ShopDefinition("FELLSIDE COFFEE", SurfaceMaterial.PaintTeal, new CoffeeShopDisplay(), SurfaceMaterial.PaintCream) { Awning = true, HangingSign = true };
        public static readonly ShopDefinition CopperKettle = new ShopDefinition("THE COPPER KETTLE", SurfaceMaterial.PaintSage, new CoffeeShopDisplay(), SurfaceMaterial.PaintOxblood) { Awning = true, AwningStripe = SurfaceMaterial.PaintWhite, Enterable = true };
        public static readonly ShopDefinition PixelAndByte = new ShopDefinition("PIXEL & BYTE", SurfaceMaterial.PaintBlack, new ComputerShopDisplay(), SurfaceMaterial.PaintDuckEgg);
        public static readonly ShopDefinition HartleysNews = new ShopDefinition("HARTLEY'S NEWS", SurfaceMaterial.PaintNavy, new NewsagentDisplay());

        public static readonly ShopDefinition Packhorse = new ShopDefinition("THE PACKHORSE", SurfaceMaterial.PaintOxblood, null)
        {
            Frontage = InnFrontage.Instance,
            Wall = SurfaceMaterial.RenderWhite,
            WindowFrames = SurfaceMaterial.PaintBlack,
        };

        public static readonly ShopDefinition CoopersBakery = new ShopDefinition("COOPER'S BAKERY", SurfaceMaterial.PaintButter, new BakeryDisplay(), SurfaceMaterial.PaintOxblood) { Awning = true, AwningStripe = SurfaceMaterial.PaintWhite };
        public static readonly ShopDefinition MrsDodds = new ShopDefinition("MRS DODD'S SWEETS", SurfaceMaterial.PaintPink, Shelves(SurfaceMaterial.PaintRed, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintPurple, SurfaceMaterial.PaintOrange, SurfaceMaterial.PaintDuckEgg), SurfaceMaterial.PaintOxblood);
        public static readonly ShopDefinition LakesideChippy = new ShopDefinition("LAKESIDE CHIPPY", SurfaceMaterial.PaintNavy, new ChippyDisplay(), SurfaceMaterial.PaintWhite);
        public static readonly ShopDefinition PostOffice = new ShopDefinition("POST OFFICE", SurfaceMaterial.PaintRed, Shelves(SurfaceMaterial.PaintRed, SurfaceMaterial.PaintWhite, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintNavy), SurfaceMaterial.PaintButter);
        public static readonly ShopDefinition FellAndCrag = new ShopDefinition("FELL & CRAG", SurfaceMaterial.PaintDarkGreen, Shelves(0.24f, 0.32f, SurfaceMaterial.PaintOrange, SurfaceMaterial.PaintRed, SurfaceMaterial.PaintNavy, SurfaceMaterial.PaintButter), SurfaceMaterial.PaintCream);
        public static readonly ShopDefinition Bluebell = new ShopDefinition("BLUEBELL FLOWERS", SurfaceMaterial.PaintDuckEgg, new FloristDisplay(), SurfaceMaterial.PaintNavy);
        public static readonly ShopDefinition Butcher = new ShopDefinition("BAINES BUTCHER", SurfaceMaterial.PaintOxblood, Shelves(SurfaceMaterial.PaintRed, SurfaceMaterial.PaintPink, SurfaceMaterial.PaintWhite), SurfaceMaterial.PaintCream);
        public static readonly ShopDefinition Ironmonger = new ShopDefinition("IRONMONGER", SurfaceMaterial.PaintBlack, Shelves(SurfaceMaterial.StoneDark, SurfaceMaterial.Timber, SurfaceMaterial.PaintRed, SurfaceMaterial.PaintSage));
        public static readonly ShopDefinition Chemist = new ShopDefinition("CHEMIST", SurfaceMaterial.PaintTeal, Shelves(SurfaceMaterial.PaintWhite, SurfaceMaterial.PaintTeal, SurfaceMaterial.PaintDuckEgg), SurfaceMaterial.PaintWhite);
        public static readonly ShopDefinition FellGallery = new ShopDefinition("FELL GALLERY", SurfaceMaterial.PaintNavy, new GalleryDisplay());
        public static readonly ShopDefinition SkeinAndFell = new ShopDefinition("SKEIN & FELL", SurfaceMaterial.PaintPurple, Shelves(0.1f, 0.1f, SurfaceMaterial.PaintPink, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintDuckEgg, SurfaceMaterial.PaintSage, SurfaceMaterial.PaintOxblood), SurfaceMaterial.PaintCream);
        public static readonly ShopDefinition Barber = new ShopDefinition("BARBER", SurfaceMaterial.PaintRed, Shelves(SurfaceMaterial.PaintWhite, SurfaceMaterial.PaintBlack, SurfaceMaterial.PaintNavy), SurfaceMaterial.PaintWhite);

        private static ShelvesDisplay Shelves(params SurfaceMaterial[] goods) => new ShelvesDisplay(goods);

        private static ShelvesDisplay Shelves(float itemWidth, float itemHeight, params SurfaceMaterial[] goods) => new ShelvesDisplay(goods, itemWidth, itemHeight);
    }
}
