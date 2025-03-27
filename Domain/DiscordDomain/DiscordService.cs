using Discord;
using Discord.WebSocket;
using group_finder.Data;
using group_finder.Domain.Matchmaking;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace group_finder.Domain.DiscordDomain;

public class DiscordService : IHostedService
{
    private readonly DiscordServiceOptions _options;
    private readonly ILogger<DiscordService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _client;

    public DiscordService(
        IOptions<DiscordServiceOptions> options,
        ILogger<DiscordService> logger,
        IServiceProvider serviceProvider,
        DiscordSocketClient client)
    {
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Starting DiscordService.");
        await _client.LoginAsync(TokenType.Bot, _options.BotToken);
        _logger.LogTrace("Logged in to DiscordService.");
        await _client.StartAsync();
        _logger.LogInformation("DiscordService started.");
        _client.MessageReceived += OnMessageRecieved;
        _client.JoinedGuild += OnJoinedServer;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Stopping DiscordService.");
        await _client.LogoutAsync();
        _logger.LogTrace("Logged out of DiscordService.");
        await _client.StopAsync();
        _logger.LogInformation("DiscordService stopped.");
        _client.MessageReceived -= OnMessageRecieved;
        _client.JoinedGuild -= OnJoinedServer;
    }

    private Task OnMessageRecieved(SocketMessage message)
    {
        if (message.Author.IsBot || message.Author.IsWebhook)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Discord bot recieved message from {name} ({id}) in {channel}\n {msg}", message.Author.GlobalName, message.Author.Id, message.Channel.Name, message.Content);

        return Task.CompletedTask;
    }

    private Task OnJoinedServer(SocketGuild socketServer)
    {

        _logger.LogInformation("Discord bot invited to server {serverName} ({serverId}), owned by {username} {userId}", socketServer.Name, socketServer.Id, socketServer.Owner.GlobalName, socketServer.OwnerId);

        return Task.CompletedTask;
    }

    public async Task<bool> SendDM(ulong userId, string message)
    {
        var user = await _client.GetUserAsync(userId);
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
        var server = _client.GetGuild(serverId);
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
        var server = _client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            _logger.LogError("Server not found '{userId}'", userId);
            return null;
        }

        var discord_user = await _client.GetUserAsync(userId);

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
        var server = _client.GetGuild(ulong.Parse(_options.ServerId));
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

    public async Task<DiscordServerItem[]> GetCourseServer(Guid courseId)
    {
        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = await dbContext.Courses.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogError("Course {courseId} not found", courseId);
                return [];
            }

            var servers = await dbContext.DiscordServers.Include(server => server.Courses).Where(server => server.Courses.Contains(course)).ToArrayAsync();
            return servers.Select(s => new DiscordServerItem() { Id = s.Id, Name = s.Name }).ToArray();
        }
    }

    public async Task<bool> AddCourseServer(Guid courseId, ulong serverId)
    {
        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var course = await dbContext.Courses.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogError("Course {courseId} not found", courseId);
                return false;
            }

            var server = await dbContext.DiscordServers.Include(server => server.Courses).FirstOrDefaultAsync(server => server.Id == serverId);
            if (server == null)
            {
                _logger.LogError("Server {serverId} not found", serverId);
                return false;
            }

            server.Courses.Add(course);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Added server {serverId} to course {courseId}", serverId, courseId);
            return true;
        }
    }

    public DiscordServerItem[] GetBotServers()
    {
        var servers = _client.Guilds.Select(server => new DiscordServerItem() { Id = server.Id, Name = server.Name }).ToArray();
        return servers;
    }

    public DiscordServerItem[] GetServersOwnedByUser(ulong ownerDiscordId)
    {
        var servers = _client.Guilds.Where(server => server.OwnerId == ownerDiscordId).Select(server => new DiscordServerItem() { Id = server.Id, Name = server.Name }).ToArray();
        return servers;
    }
    /// <summary>
    /// Get servers that are owned by the user and are not already in the database.
    /// </summary>
    /// <param name="ownerDiscordId"></param>
    /// <returns></returns>
    public DiscordServerItem[] GetServersThatCanBeAdded(ulong ownerDiscordId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existing_server_ids = db.DiscordServers.Select(server => server.Id).ToArray();

        var servers = _client.Guilds
                        .Where(server => server.OwnerId == ownerDiscordId)
                        .Where(server => !existing_server_ids.Contains(server.Id))
                        .Select(server => new DiscordServerItem() { Id = server.Id, Name = server.Name })
                        .ToArray();
        return servers;
    }

    public async Task AddServer(ulong serverId)
    {
        // TODO: check ownership before adding it.
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingServer = await dbContext.DiscordServers.FindAsync(serverId);
        if (existingServer != null)
        {
            throw new InvalidOperationException($"Server {serverId} already exists in the database.");
        }

        var socket_server = _client.GetGuild(serverId) ?? throw new Exception($"Server {serverId} not found in Discord or Bot is not in the server.");

        var server_name = socket_server.Name;
        var new_server = new Server
        {
            Id = serverId,
            Name = server_name
        };

        dbContext.DiscordServers.Add(new_server);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Added new server {serverId} ({serverName}) to the database.", serverId, server_name);
    }

    public async Task EnsureCourseServer(Guid courseId, ulong serverId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var course = await db.Courses.FindAsync(courseId);
        if (course == null)
        {
            throw new InvalidOperationException($"Course {courseId} not found.");
        }

        var server = await db.DiscordServers.FindAsync(serverId);
        if (server == null)
        {
            var socket_server = _client.GetGuild(serverId) ?? throw new Exception($"Server {serverId} not found in Discord or Bot is not in the server.");
            server = new Server { Id = serverId, Name = socket_server.Name };
            db.Add(server);
        }

        if (!server.Courses.Contains(course))
        {
            server.Courses.Add(course);
            await db.SaveChangesAsync();
            _logger.LogInformation("Added server {serverId} to course {courseId}", serverId, courseId);
        }

    }

    public async Task DeleteGroupChannels(Guid groupId)
    {
        var server = _client.GetGuild(ulong.Parse(_options.ServerId));
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

    public string InviteURL => $"https://discord.com/oauth2/authorize?client_id={_options.ClientId}";
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
