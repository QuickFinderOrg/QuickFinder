using MediatR;

namespace group_finder.Domain.Matchmaking;

public class GroupMemberAdded : INotification
{
    public required User User;
    public required Group Group;
}

public class GroupMemberLeft : INotification
{
    public required User User;
    public required Group Group;
}

public class GroupDisbanded : INotification
{
    public required Group Group; // TODO: group won't exist anymore at this point. how to handle this?
}

public class GroupFilled : INotification
{
    public required Group Group;
}