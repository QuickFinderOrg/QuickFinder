using group_finder.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class MatchmakingService(ApplicationDbContext db, IMediator mediator, ILogger<MatchmakingService> logger)
{
    public Group? LookForMatch(Ticket ticket, Group[] groups)
    {
        var potentialGroups = new List<PotentialGroupVM>();
        // other groups do exist
        foreach (var group in groups)
        {
            // is this the group for me?
            if (ticket.WillAcceptGroup(group))
            {
                var potentialGroup = new PotentialGroupVM() { Group = group };
                foreach (var preference in group.Preferences)
                {
                    // Check language preference
                    // and give 0.5 score for each match
                    if (preference.Key == "Language")
                    {
                        var languages = preference.Value as Languages[] ?? throw new Exception("No languages found");
                        foreach (var language in languages)
                        {
                            if (ticket.User.Preferences.Language.Contains(language))
                            {
                                potentialGroup.Score += 0.5;
                            }
                        }
                    }

                    // Give 1 score for each match
                    else
                    {
                        if (ticket.User.Preferences.Contains(preference))
                        {
                            potentialGroup.Score++;
                        }
                    }
                }
                potentialGroups.Add(potentialGroup);
            }

            // no: keep looking
        }

        if (potentialGroups.Count > 0)
        {
            // sort by score
            potentialGroups.Sort((a, b) => b.Score.CompareTo(a.Score));
            return potentialGroups[0].Group;
        }
        // no groups, make a new one.
        // or could not fit into any groups, make new one.
        return null;
    }

    public Group AddGroup(Ticket ticket, List<Group> groups)
    {
        var group = new Group() { Preferences = ticket.User.Preferences, Course = ticket.Course };
        if (ticket.Course.AllowCustomSize)
        {
            group.GroupLimit = ticket.User.Preferences.GroupSize;
            db.Add(group);
            groups.Add(group);
        }
        else
        {
            group.GroupLimit = ticket.Course.GroupSize;
            db.Add(group);
            groups.Add(group);
        }
        return group;
    }

    public Task AddToGroup(Ticket ticket, Group group, List<object> events)
    {
        group.Members.Add(ticket.User);
        events.Add(new GroupMemberAdded() { User = ticket.User, Group = group });

        if (group.IsFull && group.IsComplete == false)
        {
            group.IsComplete = true;
            events.Add(new GroupFilled() { Group = group });
        }

        // remove from waiting list
        db.Remove(ticket);
        return Task.CompletedTask;
    }

    public async Task<Task> AddToGroup(User user, Group group, List<object> events)
    {
        group.Members.Add(user);
        events.Add(new GroupMemberAdded() { User = user, Group = group });

        if (group.IsFull && group.IsComplete == false)
        {
            group.IsComplete = true;
            events.Add(new GroupFilled() { Group = group });
        }

        await db.SaveChangesAsync();
        return Task.CompletedTask;
    }

    public async Task DoMatching(CancellationToken cancellationToken = default)
    {
        var events = new List<object>();
        // needs a queue of people waiting to match
        var waitlist = await GetWaitlist();
        var groups = await GetAvailableGroups();

        foreach (var ticket in waitlist)
        {
            var group = LookForMatch(ticket, [.. groups.Where(g => g.Course == ticket.Course)]);
            group ??= AddGroup(ticket, groups);

            await AddToGroup(ticket, group, events);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var e in events)
        {
            await mediator.Publish(e, cancellationToken);
        }
    }

    public async Task<bool> AddToWaitlist(User user, Course course)
    {
        var existing = await db.Tickets.Include(c => c.User).Include(c => c.Course).Where(t => t.User == user && t.Course == course).ToArrayAsync();

        if (existing.Length != 0)
        {
            logger.LogError("User '{userId}' is already queued up for course '{courseId}'", user.Id, course.Id);
            return false;
        }

        db.Add(new Ticket() { User = user, Course = course });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Ticket[]> GetWaitlist()
    {
        return await db.Tickets.Include(t => t.User).Include(t => t.Course).ToArrayAsync() ?? throw new Exception("WAITLIST");
    }

    /// <summary>
    /// Get tickets for a particular course
    /// </summary>
    /// <param name="course"></param>
    /// <returns></returns>
    public async Task<Ticket[]> GetWaitlist(Course course)
    {
        return await db.Tickets.Include(t => t.User).Include(t => t.Course).Where(t => t.Course == course).ToArrayAsync();
    }

    public async Task<Course> CreateCourse(string name, uint groupSize, bool allowCustomSize)
    {
        var course = new Course() { Name = name, GroupSize = groupSize, AllowCustomSize = allowCustomSize };
        db.Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    public async Task<Course[]> GetCourses()
    {
        return await db.Courses.ToArrayAsync();
    }

    public async Task<Course[]> GetCourses(User user)
    {
        var groups = await GetGroups(user);
        var courses = await db.Courses.ToListAsync();

        foreach (Group group in groups)
        {
            courses.Remove(group.Course);
        }

        return [.. courses];
    }

    public async Task<Course> GetCourse(Guid courseId)
    {
        return await db.Courses.FirstAsync(c => c.Id == courseId) ?? throw new Exception("Course not found");
    }

    public async Task ChangeGroupSize(Guid courseId, uint newSize)
    {
        var course = await db.Courses.FirstAsync(c => c.Id == courseId) ?? throw new Exception("Course not found");
        course.GroupSize = newSize;
        await db.SaveChangesAsync();
    }

    public async Task ChangeCustomGroupSize(Guid courseId, bool allowCustomSize)
    {
        var course = await db.Courses.FirstAsync(c => c.Id == courseId) ?? throw new Exception("Course not found");
        course.AllowCustomSize = allowCustomSize;
        await db.SaveChangesAsync();
    }


    /// <summary>
    /// Get all groups
    /// </summary>
    /// <returns></returns>
    public async Task<Group[]> GetGroups()
    {
        return await db.Groups.Include(g => g.Members).Include(g => g.Course).ToArrayAsync();
    }

    public async Task<Group[]> GetGroups(Guid id)
    {
        return await db.Groups.Include(g => g.Members).Include(g => g.Course).Where(g => g.Course.Id == id).ToArrayAsync();
    }

    public async Task<List<Group>> GetAvailableGroups()
    {
        return await db.Groups.Include(g => g.Members).Where(g => g.IsComplete == false).ToListAsync();
    }

    public async Task<Group[]> GetAvailableGroups(Guid id)
    {
        return await db.Groups.Include(g => g.Members).Include(g => g.Course).Where(g => g.Course.Id == id).Where(g => g.IsComplete == false).ToArrayAsync();
    }
    public async Task<Group> GetGroup(Guid groupId)
    {
        var group = await db.Groups.Include(g => g.Members).Include(g => g.Course).FirstAsync(g => g.Id == groupId) ?? throw new Exception("Group not found");
        return group;
    }

    public async Task<User[]> GetGroupMembers(Guid groupId)
    {
        var group = await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == groupId) ?? throw new Exception("Group not found");
        var users = group.Members.ToArray();
        return users;
    }

    /// <summary>
    /// Get all groups the user is a part of
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<Group[]> GetGroups(User user)
    {
        return await db.Groups.Include(g => g.Members).Where(g => g.Members.Contains(user)).Include(g => g.Course).ToArrayAsync();
    }

    public async Task Reset()
    {
        var waitlist = await db.Tickets.Include(p => p.User).ToArrayAsync();
        db.RemoveRange(waitlist);

        var groups = await db.Groups.Include(g => g.Members).ToArrayAsync();
        foreach (var group in groups)
        {
            await QueueDeleteGroup(group);
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteGroup(Guid id)
    {
        var group = await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == id) ?? throw new Exception("Group not found");
        await QueueDeleteGroup(group);
        await db.SaveChangesAsync();
    }

    private async Task QueueDeleteGroup(Group group)
    {
        var disband_event = new GroupDisbanded() { GroupId = group.Id, Course = group.Course, Members = [.. group.Members] };
        db.Remove(group);

        await mediator.Publish(disband_event);
    }


    public async Task<bool> RemoveUserFromGroup(string userId, Guid groupId, GroupMemberRemovedReason reason = GroupMemberRemovedReason.None)
    {
        var group = await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == groupId) ?? throw new Exception("Group not found");
        var user = await db.Users.FindAsync(userId) ?? throw new Exception("User not found");
        var was_user_removed = group.Members.Remove(user);

        await db.SaveChangesAsync();

        if (group.Members.Count == 0)
        {
            await mediator.Publish(new GroupEmpty() { GroupId = group.Id });
            await DeleteGroup(group.Id);
        }
        else
        {
            await mediator.Publish(new GroupMemberLeft() { Group = group, User = user });
        }
        return was_user_removed;
    }

    public async Task<bool> RemoveUserFromWaitlist(string userId)
    {
        var user = await db.Users.FindAsync(userId) ?? throw new Exception("User not found");
        var tickets = await db.Tickets.Include(g => g.User).Where(t => t.User == user).ToArrayAsync();
        db.Tickets.RemoveRange(tickets);

        await db.SaveChangesAsync();

        return true;
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

public record class PotentialGroupVM
{
    public required Group Group;
    public double Score = 0;
}

public enum GroupMemberRemovedReason
{
    None,
    UserChoice,
    UserAccountDeleted,
    UserRemovedByAdmin
}

public class OnBeforeUserDelete(MatchmakingService matchmakingService) : INotificationHandler<UserToBeDeleted>
{
    public async Task Handle(UserToBeDeleted notification, CancellationToken cancellationToken)
    {
        var user = notification.User;

        await matchmakingService.RemoveUserFromWaitlist(user.Id);

        var groups = await matchmakingService.GetGroups(user);
        await Task.WhenAll(groups.Select(group => matchmakingService.RemoveUserFromGroup(user.Id, group.Id, GroupMemberRemovedReason.UserAccountDeleted)));
    }
}