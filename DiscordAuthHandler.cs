using Microsoft.Extensions.Options;
using QuickFinder.Options;

namespace QuickFinder;

public class DiscordAuthHandler(IOptions<DiscordAuthOptions> options)
{
    // TODO: DISCORD AUTH use options and scopes
    private readonly DiscordAuthOptions _options = options.Value;
    private readonly string[] Scopes = ["identify", "email", "guilds", "guilds.join"];

    public string CreateAuthUrl(string host)
    {
        var redirect_uri_raw = $"https://{host}/LoginDiscord";
        var redirect_uri = System.Net.WebUtility.UrlEncode(redirect_uri_raw);
        var discord_url =
            $"https://discord.com/oauth2/authorize?client_id=1328341129078505499&response_type=code&redirect_uri={redirect_uri}&scope=identify+email+guilds+guilds.join";
        return discord_url;
    }
}
