using Coravel.Events.Interfaces;
using Microsoft.Extensions.Options;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Domain.DiscordDomain;

public class CreateDiscordChannelOnGroupFilled(
    IOptions<DiscordServiceOptions> options,
    DiscordService discord,
    UserService userService,
    ILogger<CreateDiscordChannelOnGroupFilled> logger,
    GroupRepository groupRepository,
    CourseRepository courseRepository
) : IListener<GroupFilled>
{
    public async Task HandleAsync(GroupFilled notification)
    {
        try
        {
            logger.LogInformation("CREATE DISCORD CHANNEL");
            if (discord.IsEnabled == false)
            {
                return;
            }
            var groupId = notification.Group.Id;
            var groupName = notification.Group.Name;

            var group =
                await groupRepository.GetByIdAsync(groupId)
                ?? throw new Exception($"Group '{groupName}' not found");

            var groupMembers = group.Members;

            var servers = await discord.GetCourseServer(group.Course.Id);
            var server = servers.FirstOrDefault();

            logger.LogInformation("Group filled {name}", groupName);
            var defaultServerId = server?.Id ?? ulong.Parse(options.Value.ServerId);
            var defaultCategoryId = ulong.Parse(options.Value.GroupChannelCategoryId);
            var channelName = notification.Group.Name;

            var new_channel_id = await discord.CreateChannel(
                defaultServerId,
                channelName,
                defaultCategoryId,
                notification.Group.Id
            );
            if (new_channel_id == null)
            {
                logger.LogError("Could not create channel on {ServerId}.", defaultServerId);
                return;
            }

            foreach (var user in groupMembers)
            {
                var discord_id = await userService.GetDiscordId(user.Id);
                if (discord_id == null)
                {
                    continue;
                }
                await discord.SetUserPermissionsOnChannel((ulong)new_channel_id, (ulong)discord_id);
            }

            logger.LogInformation(
                "Created new Discord channel {channelId} for group {groupId} in server {ServerId}",
                new_channel_id,
                notification.Group.Id,
                defaultServerId
            );
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error creating Discord channel for group {GroupId}",
                notification.Group.Id
            );
        }
    }
}

public class DeleteDiscordChannelOnGroupDisbanded(
    DiscordService discord,
    ILogger<DeleteDiscordChannelOnGroupDisbanded> logger
) : IListener<GroupDisbanded>
{
    public async Task HandleAsync(GroupDisbanded notification)
    {
        var groupId = notification.Group.Id;
        var groupName = notification.Group.Name;

        try
        {
            if (discord.IsEnabled == false)
            {
                return;
            }

            logger.LogInformation("Group disbanded {name}({id})", groupName, groupId);

            await discord.DeleteGroupChannels(groupId);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error deleting Discord channels for group {name}({id})",
                groupName,
                groupId
            );
        }
    }
}

public class DeleteUserPermissionsOnGroupMemberLeft(
    DiscordService discord,
    ILogger<DeleteUserPermissionsOnGroupMemberLeft> logger,
    UserService userService
) : IListener<GroupMemberLeft>
{
    public async Task HandleAsync(GroupMemberLeft notification)
    {
        try
        {
            if (discord.IsEnabled == false)
            {
                return;
            }
            logger.LogInformation(
                $"Group member {notification.User.Id} left",
                notification.User.Id.ToString()
            );

            var channel =
                discord.GetChannels().SingleOrDefault(c => c.Name == notification.Group.Name)
                ?? throw new Exception($"Channel {notification.Group.Id} not found.");

            var discord_id = await userService.GetDiscordId(notification.User.Id);

            await discord.DeleteUserPermissionsOnChannel(channel.Id, (ulong)discord_id);
        }
        catch (Exception e)
        {
            logger.LogError(
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
    UserService userService,
    IOptions<DiscordServiceOptions> options
) : IListener<CourseJoined>
{
    public async Task HandleAsync(CourseJoined notification)
    {
        var userId = notification.User.Id;
        var userName = notification.User.Name;
        try
        {
            if (discord.IsEnabled == false)
            {
                return;
            }
            logger.LogInformation(
                $"User {notification.User.Id} joined course {notification.Course.Id}"
            );
            var user =
                await userService.GetUser(userId)
                ?? throw new Exception($"User '{userName}'({userId}) not found");
            var discordId = await userService.GetDiscordId(userId);
            var discordToken = await userService.GetDiscordToken(user);

            if (discordId is null || string.IsNullOrWhiteSpace(discordToken))
            {
                logger.LogTrace(
                    "User '{name}'({id}) does not have Discord connected",
                    userName,
                    userId
                );
                return;
            }

            var server = await discord.GetCourseServer(notification.Course.Id);
            await discord.InviteToServer(
                (ulong)discordId,
                discordToken,
                server.First()?.Id ?? ulong.Parse(options.Value.ServerId)
            );
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error inviting user {userId} to server for course {courseId}",
                notification.User.Id,
                notification.Course.Id
            );
        }
    }
}
