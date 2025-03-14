using group_finder.Domain.Matchmaking;

namespace group_finder;

public class Channel
{
    public ulong Id { get; init; }
    public required Server Server { get; init; }
    public ulong CategoryId { get; init; }

    public required Group OwningGroup { get; init; }
}
