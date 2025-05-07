using MediatR;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

public class NotifyUsersOnGroupFilled(UserService userService, GroupRepository groupRepository)
    : INotificationHandler<GroupFilled>
{
    public async Task Handle(GroupFilled notification, CancellationToken cancellationToken)
    {
        var groupId = notification.Group.Id;
        var groupName = notification.Group.Name;
        var group =
            await groupRepository.GetByIdAsync(groupId, cancellationToken)
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
