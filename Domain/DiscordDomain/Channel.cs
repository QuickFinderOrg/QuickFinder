using group_finder.Domain.Matchmaking;

namespace group_finder.Domain.DiscordDomain;

public class Channel
{
    public required ulong Id { get; init; }
    public required Server Server { get; init; }
    public ulong CategoryId { get; init; }

    public Guid? OwningGroupId { get; init; }
}
