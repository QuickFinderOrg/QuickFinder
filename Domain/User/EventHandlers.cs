using MediatR;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

public class NotifyUsersOnGroupFilled(UserService userService) : INotificationHandler<GroupFilled>
{
    public async Task Handle(GroupFilled notification, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        foreach (var member in notification.Group.Members)
        {
            var name = await userService.GetName(member);
            names.Add(name);
        }

        var name_list = string.Join("", names.Select(name => $"- {name}(ID)\n"));

        foreach (var member in notification.Group.Members)
        {
            await userService.NotifyUser(
                member,
                $"Group found for {notification.Group.Course.Name}.\n Your members: \n{name_list}"
            );
        }
    }
}
