using group_finder.Domain.Matchmaking;

namespace group_finder;

public class Server
{
    public ulong Id { get; init; }
    public required Course Course { get; init; }
}
