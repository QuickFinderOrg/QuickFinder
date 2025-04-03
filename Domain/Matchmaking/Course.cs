namespace QuickFinder.Domain.Matchmaking;

public class Course() : BaseEntity
{
    public Guid Id { get; init; }
    public required string Name { get; set; } = "Course";
    public List<Group> Groups { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];
    public uint GroupSize { get; set; } = 2;
    public bool AllowCustomSize { get; set; } = false;
    public List<User> Members { get; set; } = [];

    public IEnumerable<CoursePreferences> CoursePreferences { get; set; } = null!;
}