using Discord;
using group_finder.Data;
using group_finder.Domain.Matchmaking;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace group_finder.Domain.DiscordDomain;

public class DiscordService
{
    private readonly DiscordServiceOptions _options;
    private readonly ILogger<DiscordService> _logger;
    private readonly IServiceProvider _serviceProvider; // Inject IServiceProvider

    public DiscordService(
        IOptions<DiscordServiceOptions> options,
        ILogger<DiscordService> logger,
        IServiceProvider serviceProvider,
        DiscordClient client)
    {
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        Client = client;
    }

    public DiscordClient Client { get; } //No longer relies on injection

    public async Task<bool> SendDM(ulong userId, string message)
    {
        var user = await Client.GetUserAsync(userId);
        if (user == null)
        {
            _logger.LogError("User {userId} not found.", userId);
            return false;
        }

        try
        {
            await user.SendMessageAsync(message);
            _logger.LogInformation("Sent DM to {username} ({userid}): {message}", user.Username, userId, message);
            return true;
        }
        catch (Discord.Net.HttpException e)
        {
            _logger.LogError(e, "Failed to send DM to {username}: DiscordErrorCode: {DiscordErrorCode}", user.Username, e.DiscordCode);
            return false;
        }
    }

    public async Task<ulong?> CreateChannel(ulong serverId, string channelName, ulong? categoryId, Guid? owningGroup)
    {
        var server = Client.GetGuild(serverId);
        if (server == null)
        {
            return null;
        }

        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var serverDB = await dbContext.DiscordServers.FirstAsync(s => s.Id == serverId) ?? throw new Exception($"Server {_options.ServerId} not in db");

            var channel = await server.CreateTextChannelAsync(channelName, p => p.CategoryId = ulong.Parse(_options.GroupChannelCategoryId));
            var channelDB = new Channel() { Id = channel.Id, CategoryId = ulong.Parse(_options.GroupChannelCategoryId), Server = serverDB, OwningGroupId = owningGroup };
            dbContext.Add(channelDB);
            // await channel.SendMessageAsync("@everyone");

            _logger.LogInformation("Created Discord channel '{name}' ({id})", channel.ToString(), channel.Id);
            await dbContext.SaveChangesAsync();
            return channel.Id;
        }
    }

    public async Task<ulong?> SetUserPermissionsOnChannel(ulong channelId, ulong userId)
    {
        var server = Client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            _logger.LogError("Server not found '{userId}'", userId);
            return null;
        }

        var discord_user = server.GetUser(userId);
        if (discord_user == null)
        {
            _logger.LogError("User not found '{userId}'", userId);
            return null;
        }

        var discord_channel = server.GetChannel(channelId);
        if (discord_user == null)
        {
            _logger.LogError("Channel not found '{channelId}'", channelId);
            return null;
        }

        var permissions = new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow);
        await discord_channel.AddPermissionOverwriteAsync(discord_user, permissions);

        return channelId;
    }

    public async Task<ulong?> DeleteChannel(ulong channelId)
    {
        var server = Client.GetGuild(ulong.Parse(_options.ServerId));
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
        _logger.LogTrace("Deleted Discord channel '{name}' ({id})", channel.ToString(), channel.Id);

        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.SaveChangesAsync();
        }
        return channel.Id;
    }

    public DiscordChannel[] GetChannels()
    {
        var server = Client.GetGuild(ulong.Parse(_options.ServerId));
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
        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var servers = await dbContext.DiscordServers.ToArrayAsync();
            return servers.Select(s => new DiscordServerItem() { Id = s.Id, Name = s.Name }).ToArray();
        }
    }

    public async Task DeleteGroupChannels(Guid groupId)
    {
        var server = Client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            return;
        }

        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var channels = await dbContext.DiscordChannels
                .Where(ch => ch.OwningGroupId == groupId)
                .ToArrayAsync();

            foreach (var channel in channels)
            {
                await DeleteChannel(channel.Id);
            }
        }
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

public class CreateDiscordChannelOnGroupFilled : INotificationHandler<GroupFilled>
{
    private readonly IOptions<DiscordServiceOptions> _options;
    private readonly DiscordService _discord;
    private readonly UserService _userService;
    private readonly ILogger<NotifyUsersOnGroupFilled> _logger;

    public CreateDiscordChannelOnGroupFilled(IOptions<DiscordServiceOptions> options, DiscordService discord, UserService userService, ILogger<NotifyUsersOnGroupFilled> logger)
    {
        _options = options;
        _discord = discord;
        _userService = userService;
        _logger = logger;
    }

    public async Task Handle(GroupFilled notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Group filled {groupId}", notification.Group.Id);
        var defaultServerId = ulong.Parse(_options.Value.ServerId);
        var defaultCategoryId = ulong.Parse(_options.Value.GroupChannelCategoryId);
        var channelName = notification.Group.Id.ToString();

        try
        {
            var new_channel_id = await _discord.CreateChannel(defaultServerId, channelName, defaultCategoryId, notification.Group.Id);
            if (new_channel_id == null)
            {
                _logger.LogError("Could not create channel on {ServerId}.", defaultServerId);
                return;
            }

            foreach (var user in notification.Group.Members)
            {
                var discord_id = await _userService.GetDiscordId(user.Id);
                if (discord_id == null)
                {
                    continue;
                }
                await _discord.SetUserPermissionsOnChannel((ulong)new_channel_id, (ulong)discord_id);
            }

            _logger.LogInformation("Created new Discord channel {channelId} for group {groupId} in server {ServerId}", new_channel_id, notification.Group.Id, defaultServerId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating Discord channel for group {GroupId}", notification.Group.Id);
        }
    }
}

public class DeleteDiscordChannelOnGroupDisbanded : INotificationHandler<GroupDisbanded>
{
    private readonly DiscordService _discord;
    private readonly ILogger<DeleteDiscordChannelOnGroupDisbanded> _logger;

    public DeleteDiscordChannelOnGroupDisbanded(DiscordService discord, ILogger<DeleteDiscordChannelOnGroupDisbanded> logger)
    {
        _discord = discord;
        _logger = logger;
    }

    public async Task Handle(GroupDisbanded notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Group disbanded {groupId}", notification.GroupId.ToString());

        try
        {
            await _discord.DeleteGroupChannels(notification.GroupId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting Discord channels for group {GroupId}", notification.GroupId);
        }
    }
}
