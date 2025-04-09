namespace QuickFinder.Domain.Matchmaking;

public class Ticket() : BaseEntity, ICandidate
{
    public Guid Id { get; init; }
    public required User User { get; init; }
    public required Course Course { get; init; }
    public required Preferences Preferences { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public bool WillAcceptGroup(Group group)
    {
        if (group.IsFull)
        {
            return false;
        }
        if (group.Preferences.Availability != Preferences.Availability)
        {
            return false;
        }

        return true;
    }
}
