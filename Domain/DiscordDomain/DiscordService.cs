using Discord;
using group_finder.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace group_finder.Domain.DiscordDomain;

public class DiscordService(IOptions<DiscordServiceOptions> options, ILogger<DiscordService> logger, ApplicationDbContext db, DiscordClient client)
{
    private readonly DiscordServiceOptions _options = options.Value;


    public async Task<bool> SendDM(ulong userId, string message)
    {
        var user = await client.GetUserAsync(userId);
        if (user == null)
        {
            logger.LogError("User {userId} not found.", userId);
            return false;

        }


        try
        {
            await user.SendMessageAsync(message);
            logger.LogInformation("Sent DM to {username} ({userid}): {message}", user.Username, userId, message);
            return true;
        }
        catch (Discord.Net.HttpException e)
        {
            logger.LogError(e, "Failed to send DM to {username}: DiscordErrorCode: {DiscordErrorCode}", user.Username, e.DiscordCode);
            return false;
        }
    }

    public async Task<ulong?> CreateChannel(string channelName)
    {
        var server = client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            return null;
        }

        var channel = await server.CreateTextChannelAsync(channelName, p => p.CategoryId = ulong.Parse(_options.GroupChannelCategoryId));
        logger.LogTrace("Created Discord channel '{name}' ({id})", channel.ToString(), channel.Id);
        return channel.Id;
    }

    public async Task<ulong?> DeleteChannel(ulong channelId)
    {
        var server = client.GetGuild(ulong.Parse(_options.ServerId));
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
        logger.LogTrace("Deleted Discord channel '{name}' ({id})", channel.ToString(), channel.Id);
        return channel.Id;
    }

    public DiscordChannel[] GetChannels()
    {
        var server = client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            return [];
        }

        var channels = server.TextChannels.ToList();

        var discord_channels = channels.Select(channel => new DiscordChannel() { Id = channel.Id, Name = channel.Name, Category = channel.Category?.Name }).ToArray();

        return discord_channels;
    }

    public async Task<DiscordServerItem[]> GetServerList()
    {
        var servers = await db.DiscordServers.ToArrayAsync();
        return servers.Select(s => new DiscordServerItem() { Id = s.Id, Name = s.Name }).ToArray();
    }
}

public record class DiscordChannel
{
    public required ulong Id { get; init; }
    public required string Name { get; init; }
    public string? Category { get; init; }
}

public record class DiscordServerItem
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

    public string CallbackPath = String.Empty;
    public string AuthorizationEndpoint = String.Empty;
    public string TokenEndpoint = String.Empty;
    public string UserInformationEndpoint = String.Empty;
}