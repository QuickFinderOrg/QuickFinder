using group_finder.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Domain.Matchmaking;

public class MatchmakingService(ApplicationDbContext db, IMediator mediator)
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
        var events = new List<object>();
        // needs a queue of people waiting to match
        var waitlist = await db.Tickets.Include(t => t.User).Include(t => t.Course).ToArrayAsync() ?? throw new Exception("WAITLIST");
        var groups = await db.Groups.Include(c => c.Members).ToListAsync();
        foreach (var ticket in waitlist)
        {
            var group = LookForMatch(ticket, [.. groups.Where(g => g.Course == ticket.Course)]);
            if (group == null)
            {
                group = new Group() { Preferences = ticket.User.Preferences, Course = ticket.Course, GroupLimit = ticket.Course.GroupSize };
                db.Add(group);
                groups.Add(group);
            }

            group.Members.Add(ticket.User);
            events.Add(new GroupMemberAdded() { User = ticket.User, Group = group });

            if (group.IsFull)
            {
                events.Add(new GroupFilled() { Group = group });
            }

            // remove from waiting list
            db.Remove(ticket);
        }

        await db.SaveChangesAsync();
        await Task.WhenAll(events.Select(e => mediator.Publish(e))); // Only publish after save
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

    public async Task<Ticket[]> GetWaitlist()
    {
        return await db.Tickets.Include(t => t.User).Include(t => t.Course).ToArrayAsync();
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

    public async Task<Group> GetGroup(Guid groupId)
    {
        var group = await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == groupId) ?? throw new Exception("Group not found");
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
        db.RemoveRange(groups);

        await db.SaveChangesAsync();
    }

    public async Task DeleteGroup(Guid id)
    {
        var group = await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == id) ?? throw new Exception("Group not found");
        var disband_event = new GroupDisbanded() { GroupId = group.Id, Course = group.Course, Members = [.. group.Members] };
        db.Remove(group);

        await db.SaveChangesAsync();
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