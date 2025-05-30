namespace QuickFinder.Domain.Matchmaking;

public class Group() : BaseEntity
{
    public Guid Id { get; init; }
    public string Name { get; set; } = "";
    public List<User> Members { get; } = [];
    public required Course Course { get; init; }
    public required Preferences Preferences { get; init; }
    public uint GroupLimit { get; set; } = 2;

    // Set after the group has achived it's desired amount of members. Never reset.
    public bool IsComplete { get; set; } = false;

    public bool AllowAnyone { get; set; } = false;

    public bool IsFull => Members.Count >= GroupLimit;
}

public enum GroupMemberRemovedReason
{
    None,
    UserChoice,
    UserAccountDeleted,
    UserRemovedByAdmin,
}
