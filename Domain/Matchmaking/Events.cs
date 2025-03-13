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
    public required Guid GroupId;
    public required Course Course;
    public required User[] Members;
}

/// <summary>
/// Fired only the first time a group is filled
/// </summary>
public class GroupFilled : INotification
{
    public required Group Group;
}

public class GroupEmpty : INotification
{
    public required Guid GroupId;
}