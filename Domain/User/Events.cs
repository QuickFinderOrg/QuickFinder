using MediatR;

namespace group_finder;

public class UserToBeDeleted : INotification
{
    public required User User;
}
