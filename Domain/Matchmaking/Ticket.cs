namespace group_finder.Domain.Matchmaking;

public class Ticket()
{
    public Guid Id { get; init; }
    public required User User { get; init; }
    public required Course Course { get; init; }


    public bool WillAcceptGroup(Group group)
    {
        if (group.IsFull)
        {
            return false;
        }
        if (group.Preferences.Availability != User.Preferences.Availability)
        {
            return false;
        }

        return true;
    }
}

