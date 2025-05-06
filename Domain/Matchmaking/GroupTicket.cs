namespace QuickFinder.Domain.Matchmaking;

public class GroupTicket() : BaseEntity, ICandidate
{
    public Guid Id { get; init; }
    public required Group Group { get; init; }
    public required Course Course { get; init; }
    public required Preferences Preferences { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
