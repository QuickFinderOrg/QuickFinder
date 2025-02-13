using MediatR;

namespace group_finder.Domain.Matchmaking;

public class GroupMemberAdded : INotification
{
    public required User UserId;
    public required Group GroupId;
    public DateTimeOffset DateOccurred { get; protected set; } = DateTimeOffset.UtcNow;
}