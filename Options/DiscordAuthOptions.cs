namespace QuickFinder.Options;

public class DiscordAuthOptions
{
    public const string DiscordAuth = "DiscordAuth";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectScheme { get; set; } = "https";

    public bool IsValid =>
        !string.IsNullOrEmpty(ClientId)
        && !string.IsNullOrEmpty(ClientSecret)
        && !string.IsNullOrEmpty(RedirectScheme);
}
