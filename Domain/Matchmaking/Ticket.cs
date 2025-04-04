namespace QuickFinder.Domain.Matchmaking;

public class Ticket() : BaseEntity
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


public interface ICandidate
{
    public Guid Id { get; init; }
    public IPreferences Preferences { get; init; }
    /// <summary>
    /// For calculating the time in queue
    /// </summary>
    public DateTime CreatedAt { get; init; }

    public TimeSpan TimeInQueue => DateTime.UtcNow - CreatedAt;
}

public record class TestCandidate : ICandidate
{
    public Guid Id { get; init; }
    public required IPreferences Preferences { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}