namespace group_finder.Domain.Matchmaking;

public class Group()
{
    public Guid Id { get; init; }
    public List<Guid> Members { get; set; } = [];
    public required Criteria Criteria { get; init; }
    public required Preferences Preferences { get; init; }
    public uint GroupLimit { get; set; } = 2;

    public bool WillAcceptNewMember(Person person)
    {
        if (Members.Count == GroupLimit)
        {
            return false;
        }
        if (person.Criteria.Availability != Criteria.Availability)
        {
            return false;
        }

        return true;
    }

    public bool IsComplete => Members.Count == GroupLimit;
}