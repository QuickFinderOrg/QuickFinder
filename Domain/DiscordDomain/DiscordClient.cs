using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace group_finder.Domain.DiscordDomain;

public class DiscordClient(IOptions<DiscordServiceOptions> options, ILogger<DiscordClient> logger) : DiscordSocketClient, IHostedService
{
    private readonly DiscordServiceOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.LoginAsync(TokenType.Bot, _options.BotToken);
        await this.StartAsync();

        this.MessageReceived += a => { logger.LogInformation("Discord bot recieved message from user {user} '{msg}'", a.Author.GlobalName, a.Content); return Task.CompletedTask; };

        logger.LogInformation("Discord client started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await this.StopAsync();
        logger.LogInformation("Discord client shut down.");
    }
}