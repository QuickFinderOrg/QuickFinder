using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class Person()
{
    public Guid Id { get; init; }
    public required User User { get; set; }


    public bool WillAcceptGroup(Group group)
    {
        if (group.IsFull)
        {
            return false;
        }
        if (group.Criteria.Availability != User.Criteria.Availability)
        {
            return false;
        }

        return true;
    }
}

