namespace group_finder.Domain.Matchmaking;

public class Group()
{
    public Guid Id { get; init; }
    public List<User> Members { get; } = [];
    public Course Course = null!;
    public required Criteria Criteria { get; init; }
    public required Preferences Preferences { get; init; }
    public uint GroupLimit { get; set; } = 2;

    public bool IsFull => Members.Count >= GroupLimit;
}