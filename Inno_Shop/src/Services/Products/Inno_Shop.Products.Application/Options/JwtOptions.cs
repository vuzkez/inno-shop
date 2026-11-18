namespace Inno_Shop.Products.Application.Options
{
    /// <summary>
    /// Must match the Users service exactly (issuer, audience, secret), otherwise a token issued
    /// by Users would be rejected here.
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "JwtSettings";

        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; }
        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}
