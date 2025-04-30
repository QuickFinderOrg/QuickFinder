using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QuickFinder.Data;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Domain.DiscordDomain;

public class DiscordService : IHostedService
{
    private readonly DiscordServiceOptions _options;
    private readonly ILogger<DiscordService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _client;
    private readonly IQueue _queue;

    public DiscordService(
        IOptions<DiscordServiceOptions> options,
        ILogger<DiscordService> logger,
        IServiceProvider serviceProvider,
        DiscordSocketClient client,
        IQueue queue
    )
    {
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = client;
        _queue = queue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Starting DiscordService.");
        if (_options.IsEmpty)
        {
            _logger.LogWarning(
                "One or more DiscordService options are emptry. App will still run without Discord connection."
            );
            return;
        }

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

        _logger.LogInformation(
            "Discord bot recieved message from {name} ({id}) in {channel}\n {msg}",
            message.Author.GlobalName,
            message.Author.Id,
            message.Channel.Name,
            message.Content
        );

        return Task.CompletedTask;
    }

    private Task OnJoinedServer(SocketGuild socketServer)
    {
        _logger.LogInformation(
            "Discord bot invited to server {serverName} ({serverId}), owned by {username} {userId}",
            socketServer.Name,
            socketServer.Id,
            socketServer.Owner.GlobalName,
            socketServer.OwnerId
        );

        return Task.CompletedTask;
    }

    public void QueueSendDM(ulong userId, string message)
    {
        _queue.QueueInvocableWithPayload<SendDMInvocable, SendDMPayload>(
            new SendDMPayload { userId = userId, message = message }
        );
    }

    public async Task<bool> SendDM(ulong userId, string message)
    {
        ThrowIfNotConnected();
        var user = await _client.GetUserAsync(userId);
        if (user == null)
        {
            _logger.LogError("User {userId} not found.", userId);
            return false;
        }

        try
        {
            await user.SendMessageAsync(message);
            _logger.LogInformation(
                "Sent DM to {username} ({userid}): {message}",
                user.Username,
                userId,
                message
            );
            return true;
        }
        catch (Discord.Net.HttpException e)
        {
            _logger.LogError(
                e,
                "Failed to send DM to {username}: DiscordErrorCode: {DiscordErrorCode}",
                user.Username,
                e.DiscordCode
            );
            return false;
        }
    }

    public async Task<ulong?> CreateChannel(
        ulong serverId,
        string channelName,
        ulong? categoryId,
        Guid? owningGroup
    )
    {
        ThrowIfNotConnected();
        var server = _client.GetGuild(serverId);
        if (server == null)
        {
            return null;
        }

        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var serverDB =
                await dbContext.DiscordServers.FirstAsync(s => s.Id == serverId)
                ?? throw new Exception($"Server {_options.ServerId} not in db");

            var channel = await server.CreateTextChannelAsync(
                channelName,
                p => p.CategoryId = ulong.Parse(_options.GroupChannelCategoryId)
            );
            var channelDB = new Channel()
            {
                Id = channel.Id,
                CategoryId = ulong.Parse(_options.GroupChannelCategoryId),
                Server = serverDB,
                OwningGroupId = owningGroup,
            };
            dbContext.Add(channelDB);
            // await channel.SendMessageAsync("@everyone");

            _logger.LogInformation(
                "Created Discord channel '{name}' ({id})",
                channel.ToString(),
                channel.Id
            );
            await dbContext.SaveChangesAsync();
            return channel.Id;
        }
    }

    public async Task<ulong?> SetUserPermissionsOnChannel(ulong channelId, ulong userId)
    {
        ThrowIfNotConnected();
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

        var permissions = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );
        await discord_channel.AddPermissionOverwriteAsync(discord_user, permissions);

        return channelId;
    }

    public async Task<ulong?> DeleteUserPermissionsOnChannel(ulong channelId, ulong userId)
    {
        ThrowIfNotConnected();
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

        var permissions = new OverwritePermissions(
            viewChannel: PermValue.Deny,
            sendMessages: PermValue.Deny
        );
        await discord_channel.AddPermissionOverwriteAsync(discord_user, permissions);

        return channelId;
    }

    public async Task<ulong?> DeleteChannel(ulong channelId)
    {
        ThrowIfNotConnected();
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
        ThrowIfNotConnected();
        var server = _client.GetGuild(ulong.Parse(_options.ServerId));
        if (server == null)
        {
            return [];
        }

        var channels = server.TextChannels.ToList();

        var discord_channels = channels
            .Select(channel => new DiscordChannel()
            {
                Id = channel.Id,
                Name = channel.Name,
                Category = channel.Category?.Name,
            })
            .ToArray();

        return discord_channels;
    }

    public async Task<DiscordServerItem[]> GetServerList()
    {
        ThrowIfNotConnected();
        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var servers = await dbContext.DiscordServers.ToArrayAsync();
            return servers
                .Select(s => new DiscordServerItem() { Id = s.Id, Name = s.Name })
                .ToArray();
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

            var servers = await dbContext
                .DiscordServers.Include(server => server.Courses)
                .Where(server => server.Courses.Contains(course))
                .ToArrayAsync();
            return servers
                .Select(s => new DiscordServerItem() { Id = s.Id, Name = s.Name })
                .ToArray();
        }
    }

    public async Task<bool> AddCourseServer(Guid courseId, ulong serverId)
    {
        ThrowIfNotConnected();
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

            var server = await dbContext
                .DiscordServers.Include(server => server.Courses)
                .FirstOrDefaultAsync(server => server.Id == serverId);
            if (server == null)
            {
                _logger.LogError("Server {serverId} not found", serverId);
                return false;
            }

            server.Courses.Add(course);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Added server {serverId} to course {courseId}",
                serverId,
                courseId
            );
            return true;
        }
    }

    public DiscordServerItem[] GetBotServers()
    {
        ThrowIfNotConnected();
        var servers = _client
            .Guilds.Select(server => new DiscordServerItem() { Id = server.Id, Name = server.Name })
            .ToArray();
        return servers;
    }

    public DiscordServerItem[] GetServersOwnedByUser(ulong ownerDiscordId)
    {
        ThrowIfNotConnected();
        var servers = _client
            .Guilds.Where(server => server.OwnerId == ownerDiscordId)
            .Select(server => new DiscordServerItem() { Id = server.Id, Name = server.Name })
            .ToArray();
        return servers;
    }

    /// <summary>
    /// Get servers that are owned by the user and are not already in the database.
    /// </summary>
    /// <param name="ownerDiscordId"></param>
    /// <returns></returns>
    public DiscordServerItem[] GetServersThatCanBeAdded(ulong ownerDiscordId)
    {
        ThrowIfNotConnected();
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existing_server_ids = db.DiscordServers.Select(server => server.Id).ToArray();

        var servers = _client
            .Guilds.Where(server => server.OwnerId == ownerDiscordId)
            .Where(server => !existing_server_ids.Contains(server.Id))
            .Select(server => new DiscordServerItem() { Id = server.Id, Name = server.Name })
            .ToArray();
        return servers;
    }

    public async Task AddServer(ulong serverId)
    {
        ThrowIfNotConnected();
        // TODO: check ownership before adding it.
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingServer = await dbContext.DiscordServers.FindAsync(serverId);
        if (existingServer != null)
        {
            throw new InvalidOperationException(
                $"Server {serverId} already exists in the database."
            );
        }

        var socket_server =
            _client.GetGuild(serverId)
            ?? throw new Exception(
                $"Server {serverId} not found in Discord or Bot is not in the server."
            );

        var server_name = socket_server.Name;
        var new_server = new Server { Id = serverId, Name = server_name };

        dbContext.DiscordServers.Add(new_server);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Added new server {serverId} ({serverName}) to the database.",
            serverId,
            server_name
        );
    }

    public async Task EnsureCourseServer(Guid courseId, ulong serverId)
    {
        ThrowIfNotConnected();
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
            var socket_server =
                _client.GetGuild(serverId)
                ?? throw new Exception(
                    $"Server {serverId} not found in Discord or Bot is not in the server."
                );
            server = new Server { Id = serverId, Name = socket_server.Name };
            db.Add(server);
        }

        if (!server.Courses.Contains(course))
        {
            server.Courses.Add(course);
            await db.SaveChangesAsync();
            _logger.LogInformation(
                "Added server {serverId} to course {courseId}",
                serverId,
                courseId
            );
        }
    }

    public async Task DeleteGroupChannels(Guid groupId)
    {
        ThrowIfNotConnected();
        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var groupRepository = scope.ServiceProvider.GetRequiredService<GroupRepository>();
            var group = await groupRepository.GetGroup(groupId);
            var courseServer = await GetCourseServer(group.Course.Id);

            var server = _client.GetGuild(courseServer[0].Id);
            if (server == null)
            {
                return;
            }

            var channels = await dbContext
                .DiscordChannels.Where(ch => ch.OwningGroupId == groupId)
                .ToArrayAsync();

            foreach (var channel in channels)
            {
                await DeleteChannel(channel.Id);
            }
        }
    }

    public async Task DeleteUnusedGroupChannels()
    {
        ThrowIfNotConnected();
        // Create a new scope to resolve the DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var servers = await dbContext.DiscordServers.Include(s => s.Courses).ToArrayAsync();
            foreach (var server in servers)
            {
                var course =
                    await dbContext
                        .Courses.Include(c => c.Groups)
                        .SingleOrDefaultAsync(c => c.Id == server.Courses[0].Id)
                    ?? throw new Exception("Course not found.");

                var groups = dbContext.Groups.Where(g => g.Course.Id == course.Id).ToArray();

                var channels = await dbContext
                    .DiscordChannels.Where(c => c.Server.Id == server.Id)
                    .ToArrayAsync();

                foreach (var channel in channels)
                {
                    if (!course.Groups.Any(g => g.Id == channel.OwningGroupId))
                    {
                        await DeleteChannel(channel.Id);
                    }
                }
            }
        }
    }

    public async Task InviteToServer(ulong userId, string accessToken, ulong serverId)
    {
        ThrowIfNotConnected();
        var guild = _client.GetGuild(serverId);

        await AddGuildMember(_options.BotToken, guild.Id, userId, accessToken);
    }

    public static async Task AddGuildMember(
        string botToken,
        ulong guildId,
        ulong userId,
        string accessToken
    )
    {
        var client = new HttpClient();
        var uri =
            $"https://discord.com/api/guilds/{guildId}/members/{userId}?scope=bot%20guilds.join";

        var properties = new { access_token = accessToken };

        var json = JsonConvert.SerializeObject(properties);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", botToken);
        var response = await client.PutAsync(uri, data);

        if (!response.IsSuccessStatusCode)
        {
            //Log
            Console.WriteLine($"Error joining guild. Status code: {response.StatusCode}");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
        else
        {
            //Log to know it was a success.
            Console.WriteLine("Sucess! user id was sent over");
        }
    }

    public string InviteURL =>
        $"https://discord.com/oauth2/authorize?client_id={_options.ClientId}";

    private void ThrowIfNotConnected()
    {
        if (_client.ConnectionState != ConnectionState.Connected)
            throw new Exception("");
    }

    public bool IsEnabled => _options.IsEmpty == false;
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

    public bool IsEmpty =>
        string.IsNullOrEmpty(BotToken)
        | string.IsNullOrEmpty(ClientSecret)
        | string.IsNullOrEmpty(ClientId)
        | string.IsNullOrEmpty(ServerId)
        | string.IsNullOrEmpty(GroupChannelCategoryId);
}

public class CreateDiscordChannelOnGroupFilled : INotificationHandler<GroupFilled>
{
    private readonly IOptions<DiscordServiceOptions> _options;
    private readonly DiscordService _discord;
    private readonly UserService _userService;
    private readonly ILogger<NotifyUsersOnGroupFilled> _logger;

    public CreateDiscordChannelOnGroupFilled(
        IOptions<DiscordServiceOptions> options,
        DiscordService discord,
        UserService userService,
        ILogger<NotifyUsersOnGroupFilled> logger
    )
    {
        _options = options;
        _discord = discord;
        _userService = userService;
        _logger = logger;
    }

    public async Task Handle(GroupFilled notification, CancellationToken cancellationToken)
    {
        try
        {
            if (_discord.IsEnabled == false)
            {
                return;
            }
            _logger.LogInformation("Group filled {groupId}", notification.Group.Id);
            var defaultServerId = ulong.Parse(_options.Value.ServerId);
            var defaultCategoryId = ulong.Parse(_options.Value.GroupChannelCategoryId);
            var channelName = notification.Group.Name;

            var new_channel_id = await _discord.CreateChannel(
                defaultServerId,
                channelName,
                defaultCategoryId,
                notification.Group.Id
            );
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
                await _discord.SetUserPermissionsOnChannel(
                    (ulong)new_channel_id,
                    (ulong)discord_id
                );
            }

            _logger.LogInformation(
                "Created new Discord channel {channelId} for group {groupId} in server {ServerId}",
                new_channel_id,
                notification.Group.Id,
                defaultServerId
            );
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error creating Discord channel for group {GroupId}",
                notification.Group.Id
            );
        }
    }
}

public class DeleteDiscordChannelOnGroupDisbanded : INotificationHandler<GroupDisbanded>
{
    private readonly DiscordService _discord;
    private readonly ILogger<DeleteDiscordChannelOnGroupDisbanded> _logger;

    public DeleteDiscordChannelOnGroupDisbanded(
        DiscordService discord,
        ILogger<DeleteDiscordChannelOnGroupDisbanded> logger
    )
    {
        _discord = discord;
        _logger = logger;
    }

    public async Task Handle(GroupDisbanded notification, CancellationToken cancellationToken)
    {
        try
        {
            if (_discord.IsEnabled == false)
            {
                return;
            }
            _logger.LogInformation("Group disbanded {groupId}", notification.GroupId.ToString());

            await _discord.DeleteGroupChannels(notification.GroupId);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error deleting Discord channels for group {GroupId}",
                notification.GroupId
            );
        }
    }
}

public class DeleteUserPermissionsOnGroupMemberLeft : INotificationHandler<GroupMemberLeft>
{
    private readonly DiscordService _discord;
    private readonly ILogger<DeleteUserPermissionsOnGroupMemberLeft> _logger;
    private readonly UserService _userService;

    public DeleteUserPermissionsOnGroupMemberLeft(
        DiscordService discord,
        ILogger<DeleteUserPermissionsOnGroupMemberLeft> logger,
        UserService userService
    )
    {
        _discord = discord;
        _logger = logger;
        _userService = userService;
    }

    public async Task Handle(GroupMemberLeft notification, CancellationToken cancellationToken)
    {
        try
        {
            if (_discord.IsEnabled == false)
            {
                return;
            }
            _logger.LogInformation(
                $"Group member {notification.User.Id} left",
                notification.User.Id.ToString()
            );

            var channel =
                _discord.GetChannels().SingleOrDefault(c => c.Name == notification.Group.Name)
                ?? throw new Exception($"Channel {notification.Group.Id} not found.");

            var discord_id = await _userService.GetDiscordId(notification.User.Id);

            await _discord.DeleteUserPermissionsOnChannel(channel.Id, (ulong)discord_id);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error deleting Discord channels for user {userId}",
                notification.User
            );
        }
    }
}

public class InviteToServerOnCourseJoined(
    DiscordService discord,
    ILogger<InviteToServerOnCourseJoined> logger,
    UserManager<User> userManager,
    IOptions<DiscordServiceOptions> options
) : INotificationHandler<CourseJoined>
{
    private readonly DiscordService _discord = discord;
    private readonly ILogger<InviteToServerOnCourseJoined> _logger = logger;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IOptions<DiscordServiceOptions> _options = options;

    public async Task Handle(CourseJoined notification, CancellationToken cancellationToken)
    {
        try
        {
            if (_discord.IsEnabled == false)
            {
                return;
            }
            _logger.LogInformation(
                $"User {notification.User.Id} joined course {notification.Course.Id}"
            );
            var claims = await _userManager.GetClaimsAsync(notification.User);
            var c = new List<Claim>(claims);

            var discordIdClaim =
                c.Find(c => c.Type == ApplicationClaimTypes.DiscordId)
                ?? throw new Exception("DiscordId claim not found");
            var discordTokenClaim =
                c.Find(c => c.Type == ApplicationClaimTypes.DiscordToken)
                ?? throw new Exception("DiscordId claim not found");

            var server = await _discord.GetCourseServer(notification.Course.Id);
            if (server.Length == 0)
            {
                await _discord.InviteToServer(
                    ulong.Parse(discordIdClaim.Value),
                    discordTokenClaim.Value,
                    ulong.Parse(_options.Value.ServerId)
                );
            }
            else
            {
                await _discord.InviteToServer(
                    ulong.Parse(discordIdClaim.Value),
                    discordTokenClaim.Value,
                    server[0].Id
                );
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error inviting user {userId} to server for course {courseId}",
                notification.User.Id,
                notification.Course.Id
            );
        }
    }
}

public record class SendDMPayload
{
    public ulong userId;
    public required string message;
}

public class SendDMInvocable(DiscordService discordService)
    : IInvocable,
        IInvocableWithPayload<SendDMPayload>
{
    public required SendDMPayload Payload { get; set; }

    public async Task Invoke()
    {
        if (discordService.IsEnabled == false)
        {
            return;
        }

        await discordService.SendDM(Payload.userId, Payload.message);
    }
}
