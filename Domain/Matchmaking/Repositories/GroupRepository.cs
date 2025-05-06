using MediatR;
using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;
using RandomFriendlyNameGenerator;

namespace QuickFinder.Domain.Matchmaking;

public class GroupRepository : Repository<Group, Guid>
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<TicketRepository> logger;
    private readonly IMediator mediator;
    private readonly UserService userService;

    public GroupRepository(
        ApplicationDbContext applicationDbContext,
        ILogger<TicketRepository> ticketLogger,
        UserService ticketUserService,
        IMediator ticketMediator
    )
        : base(applicationDbContext)
    {
        db = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        logger = ticketLogger ?? throw new ArgumentNullException(nameof(ticketLogger));
        mediator = ticketMediator ?? throw new ArgumentNullException(nameof(ticketMediator));
        userService =
            ticketUserService ?? throw new ArgumentNullException(nameof(ticketUserService));
    }

    /// <summary>
    /// Gets group including members and course.
    /// </summary>
    public new async Task<Group?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await db
            .Groups.Include(g => g.Members)
            .Include(g => g.Course)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
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

        if (string.IsNullOrEmpty(group.Name))
        {
            group.Name = NameGenerator.Identifiers.Get();
        }

        await base.AddAsync(group, cancellationToken);
    }

    public async Task AddGroupMembersAsync(
        Guid groupId,
        IEnumerable<string> idsToAdd,
        CancellationToken cancellationToken = default
    )
    {
        var group = await db
            .Groups.Include(g => g.Members)
            .SingleAsync(g => g.Id == groupId, cancellationToken: cancellationToken);

        var existingMemberIds = group.Members.Select(member => member.Id);
        var newMemberIds = idsToAdd.Except(existingMemberIds);

        // TODO: get only the users you need.
        var newMembersToAdd = (await userService.GetAllUsers()).Where(user =>
            newMemberIds.Contains(user.Id)
        );

        group.Members.AddRange(newMembersToAdd);

        foreach (var member in newMembersToAdd)
        {
            // TODO: publish better events
            await mediator.Publish(
                new GroupMemberAdded() { User = member, Group = group },
                cancellationToken
            );
            logger.LogInformation("{user} was added to group", member.UserName);
        }

        if (group.IsFull && group.IsComplete == false)
        {
            group.IsComplete = true;
            group.Events.Add(new GroupFilled() { Group = group });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveGroupMembersAsync(
        Guid groupId,
        IEnumerable<string> idsToRemove,
        CancellationToken cancellationToken = default
    )
    {
        var group = await db
            .Groups.Include(g => g.Members)
            .SingleAsync(g => g.Id == groupId, cancellationToken: cancellationToken);

        group.Members.RemoveAll(member => idsToRemove.Contains(member.Id));

        var memberToRemove = group.Members.Where(member => idsToRemove.Contains(member.Id));

        foreach (var member in memberToRemove)
        {
            // TODO: fix events
            GroupMemberLeft publish = null;
        }

        var memberCount = group.Members.Count;

        if (memberCount == 0)
        {
            await DeleteAsync(groupId, cancellationToken);
        }

        // TODO: queue for delete if last member leaves.

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Removed members: {members} from group: {groupName}",
            string.Join(", ", memberToRemove.Select(m => m.UserName)),
            group.Name
        );
    }

    //TODO: DeleteAsync, which calls GroupDisbanded.

    private async Task<string[]> ValidateAsync(
        Group groupToValidate,
        CancellationToken cancellationToken = default
    )
    {
        var errors = new List<string>();

        var course = groupToValidate.Course ?? throw new NullReferenceException("Course is null");

        if (groupToValidate.Members.Count > groupToValidate.GroupLimit)
        {
            errors.Add("Members are over group member limit");
        }

        var duplicateMembers = groupToValidate
            .Members.GroupBy(member => member.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateMembers.Count > 0)
        {
            errors.Add("Group contains duplicate members: " + string.Join(", ", duplicateMembers));
        }

        foreach (var member in groupToValidate.Members)
        {
            var otherGroups = await db
                .Groups.Include(g => g.Course)
                .Include(g => g.Members)
                .Where(g => g.Course == course)
                .Where(g => g.Members.Contains(member))
                .ToListAsync(cancellationToken);
            otherGroups.Remove(groupToValidate); // ignore current group
            if (otherGroups.Count > 0)
            {
                errors.Add(
                    $"Group member {member.UserName} {member.Id} is already in another group in course {course.Name}"
                );
            }
        }

        return errors.ToArray();
    }

    public async Task SetAllowAnyoneAsync(
        Guid groupId,
        bool newState,
        CancellationToken cancellationToken = default
    )
    {
        var group = await db
            .Groups.Include(g => g.Members)
            .SingleAsync(g => g.Id == groupId, cancellationToken: cancellationToken);

        group.AllowAnyone = newState;
        await db.SaveChangesAsync(cancellationToken);
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

    // TODO: move to course
    public async Task<Group[]> GetGroups(Guid id)
    {
        return await db
            .Groups.Include(g => g.Members)
            .Include(g => g.Course)
            .Include(g => g.Preferences)
            .Where(g => g.Course.Id == id)
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

    // TODO: replace with DeleteAsync
    public async Task DeleteGroup(Guid id, CancellationToken cancellationToken = default)
    {
        var group =
            await db
                .Groups.Include(g => g.Members)
                .FirstAsync(g => g.Id == id, cancellationToken: cancellationToken)
            ?? throw new Exception("Group not found");
        var disband_event = new GroupDisbanded()
        {
            GroupId = group.Id,
            Course = group.Course,
            Members = [.. group.Members],
        };
        await mediator.Publish(disband_event, cancellationToken);
        await base.DeleteAsync(id, cancellationToken);
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
}
