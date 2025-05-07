using Coravel.Events.Interfaces;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

public class NotifyUsersOnGroupFilled(UserService userService, GroupRepository groupRepository)
    : IListener<GroupFilled>
{
    public async Task HandleAsync(GroupFilled notification)
    {
        var groupId = notification.Group.Id;
        var groupName = notification.Group.Name;
        var group =
            await groupRepository.GetByIdAsync(groupId)
            ?? throw new Exception($"Group '{groupName}'({groupId}) not found");
        var groupMembers = group.Members;
        var courseName = group.Course.Name;

        var names = groupMembers.Select(m => m.UserName);

        var name_list = string.Join("", names.Select(name => $"- {name}(ID)\n"));

        foreach (var member in groupMembers)
        {
            await userService.NotifyUser(
                member,
                $"Group found for {courseName}.\n Your members: \n{name_list}"
            );
        }
    }
}

// TODO: remove demo event handler
public class OnUserDeleted : IListener<UserDeleted>
{
    public async Task HandleAsync(UserDeleted notification)
    {
        var id = notification.Id;
        Console.WriteLine("USER DELETED");

        await Task.CompletedTask;
    }
}
