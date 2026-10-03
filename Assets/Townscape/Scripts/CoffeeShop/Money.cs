using System.Globalization;

namespace Townscape.CoffeeShop
{
    /// <summary>Money is kept in whole pence so sums are exact; this shows it in pounds.</summary>
    public static class Money
    {
        /// <summary>"£3.40", "-£12.05", "£240".</summary>
        public static string Format(int pence)
        {
            var sign = pence < 0 ? "-" : string.Empty;
            var abs = System.Math.Abs((long)pence);
            var pounds = abs / 100;
            var rest = abs % 100;
            return rest == 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}£{1:N0}", sign, pounds)
                : string.Format(CultureInfo.InvariantCulture, "{0}£{1:N0}.{2:00}", sign, pounds, rest);
        }

        /// <summary>"+£3.40" or "-£3.40", for differences.</summary>
        public static string Signed(int pence) => pence > 0 ? "+" + Format(pence) : Format(pence);
    }
}
