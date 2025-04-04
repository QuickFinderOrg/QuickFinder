
namespace QuickFinder.Domain.Matchmaking;

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