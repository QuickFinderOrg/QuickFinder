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
            if (personToMatch.WillAcceptGroup(group))
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
        var waitlist = await db.People.Include(p => p.User).ToArrayAsync() ?? throw new Exception("WAITLIST");
        foreach (var person in waitlist)
        {
            var groups = await db.Groups.ToArrayAsync();
            var foundGroup = LookForMatch(person, groups);
            if (foundGroup != null)
            {
                // add to group
                foundGroup.Members.Add(person.User);
            }
            else
            {
                // create new group
                var group = new Group() { Criteria = person.User.Criteria, Preferences = person.User.Preferences };
                group.Members.Add(person.User);
                db.Add(group);
            }

            // remove from waiting list

            db.Remove(person);
            await db.SaveChangesAsync(); // TODO: make more efficient
        }

    }

    public async Task AddToWaitlist(User user, Course course)
    {
        db.Add(new Person() { User = user });
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
        var waitlist = await db.People.Include(p => p.User).ToArrayAsync();
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