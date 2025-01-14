namespace group_finder.Domain.Matchmaking;

public class Group()
{
    public Guid Id { get; private set; }
    public List<Person> Members { get; set; } = [];
    public uint GroupLimit { get; set; } = 5;
}