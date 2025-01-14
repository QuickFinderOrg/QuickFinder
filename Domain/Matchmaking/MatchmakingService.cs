using group_finder.Data;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class MatchmakingService(ApplicationDbContext db)
{
    public Group? LookForMatch(Person personToMatch, Group[] groups)
    {
        // other groups do exist

        foreach (var group in groups)
        {
            // is this the group for me?
            if (group.WillAcceptNewMember(personToMatch))
            {
                return group;
            }

            // no: keep looking
        }

        // no groups, make a new one.
        // or could not fit into any groups, make new one.
        return null;
    }

    public async void DoMatching()
    {
        // needs a queue of people waiting to match
        var waitlist = await db.WaitingPeople.Include(c => c.Person).ToArrayAsync() ?? throw new Exception("WAITLIST");
        foreach (var waiting in waitlist)
        {
            var groups = await db.Groups.ToArrayAsync();
            var foundGroup = LookForMatch(waiting.Person, groups);
            if (foundGroup != null)
            {
                // add to group
            }
            else
            {
                // create new group
            }
        }
    }

}