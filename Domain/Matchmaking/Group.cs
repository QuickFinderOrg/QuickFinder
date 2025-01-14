namespace group_finder.Domain.Matchmaking;

public class Group()
{
    public Guid Id { get; init; }
    public List<Person> Members { get; set; } = [];
    public uint GroupLimit { get; set; } = 5;

    public bool WillAcceptNewMember(Person person)
    {
        foreach (var member in Members)
        {
            if (person.Criteria.Availability != member.Criteria.Availability)
            {
                return false;
            }
        }
        return true;
    }
}