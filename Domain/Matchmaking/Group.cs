namespace group_finder.Domain.Matchmaking;

public class Group()
{
    public Guid Id { get; init; }
    public List<User> Members { get; } = [];
    public required Course Course { get; init; }
    public required Preferences Preferences { get; init; }
    public uint GroupLimit { get; set; } = 2;

    // Set after the group has achived it's desired amount of members. Never reset.
    public bool IsComplete { get; set; } = false;

    public bool IsFull => Members.Count >= GroupLimit;
}