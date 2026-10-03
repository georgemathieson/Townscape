using System;
using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Shops
{
    /// <summary>Shelves of books along the back and piles in the window.</summary>
    public sealed class BookshopDisplay : IShopDisplay
    {
        private static readonly SurfaceMaterial[] Spines =
        {
            SurfaceMaterial.PaintOxblood, SurfaceMaterial.PaintNavy, SurfaceMaterial.PaintDarkGreen, SurfaceMaterial.PaintCream,
            SurfaceMaterial.PaintButter, SurfaceMaterial.PaintTeal, SurfaceMaterial.PaintBlack, SurfaceMaterial.PaintPurple,
        };

        public void Furnish(DisplayBox box)
        {
            DisplayParts.Shelves(box, 4, (x0, x1, y, d0, d1) =>
            {
                var x = x0 + 0.02f;
                while (x < x1 - 0.08f)
                {
                    var width = box.Range(0.035f, 0.075f);
                    if (box.Context.Chance(0.06f))
                    {
                        x += width * 2f;
                        continue;
                    }

                    box.Box(x, x + width, y, y + box.Range(0.2f, 0.33f), d0 + box.Range(0f, 0.04f), d1, box.Context.Pick(Spines));
                    x += width + 0.004f;
                }
            });

            // Piles of books lying flat on the window bed.
            for (var x = 0.25f; x < box.Width - 0.35f; x += box.Range(0.45f, 0.7f))
            {
                var y = 0f;
                var count = box.Context.Random.Next(3, 7);
                for (var i = 0; i < count; i++)
                {
                    var thickness = box.Range(0.03f, 0.055f);
                    var shift = box.Range(-0.03f, 0.03f);
                    box.Box(x + shift, x + 0.26f + shift, y, y + thickness, 0.12f, 0.33f, box.Context.Pick(Spines));
                    y += thickness;
                }
            }
        }
    }

    /// <summary>A café: counter and coffee machine at the back, a cake stand and bags of beans in the window.</summary>
    public sealed class CoffeeShopDisplay : IShopDisplay
    {
        public void Furnish(DisplayBox box)
        {
            var back = box.Depth;
            box.Box(0.2f, box.Width - 0.2f, 0f, 0.5f, back - 0.5f, back, SurfaceMaterial.Timber);
            box.Box(0.15f, box.Width - 0.15f, 0.5f, 0.55f, back - 0.55f, back, SurfaceMaterial.PaintCream);
            box.Box(box.Width * 0.3f, (box.Width * 0.3f) + 0.55f, 0.55f, 0.95f, back - 0.45f, back - 0.05f, SurfaceMaterial.PaintBlack);
            box.Box(box.Width * 0.3f, (box.Width * 0.3f) + 0.55f, 0.95f, 1.0f, back - 0.45f, back - 0.05f, SurfaceMaterial.ChimneyPot);

            // Menu board on the back wall.
            box.Panel(box.Width * 0.55f, 1.1f, box.Width - 0.3f, 1.75f, back - 0.01f, SurfaceMaterial.PaintBlack);
            box.Lettering("MENU", (box.Width * 0.775f) - 0.15f, 1.6f, (box.Width * 0.45f) - 0.2f, 0.025f, back - 0.02f, SurfaceMaterial.PaintCream);

            // Cake stand and coffee bags in the window.
            box.Round(0.55f, 0f, 0.35f, 0.04f, 0.3f, SurfaceMaterial.PaintCream);
            box.Round(0.55f, 0.3f, 0.35f, 0.2f, 0.03f, SurfaceMaterial.PaintCream);
            box.Round(0.55f, 0.33f, 0.35f, 0.13f, 0.1f, SurfaceMaterial.PaintPink);
            box.Round(0.55f, 0.15f, 0.35f, 0.26f, 0.02f, SurfaceMaterial.PaintCream);
            for (var i = 0; i < 4; i++)
            {
                var x = 1.1f + (i * 0.24f);
                if (x > box.Width - 0.3f)
                {
                    break;
                }

                box.Box(x, x + 0.16f, 0f, 0.26f, 0.15f, 0.27f, i % 2 == 0 ? SurfaceMaterial.Timber : SurfaceMaterial.PaintOxblood);
            }
        }
    }

    /// <summary>Monitors with glowing screens, a tower PC and boxed kit on the shelves.</summary>
    public sealed class ComputerShopDisplay : IShopDisplay
    {
        public void Furnish(DisplayBox box)
        {
            DisplayParts.Shelves(box, 3, (x0, x1, y, d0, d1) =>
            {
                for (var x = x0 + 0.05f; x < x1 - 0.3f; x += box.Range(0.3f, 0.42f))
                {
                    var material = box.Context.Chance(0.5f) ? SurfaceMaterial.PaintWhite : SurfaceMaterial.PaintNavy;
                    box.Box(x, x + 0.25f, y, y + box.Range(0.18f, 0.32f), d0 + 0.02f, d1, material);
                }
            });

            var count = Math.Max(1, (int)((box.Width - 0.4f) / 0.9f));
            for (var i = 0; i < count; i++)
            {
                var x = 0.3f + (i * 0.9f);
                Monitor(box, x, 0.35f);
            }

            box.Box(box.Width - 0.45f, box.Width - 0.22f, 0f, 0.48f, 0.25f, 0.65f, SurfaceMaterial.PaintBlack);
            box.Panel(box.Width - 0.42f, 0.38f, box.Width - 0.25f, 0.42f, 0.249f, SurfaceMaterial.Screen);
        }

        private static void Monitor(DisplayBox box, float x, float d)
        {
            box.Box(x + 0.25f, x + 0.33f, 0f, 0.18f, d + 0.05f, d + 0.12f, SurfaceMaterial.PaintBlack);
            box.Box(x + 0.15f, x + 0.43f, 0f, 0.02f, d, d + 0.16f, SurfaceMaterial.PaintBlack);
            box.Box(x, x + 0.58f, 0.18f, 0.56f, d, d + 0.05f, SurfaceMaterial.PaintBlack);
            box.Panel(x + 0.03f, 0.21f, x + 0.55f, 0.53f, d - 0.002f, SurfaceMaterial.Screen);
            box.Box(x + 0.05f, x + 0.53f, 0f, 0.02f, d - 0.25f, d - 0.1f, SurfaceMaterial.PaintBlack);
        }
    }

    /// <summary>A cosy newsagent: magazines in rows, newspapers on the bed, jars of sweets.</summary>
    public sealed class NewsagentDisplay : IShopDisplay
    {
        private static readonly SurfaceMaterial[] Covers =
        {
            SurfaceMaterial.PaintRed, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintDuckEgg, SurfaceMaterial.PaintPink,
            SurfaceMaterial.PaintWhite, SurfaceMaterial.PaintNavy, SurfaceMaterial.PaintOrange, SurfaceMaterial.PaintSage,
        };

        public void Furnish(DisplayBox box)
        {
            // Racks of magazines facing out, each row slightly further back.
            for (var row = 0; row < 4; row++)
            {
                var y = 0.45f + (row * 0.38f);
                var d = box.Depth - 0.35f + (row * 0.06f);
                box.Box(0.1f, box.Width - 0.1f, y - 0.03f, y, d - 0.08f, d + 0.04f, SurfaceMaterial.Timber);
                for (var x = 0.15f; x < box.Width - 0.35f; x += 0.24f)
                {
                    box.Box(x, x + 0.2f, y, y + 0.28f, d, d + 0.02f, box.Context.Pick(Covers));
                }
            }

            // Newspapers and sweet jars on the window bed.
            for (var x = 0.2f; x < box.Width * 0.55f; x += 0.42f)
            {
                var stack = box.Range(0.06f, 0.14f);
                box.Box(x, x + 0.34f, 0f, stack, 0.12f, 0.36f, SurfaceMaterial.PaintWhite);
                box.Box(x, x + 0.34f, stack, stack + 0.005f, 0.12f, 0.36f, box.Context.Chance(0.3f) ? SurfaceMaterial.PaintPink : SurfaceMaterial.PaintCream);
            }

            for (var x = box.Width * 0.6f; x < box.Width - 0.2f; x += 0.22f)
            {
                box.Round(x, 0f, 0.25f, 0.07f, 0.2f, SurfaceMaterial.ShopGlass);
                box.Round(x, 0f, 0.25f, 0.055f, 0.13f, box.Context.Pick(Covers));
                box.Round(x, 0.2f, 0.25f, 0.06f, 0.03f, SurfaceMaterial.PaintRed);
            }
        }
    }

    /// <summary>Tiers of loaves and cakes.</summary>
    public sealed class BakeryDisplay : IShopDisplay
    {
        public void Furnish(DisplayBox box)
        {
            for (var tier = 0; tier < 3; tier++)
            {
                var y = tier * 0.32f;
                var d0 = 0.15f + (tier * 0.3f);
                box.Box(0.1f, box.Width - 0.1f, y, y + 0.05f, d0, d0 + 0.32f, SurfaceMaterial.Timber);
                for (var x = 0.2f; x < box.Width - 0.3f; x += 0.3f)
                {
                    if (tier == 1)
                    {
                        box.Round(x + 0.1f, y + 0.05f, d0 + 0.15f, 0.1f, 0.09f, box.Context.Chance(0.5f) ? SurfaceMaterial.PaintPink : SurfaceMaterial.PaintCream);
                        box.Box(x + 0.08f, x + 0.12f, y + 0.14f, y + 0.18f, d0 + 0.13f, d0 + 0.17f, SurfaceMaterial.PaintRed);
                    }
                    else
                    {
                        box.Box(x, x + 0.22f, y + 0.05f, y + 0.13f, d0 + 0.07f, d0 + 0.22f, SurfaceMaterial.Bread);
                        box.Context.Builder.AddPyramid(box.Point(x + 0.11f, y + 0.13f, d0 + 0.145f), box.Wall.Right, -box.Wall.Out, new System.Numerics.Vector2(0.11f, 0.075f), 0.05f, System.Numerics.Vector3.UnitY, SurfaceMaterial.Bread);
                    }
                }
            }
        }
    }

    /// <summary>A fish and chip shop: steel counter, fryer and a menu board.</summary>
    public sealed class ChippyDisplay : IShopDisplay
    {
        public void Furnish(DisplayBox box)
        {
            var back = box.Depth;
            box.Box(0.1f, box.Width - 0.1f, 0f, 0.55f, back - 0.6f, back - 0.05f, SurfaceMaterial.StoneDark);
            box.Box(0.3f, 1.1f, 0.55f, 0.85f, back - 0.5f, back - 0.1f, SurfaceMaterial.Kerb);
            box.Panel(0.25f, 0.95f, box.Width - 0.25f, 1.65f, back - 0.01f, SurfaceMaterial.PaintNavy);
            box.Lettering("FISH & CHIPS", box.Width * 0.5f, 1.45f, box.Width - 0.7f, 0.03f, back - 0.02f, SurfaceMaterial.PaintWhite);
            box.Lettering("MUSHY PEAS", box.Width * 0.5f, 1.15f, box.Width - 0.7f, 0.022f, back - 0.02f, SurfaceMaterial.PaintButter);
        }
    }

    /// <summary>Buckets of flowers.</summary>
    public sealed class FloristDisplay : IShopDisplay
    {
        private static readonly SurfaceMaterial[] Petals =
        {
            SurfaceMaterial.PaintPink, SurfaceMaterial.PaintButter, SurfaceMaterial.PaintPurple, SurfaceMaterial.PaintOrange,
            SurfaceMaterial.PaintWhite, SurfaceMaterial.PaintRed,
        };

        public void Furnish(DisplayBox box)
        {
            for (var row = 0; row < 2; row++)
            {
                var y = row * 0.35f;
                var d = 0.3f + (row * 0.45f);
                if (row == 1)
                {
                    box.Box(0.1f, box.Width - 0.1f, 0f, y, d - 0.2f, d + 0.2f, SurfaceMaterial.Timber);
                }

                for (var x = 0.25f; x < box.Width - 0.25f; x += 0.36f)
                {
                    box.Round(x, y, d, 0.13f, 0.28f, SurfaceMaterial.StoneDark);
                    var petals = box.Context.Pick(Petals);
                    for (var k = 0; k < 6; k++)
                    {
                        var fx = x + box.Range(-0.1f, 0.1f);
                        var fy = y + 0.3f + box.Range(0f, 0.22f);
                        var fd = d + box.Range(-0.1f, 0.1f);
                        box.Box(fx - 0.006f, fx + 0.006f, y + 0.25f, fy, fd - 0.006f, fd + 0.006f, SurfaceMaterial.GrassDark);
                        box.Box(fx - 0.045f, fx + 0.045f, fy, fy + 0.07f, fd - 0.045f, fd + 0.045f, petals);
                    }
                }
            }
        }
    }

    /// <summary>Framed pictures on the walls and a canvas on an easel.</summary>
    public sealed class GalleryDisplay : IShopDisplay
    {
        private static readonly SurfaceMaterial[] Paintings =
        {
            SurfaceMaterial.FellGrass, SurfaceMaterial.PaintDuckEgg, SurfaceMaterial.Bracken, SurfaceMaterial.PaintNavy, SurfaceMaterial.PaintSage,
        };

        public void Furnish(DisplayBox box)
        {
            var back = box.Depth - 0.01f;
            for (var x = 0.2f; x < box.Width - 0.6f; x += box.Range(0.65f, 0.9f))
            {
                var w = box.Range(0.4f, 0.55f);
                var h = box.Range(0.35f, 0.6f);
                var y = box.Range(0.7f, 1.0f);
                box.Box(x, x + w, y, y + h, back - 0.03f, back, SurfaceMaterial.PaintGold);
                box.Panel(x + 0.05f, y + 0.05f, x + w - 0.05f, y + h - 0.05f, back - 0.035f, box.Context.Pick(Paintings));
            }

            var easel = box.Width * 0.5f;
            box.Box(easel - 0.25f, easel - 0.21f, 0f, 1.0f, 0.4f, 0.44f, SurfaceMaterial.Timber);
            box.Box(easel + 0.21f, easel + 0.25f, 0f, 1.0f, 0.4f, 0.44f, SurfaceMaterial.Timber);
            box.Box(easel - 0.3f, easel + 0.3f, 0.45f, 0.95f, 0.36f, 0.4f, SurfaceMaterial.PaintWhite);
            box.Panel(easel - 0.26f, 0.49f, easel + 0.26f, 0.91f, 0.359f, box.Context.Pick(Paintings));
        }
    }

    /// <summary>General shop: shelves of goods in the shop's colours, and a few bigger items in the window.</summary>
    public sealed class ShelvesDisplay : IShopDisplay
    {
        private readonly IReadOnlyList<SurfaceMaterial> _goods;
        private readonly float _itemWidth;
        private readonly float _itemHeight;

        public ShelvesDisplay(IReadOnlyList<SurfaceMaterial> goods, float itemWidth = 0.14f, float itemHeight = 0.18f)
        {
            _goods = goods;
            _itemWidth = itemWidth;
            _itemHeight = itemHeight;
        }

        public void Furnish(DisplayBox box)
        {
            DisplayParts.Shelves(box, 4, (x0, x1, y, d0, d1) =>
            {
                for (var x = x0 + 0.04f; x < x1 - _itemWidth; x += _itemWidth + box.Range(0.02f, 0.08f))
                {
                    box.Box(x, x + _itemWidth, y, y + (_itemHeight * box.Range(0.7f, 1.2f)), d0 + 0.03f, d1, box.Context.Pick(_goods));
                }
            });

            for (var x = 0.3f; x < box.Width - 0.4f; x += box.Range(0.5f, 0.8f))
            {
                var size = box.Range(0.18f, 0.3f);
                box.Box(x, x + size, 0f, size * box.Range(0.8f, 1.6f), 0.2f, 0.2f + size, box.Context.Pick(_goods));
            }
        }
    }

    internal static class DisplayParts
    {
        public delegate void ShelfFiller(float x0, float x1, float y, float d0, float d1);

        /// <summary>Timber shelves along the back wall; <paramref name="fill"/> stocks each one.</summary>
        public static void Shelves(DisplayBox box, int count, ShelfFiller fill)
        {
            var d1 = box.Depth;
            var d0 = d1 - 0.32f;
            var spacing = (box.Height - 0.2f) / count;
            for (var i = 0; i < count; i++)
            {
                var y = 0.15f + (i * spacing);
                box.Box(0.02f, box.Width - 0.02f, y - 0.03f, y, d0, d1, SurfaceMaterial.Timber);
                fill(0.02f, box.Width - 0.02f, y, d0, d1);
            }
        }
    }
}
