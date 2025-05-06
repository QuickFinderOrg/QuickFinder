using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class GroupMatchmakingService(
    ILogger<GroupMatchmakingService> logger,
    GroupRepository groupRepository,
    TicketRepository ticketRepository,
    GroupTicketRepository groupTicketRepository,
    CourseRepository courseRepository,
    ApplicationDbContext db
)
{
    public readonly Matchmaker<UserMatchmakingTicket, GroupMatchmakingTicket> matchmaker =
        new Matchmaker<UserMatchmakingTicket, GroupMatchmakingTicket>(new MatchmakerConfig());

    public async Task DoMatching(CancellationToken cancellationToken = default)
    {
        var courses = await courseRepository.GetAllAsync(cancellationToken);

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var course in courses)
            {
                await DoMatchmakingForCourse(course, cancellationToken);
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in matchmaking. Doing rollback.");
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public async Task DoMatchmakingForCourse(
        Course course,
        CancellationToken cancellationToken = default
    )
    {
        var groupTicketsInCourse = await groupTicketRepository.GetAllInCourseAsync(
            course.Id,
            cancellationToken
        );

        if (groupTicketsInCourse.Length == 0)
        {
            logger.LogInformation("No groups in {course}.", course.Name);
            return;
        }

        var userTicketsInCourse = await ticketRepository.GetAllInCourseAsync(
            course.Id,
            cancellationToken
        );

        // need a course and a user before starting matching
        var canMatch = groupTicketsInCourse.Length > 1 || userTicketsInCourse.Length > 1;

        if (!canMatch)
        {
            return;
        }

        // pick a seed at random.
        var randomizedTickets = groupTicketsInCourse.OrderBy(_ => Guid.NewGuid());

        var seedTicket = randomizedTickets.Take(1).Single();
        var seedData = CreateGroupMatchmakingData(seedTicket);
        var groupMemberCount = seedTicket.Group.Members.Count;

        var candidates = userTicketsInCourse;
        var candidatesData = candidates.Select(CreateUserMatchmakingData);

        var t0 = DateTime.Now;

        var groupMembersToFind = (int)course.GroupSize - groupMemberCount;

        var matchingCandidatesData = matchmaker.Match(seedData, candidatesData, groupMembersToFind);

        logger.LogWarning("{}", matchingCandidatesData);

        var dt = DateTime.Now - t0;

        logger.LogInformation(
            "Group matchmaking with {candidates} took {t}",
            candidates.Count(),
            dt
        );

        if (matchingCandidatesData.Length == 0)
        {
            logger.LogInformation("No members found for {groupname}", seedTicket.Group.Name);
            return;
        }

        Ticket[] matchingCandidatesTickets = matchingCandidatesData.Select(c => c.Ticket).ToArray();

        if (matchingCandidatesTickets.Length < groupMembersToFind)
        {
            logger.LogInformation(
                "Not enough members to form a group for {course}. Had {x}, need {n}",
                course.Name,
                groupMemberCount,
                groupMembersToFind
            );
            return;
        }

        logger.LogInformation(
            "New potential group in {course}: {candidates}",
            course,
            string.Join(", ", matchingCandidatesTickets.Select(t => t.User.UserName))
        );

        var group = seedTicket.Group;

        await groupRepository.AddGroupMembersAsync(
            group.Id,
            matchingCandidatesTickets.Select(t => t.User.Id)
        );
        await ticketRepository.RemoveRangeAsync(matchingCandidatesTickets, cancellationToken);

        await groupTicketRepository.RemoveRangeAsync([seedTicket], cancellationToken);
        logger.LogInformation("Group {group} filled. Removed group ticket.", seedTicket.Group.Name);
    }

    private static UserMatchmakingTicket CreateUserMatchmakingData(Ticket ticket)
    {
        var data = new UserMatchmakingTicket()
        {
            UserId = ticket.User.Id,
            Ticket = ticket,
            Languages = ticket.Preferences.Language,
            Availability = ticket.Preferences.Availability,
            Days = ticket.Preferences.Days,
            WaitTime = DateTime.Now - ticket.CreatedAt,
        };
        return data;
    }

    private static GroupMatchmakingTicket CreateGroupMatchmakingData(GroupTicket ticket)
    {
        var data = new GroupMatchmakingTicket()
        {
            GroupId = ticket.Group.Id,
            Ticket = ticket,
            Languages = ticket.Preferences.Language,
            Availability = ticket.Preferences.Availability,
            Days = ticket.Preferences.Days,
            WaitTime = DateTime.Now - ticket.CreatedAt,
        };
        return data;
    }

    public async Task<AddToQueueResult> QueueForMatchmakingAsync(
        Guid groupId,
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        var group =
            await groupRepository.GetByIdAsync(groupId)
            ?? throw new Exception($"Group with ID '{groupId}' does not exist.");
        var course =
            await courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new Exception($"Course with ID '{courseId}' does not exist.");
        var preferences = group.Preferences;
        var ticket = new GroupTicket()
        {
            Course = course,
            Group = group,
            Preferences = preferences,
        };
        try
        {
            await groupTicketRepository.AddAsync(ticket, cancellationToken);
        }
        catch (AlreadyInQueueException)
        {
            return AddToQueueResult.AlreadyInQueue;
        }
        catch (Exception)
        {
            return AddToQueueResult.Failure;
        }

        return AddToQueueResult.Success;
    }

    public async Task<RemoveFromQueueResult> RemoveFromQueueAsync(
        Guid groupId,
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        var group =
            await groupRepository.GetGroup(groupId)
            ?? throw new Exception($"Group with ID '{groupId}' does not exist.");
        var course =
            await courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new Exception($"Course with ID '{courseId}' does not exist.");
        try
        {
            var groupTicket = await groupTicketRepository.GetByCourseAsync(
                group.Id,
                course.Id,
                cancellationToken
            );
            if (groupTicket == null)
            {
                return RemoveFromQueueResult.Failure;
            }
            await groupTicketRepository.DeleteAsync(groupTicket.Id, cancellationToken);
        }
        catch (Exception)
        {
            return RemoveFromQueueResult.Failure;
        }
        return RemoveFromQueueResult.Success;
    }
}

public record class GroupMatchmakingTicket : IGroupMatchmakingData
{
    public required GroupTicket Ticket { get; init; }
    public required Guid GroupId { get; init; }
    public LanguageFlags Languages { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public TimeSpan WaitTime { get; init; }
}
