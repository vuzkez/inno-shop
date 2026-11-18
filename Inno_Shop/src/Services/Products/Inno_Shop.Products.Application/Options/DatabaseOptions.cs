namespace Inno_Shop.Products.Application.Options
{
    public class DatabaseOptions
    {
        public const string SectionName = "ConnectionStrings";

        public string DefaultConnection { get; set; } = string.Empty;
    }
}
