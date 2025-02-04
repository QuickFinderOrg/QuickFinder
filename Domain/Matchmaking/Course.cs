namespace group_finder.Domain.Matchmaking;

public class Course()
{
    public Guid Id { get; init; }
    public required string Name { get; set; } = "Course";
    public List<Group> Groups { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];
    public uint GroupSize { get; set; } = 2;
}