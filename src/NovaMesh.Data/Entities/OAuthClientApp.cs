namespace NovaMesh.Data.Entities;

public class OAuthClientApp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientType { get; set; } = "Confidential";
    public string AllowedGrantTypes { get; set; } = "authorization_code, refresh_token";
    public string AllowedScopes { get; set; } = "openid, profile, email, api";
    public string RedirectUris { get; set; } = "https://localhost:5001/signin-oidc";
    public string? PostLogoutRedirectUris { get; set; } = "https://localhost:5001/signout-callback-oidc";
    public string ClientSecretMasked { get; set; } = "sk_live_••••••••••••••••";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}