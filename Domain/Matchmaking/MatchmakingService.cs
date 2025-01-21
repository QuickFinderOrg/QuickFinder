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

    public async Task DoMatching()
    {
        // needs a queue of people waiting to match
        var waitlist = await db.People.ToArrayAsync() ?? throw new Exception("WAITLIST");
        foreach (var person in waitlist)
        {
            var groups = await db.Groups.ToArrayAsync();
            var foundGroup = LookForMatch(person, groups);
            if (foundGroup != null)
            {
                // add to group
                foundGroup.Members.Add(person.UserId);
            }
            else
            {
                // create new group
                var group = new Group() { Criteria = person.Criteria with { }, Preferences = person.Preferences with { } };
                group.Members.Add(person.UserId);
                db.Add(group);
            }

            // remove from waiting list

            db.Remove(person);
            await db.SaveChangesAsync(); // TODO: make more efficient
        }

    }

    public async Task AddToWaitlist(User user)
    {
        db.Add(new Person() { UserId = Guid.Parse(user.Id), Name = user.UserName, Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() });
        await db.SaveChangesAsync();
    }

    public async Task<Group[]> GetGroups(User user)
    {
        return await db.Groups.Where(g => g.Members.Contains(Guid.Parse(user.Id))).ToArrayAsync();
    }

    public async Task Reset()
    {
        var waitlist = await db.People.ToArrayAsync();
        db.RemoveRange(waitlist);


        var groups = await db.Groups.ToArrayAsync();
        db.RemoveRange(groups);

        var people = new List<Person> {
            new Person() { Id = Guid.NewGuid(), Name = "Van Hellsing", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Blade", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Nosferatu", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Dracula", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Sylvanas", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } }
        };
        db.AddRange(people);

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