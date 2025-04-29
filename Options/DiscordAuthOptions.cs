namespace QuickFinder.Options;

public class DiscordAuthOptions
{
    public const string DiscordAuth = "DiscordAuth";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
