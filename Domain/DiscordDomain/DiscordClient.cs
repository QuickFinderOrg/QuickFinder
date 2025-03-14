using Discord;
using Discord.WebSocket;
using group_finder.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace group_finder.Domain.DiscordDomain;

public class DiscordClient(IOptions<DiscordServiceOptions> options, ILogger<DiscordClient> logger) : DiscordSocketClient
{
    private readonly DiscordServiceOptions _options = options.Value;

    public async Task StartClientAsync()
    {
        await this.LoginAsync(TokenType.Bot, _options.BotToken);
        await this.StartAsync();

        this.MessageReceived += a => { logger.LogInformation("Discord bot recieved message {msg}", a.Content); return Task.CompletedTask; };
    }

}