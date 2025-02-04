using group_finder.Data;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class MatchmakingService(ApplicationDbContext db)
{
    public Group? LookForMatch(Ticket ticket, Group[] groups)
    {
        // other groups do exist

        foreach (var group in groups)
        {
            // is this the group for me?
            if (ticket.WillAcceptGroup(group))
            {
                return group;
            }

            // no: keep looking
        }

        // no groups, make a new one.
        // or could not fit into any groups, make new one.
        return null;
    }

    public async Task DoMatching()
    {
        // needs a queue of people waiting to match
        var waitlist = await db.Tickets.Include(p => p.User).ToArrayAsync() ?? throw new Exception("WAITLIST");
        foreach (var ticket in waitlist)
        {
            var groups = await db.Groups.Include(c => c.Members).ToArrayAsync();
            var foundGroup = LookForMatch(ticket, groups);
            if (foundGroup != null)
            {
                // add to group
                foundGroup.Members.Add(ticket.User);
            }
            else
            {
                // create new group
                var group = new Group() { Criteria = ticket.User.Criteria, Preferences = ticket.User.Preferences };
                group.Members.Add(ticket.User);
                db.Add(group);
            }

            // remove from waiting list

            db.Remove(ticket);
            await db.SaveChangesAsync(); // TODO: make more efficient
        }

    }

    public async Task AddToWaitlist(User user, Course course)
    {
        var existing = await db.Tickets.Include(c => c.User).Include(c => c.Course).Where(t => t.User == user && t.Course == course).ToArrayAsync();

        if (existing.Length != 0)
        {
            throw new Exception("User is already queued up for this course.");
        }

        db.Add(new Ticket() { User = user, Course = course });
        await db.SaveChangesAsync();
    }

    public async Task<Course[]> GetCourses()
    {
        return await db.Courses.ToArrayAsync();
    }

    public async Task<Group[]> GetGroups(User user)
    {
        return await db.Groups.Where(g => g.Members.Contains(user)).Include(g => g.Members).ToArrayAsync();
    }

    public async Task Reset()
    {
        var waitlist = await db.Tickets.Include(p => p.User).ToArrayAsync();
        db.RemoveRange(waitlist);


        var groups = await db.Groups.Include(g => g.Members).ToArrayAsync();
        db.RemoveRange(groups);

        await db.SaveChangesAsync();
    }



}

public record class GroupVM
{
    public required GroupMemberVM[] Members;
}

public record class GroupMemberVM
{
    public required string Name;
}