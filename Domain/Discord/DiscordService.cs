using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace group_finder;

public class DiscordService(IOptions<DiscordServiceOptions> options, ILogger<DiscordService> logger)
{
    private readonly DiscordSocketClient _client = new DiscordSocketClient();
    private readonly DiscordServiceOptions _options = options.Value;

    public async Task StartAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _options.BotToken);
        await _client.StartAsync();

        _client.MessageReceived += a => { logger.LogInformation("Discord bot recieved message {msg}", a.Content); return Task.CompletedTask; };
    }

    public async Task<bool> SendDM(ulong userId, string message)
    {
        var user = await _client.GetUserAsync(userId);
        if (user == null)
        {
            Console.WriteLine("User not found.");
            return false;

        }


        try
        {
            await user.SendMessageAsync(message);
            Console.WriteLine($"Sent DM to {user.Username}: {message}");
            return true;
        }
        catch (Discord.Net.HttpException e)
        {
            Console.WriteLine($"Failed to send DM to {user.Username}: DiscordErrorCode: {e.DiscordCode}");
            return false;
        }
    }

    public async Task<ulong?> CreateChannel(string channelName)
    {
        var server = _client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            return null;
        }
        Console.WriteLine($"groupChannelId {_options.GroupChannelCategoryId}");

        var channel = await server.CreateTextChannelAsync(channelName, p => p.CategoryId = ulong.Parse(_options.GroupChannelCategoryId));
        Console.WriteLine(channel.ToString());
        return channel.Id;
    }

    public async Task<ulong?> DeleteChannel(ulong channelId)
    {
        var server = _client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            return null;
        }

        var channel = server.GetChannel(channelId);
        if (channel == null)
        {
            return null;
        }
        // TODO: limit to only delete within the category.
        await channel.DeleteAsync(new RequestOptions() { AuditLogReason = "DeleteChannel" });
        return channel.Id;
    }

    public DiscordChannel[] GetChannels()
    {
        var server = _client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            return [];
        }

        var channels = server.TextChannels.ToList();

        var discord_channels = channels.Select(channel => new DiscordChannel() { Id = channel.Id, Name = channel.Name, Category = channel.Category?.Name }).ToArray();

        return discord_channels;
    }
}

public record class DiscordChannel
{
    public required ulong Id { get; init; }
    public required string Name { get; init; }
    public string? Category { get; init; }
}

public class DiscordServiceOptions
{
    public const string Discord = "Discord";

    public string BotToken { get; set; } = String.Empty;
    public string ClientSecret { get; set; } = String.Empty;
    public string ClientId { get; set; } = String.Empty;
    public string ServerId { get; set; } = String.Empty;
    public string GroupChannelCategoryId { get; set; } = String.Empty;
}