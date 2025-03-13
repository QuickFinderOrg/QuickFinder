using Discord;
using Discord.WebSocket;

namespace group_finder;

public class DiscordService(ILogger<DiscordService> logger)
{
    private readonly DiscordSocketClient _client = new DiscordSocketClient();

    private ulong serverId;
    private ulong groupChannelId;

    public async Task StartAsync(ulong serverId, ulong groupChannelId, string token)
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        this.serverId = serverId;
        this.groupChannelId = groupChannelId;

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
        var server = _client.GetGuild(serverId);
        if (server == null)
        {
            return null;
        }
        Console.WriteLine($"groupChannelId {groupChannelId}");

        var channel = await server.CreateTextChannelAsync(channelName, p => p.CategoryId = groupChannelId);
        Console.WriteLine(channel.ToString());
        return channel.Id;
    }

    public async Task<ulong?> DeleteChannel(ulong channelId)
    {
        var server = _client.GetGuild(serverId);
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
        var server = _client.GetGuild(serverId);
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
