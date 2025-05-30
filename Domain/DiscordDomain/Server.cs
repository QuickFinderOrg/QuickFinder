using QuickFinder.Domain.Matchmaking;

namespace QuickFinder.Domain.DiscordDomain;

public class Server : BaseEntity
{
    public ulong Id { get; init; }

    /// <summary>
    /// Which category to put new channels into
    /// </summary>
    public ulong CategoryId { get; set; }
    public required string Name { get; set; }
    public List<Course> Courses { get; init; } = [];
}
