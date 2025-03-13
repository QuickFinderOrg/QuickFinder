using group_finder.Domain.Matchmaking;

namespace group_finder;

public class Channel
{
    public ulong Id { get; init; }
    public ulong ServerId { get; init; }

    public required Group OwningGroup { get; init; }
}
