using System;

namespace LingGuangV05.Core
{
    /// <summary>Only for purchase quotes. Do not apply to bills, rewards, wages or resale values.</summary>
    public static class PurchasePricing
    {
        public static double AtLeastHundred(double price)
        {
            if (price <= 0 || double.IsNaN(price) || double.IsInfinity(price)) return price;
            // Multiplication, not a flat minimum: 15 -> 150; 8 -> 800. Idempotent.
            while (price < 100) price *= 10;
            return price;
        }
    }
}
