using MediatR;
using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class Group() : BaseEntity
{
    public Guid Id { get; init; }
    public List<User> Members { get; } = [];
    public required Course Course { get; init; }
    public required Preferences Preferences { get; init; }
    public uint GroupLimit { get; set; } = 2;

    // Set after the group has achived it's desired amount of members. Never reset.
    public bool IsComplete { get; set; } = false;

    public bool IsFull => Members.Count >= GroupLimit;
}

public class GroupRepository : Repository<Group, Guid>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<TicketRepository> logger;
    private readonly IMediator mediator;

    public GroupRepository(
        ApplicationDbContext applicationDbContext,
        ILogger<TicketRepository> ticketLogger,
        IMediator ticketMediator
    )
        : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = ticketLogger ?? throw new ArgumentNullException(nameof(ticketLogger));
        mediator = ticketMediator ?? throw new ArgumentNullException(nameof(ticketMediator));
    }

    public new async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(group, cancellationToken);
        if (errors.Length > 0)
        {
            throw new Exception("Cannot create group: " + string.Join(", ", errors));
        }

        if (group.Members.Count >= group.GroupLimit)
        {
            group.Events.Add(new GroupFilled { Group = group });
        }

        db.Add(group);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string[]> ValidateAsync(
        Group groupToValidate,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<string>();

        var course = groupToValidate.Course ?? throw new NullReferenceException("Course is null");

        if (course.AllowCustomSize == false && groupToValidate.GroupLimit != course.GroupSize)
        {
            errors.Add($"Custom sizes are not allowed for course {course.Name}");
        }

        if (groupToValidate.Members.Count > groupToValidate.GroupLimit)
        {
            errors.Add("Members are over group member limit");
        }

        foreach (var member in groupToValidate.Members)
        {
            var otherGroups = await db
                .Groups.Include(g => g.Course)
                .Include(g => g.Members)
                .Where(g => g.Course == course)
                .Where(g => g.Members.Contains(member))
                .ToArrayAsync(cancellationToken);
            if (otherGroups.Length > 0)
            {
                errors.Add(
                    $"Group member {member.UserName} {member.Id} is already in another group in course {course.Name}"
                );
            }
        }

        return errors.ToArray();
    }

    public async Task<bool> IsUserInGroup(User user, Course course)
    {
        var groups = await GetGroups(user);
        foreach (var group in groups)
        {
            if (group.Course == course)
            {
                return true;
            }
        }
        return false;
    }

    public async Task ChangeGroupSize(Guid courseId, uint newSize)
    {
        var course =
            await db.Courses.FirstAsync(c => c.Id == courseId)
            ?? throw new Exception("Course not found");
        course.GroupSize = newSize;
        await db.SaveChangesAsync();
    }

    public async Task ChangeCustomGroupSize(Guid courseId, bool allowCustomSize)
    {
        var course =
            await db.Courses.FirstAsync(c => c.Id == courseId)
            ?? throw new Exception("Course not found");
        course.AllowCustomSize = allowCustomSize;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Get all groups
    /// </summary>
    /// <returns></returns>
    public async Task<Group[]> GetGroups()
    {
        return await db
            .Groups.Include(g => g.Members)
            .Include(g => g.Course)
            .Include(g => g.Preferences)
            .ToArrayAsync();
    }

    public async Task<Group[]> GetGroups(Guid id)
    {
        return await db
            .Groups.Include(g => g.Members)
            .Include(g => g.Course)
            .Include(g => g.Preferences)
            .Where(g => g.Course.Id == id)
            .ToArrayAsync();
    }

    public async Task<List<Group>> GetAvailableGroups()
    {
        return await db
            .Groups.Include(g => g.Members)
            .Include(g => g.Preferences)
            .Where(g => g.IsComplete == false)
            .ToListAsync();
    }

    public async Task<Group[]> GetAvailableGroups(Guid id)
    {
        return await db
            .Groups.Include(g => g.Members)
            .Include(g => g.Course)
            .Include(g => g.Preferences)
            .Where(g => g.Course.Id == id)
            .Where(g => g.IsComplete == false)
            .ToArrayAsync();
    }

    public async Task<Group> GetGroup(Guid groupId)
    {
        var group =
            await db
                .Groups.Include(g => g.Members)
                .Include(g => g.Course)
                .Include(g => g.Preferences)
                .FirstAsync(g => g.Id == groupId) ?? throw new Exception("Group not found");
        return group;
    }

    public async Task<User[]> GetGroupMembers(Guid groupId)
    {
        var group =
            await db
                .Groups.Include(g => g.Members)
                .Include(g => g.Course)
                .FirstAsync(g => g.Id == groupId) ?? throw new Exception("Group not found");
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
        return await db
            .Groups.Include(g => g.Members)
            .Include(g => g.Course)
            .Where(g => g.Members.Contains(user))
            .Include(g => g.Course)
            .ToArrayAsync();
    }

    public async Task DeleteGroup(Guid id)
    {
        var group =
            await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == id)
            ?? throw new Exception("Group not found");
        db.Remove(group);
        var disband_event = new GroupDisbanded()
        {
            GroupId = group.Id,
            Course = group.Course,
            Members = [.. group.Members],
        };
        await mediator.Publish(disband_event);
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveUserFromGroup(
        string userId,
        Guid groupId,
        GroupMemberRemovedReason reason = GroupMemberRemovedReason.None
    )
    {
        var group =
            await db.Groups.Include(g => g.Members).FirstAsync(g => g.Id == groupId)
            ?? throw new Exception("Group not found");
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

    public async Task<bool> CheckIfInGroup(User user, Course course)
    {
        var group = await db
            .Groups.Where(g => g.Course == course && g.Members.Contains(user))
            .FirstOrDefaultAsync();
        if (group is null)
        {
            return false;
        }
        return true;
    }

    public async Task<Task> AddToGroup(User user, Group group)
    {
        if (group.Members.Contains(user))
        {
            logger.LogError("User '{userId}' is already in group '{groupId}'", user.Id, group.Id);
            return Task.CompletedTask;
        }
        group.Members.Add(user);
        group.Events.Add(new GroupMemberAdded() { User = user, Group = group });

        if (group.IsFull && group.IsComplete == false)
        {
            group.IsComplete = true;
            group.Events.Add(new GroupFilled() { Group = group });
        }

        await db.SaveChangesAsync();
        return Task.CompletedTask;
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
    UserRemovedByAdmin,
}
