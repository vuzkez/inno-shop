using Inno_Shop.Products.Application.Validators;

namespace Inno_Shop.Products.Application.Validators
{
    /// <summary>Price rules shared by the create and update validators.</summary>
    public static class ProductPriceRules
    {
        public static bool HaveAtMostTwoDecimals(decimal price) =>
            decimal.Round(price, ProductRules.PriceDecimalPlaces) == price;
    }
}
