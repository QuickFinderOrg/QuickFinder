using MediatR;

namespace QuickFinder.Domain.Matchmaking;

public record class GroupMemberAdded : BaseDomainEvent
{
    public required User User;
    public required Group Group;
}

public record class GroupMemberLeft : BaseDomainEvent
{
    public required User User;
    public required Group Group;
}

public record class GroupDisbanded : BaseDomainEvent
{
    public required Guid GroupId;
    public required Course Course;
    public required User[] Members;
}

/// <summary>
/// Fired only the first time a group is filled
/// </summary>
public record class GroupFilled : BaseDomainEvent
{
    public required Group Group;
}

public record class GroupEmpty : BaseDomainEvent
{
    public required Guid GroupId;
}