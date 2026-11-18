namespace Inno_Shop.Products.Application.Validators
{
    /// <summary>
    /// Limits shared by the FluentValidation rules and the EF Core model configuration,
    /// so the API validation and the database constraints can never disagree.
    /// Same idea as <c>CommonRules</c> in the Users service.
    /// </summary>
    public static class ProductRules
    {
        public const int MaxNameLength = 200;
        public const int MaxDescriptionLength = 2000;
        public const int MaxOwnerIdLength = 450;
        public const int PriceDecimalPlaces = 2;
        public const decimal MaxPrice = 999_999_999.99m;
    }
}
