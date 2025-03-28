namespace QuickFinder.Domain.DiscordDomain;

public class Channel : BaseEntity
{
    public required ulong Id { get; init; }
    public required Server Server { get; init; }
    public ulong CategoryId { get; init; }

    public Guid? OwningGroupId { get; init; }
}
