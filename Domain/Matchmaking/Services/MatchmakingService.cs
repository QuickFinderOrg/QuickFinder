using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class MatchmakingService(
    ILogger<MatchmakingService> logger,
    GroupRepository groupRepository,
    TicketRepository ticketRepository,
    CourseRepository courseRepository,
    PreferencesRepository preferencesRepository,
    UserService userService,
    ApplicationDbContext db
)
{
    public readonly Matchmaker<UserMatchmakingTicket, DefaultGroupMatchmakingData> matchmaker = new(
        new MatchmakerConfig()
    );

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
        var ticketsInCourse = await ticketRepository.GetAllInCourseAsync(
            course.Id,
            cancellationToken
        );

        if (ticketsInCourse.Length < 2)
        {
            return; // need two students to match
        }

        // pick a seed at random.
        var randomizedTickets = ticketsInCourse.OrderBy(_ => Guid.NewGuid()).ToList();

        var seedTicket = randomizedTickets[0]; // pop the first element
        randomizedTickets.RemoveAt(0);

        var seedData = CreateUserMatchmakingData(seedTicket);

        var candidates = randomizedTickets;
        var candidatesData = candidates.Select(CreateUserMatchmakingData);

        var t0 = DateTime.Now;

        var groupMembersToFind = (int)course.GroupSize - 1;

        var matchingCandidatesData = matchmaker.Match(seedData, candidatesData, groupMembersToFind);

        logger.LogWarning("{}", matchingCandidatesData);

        var dt = DateTime.Now - t0;

        logger.LogInformation("matchmaking with {candidates} took {t}", candidates.Count(), dt);

        if (matchingCandidatesData.Length == 0)
        {
            logger.LogInformation("No match found for {username}", seedTicket.User.UserName);
            return;
        }

        Ticket[] matchingCandidatesTickets = matchingCandidatesData.Select(c => c.Ticket).ToArray();
        Ticket[] fullGroupTickets = [seedTicket, .. matchingCandidatesTickets];

        if (fullGroupTickets.Length < course.GroupSize)
        {
            logger.LogInformation(
                "Not enough members to form a group for {course}. Had {x}, need {n}",
                course.Name,
                fullGroupTickets.Length,
                course.GroupSize
            );
            return;
        }

        logger.LogInformation(
            "New potential group in {course}: {candidates}",
            course,
            string.Join(", ", fullGroupTickets.Select(t => t.User.UserName))
        );

        var group = new Group
        {
            Course = seedTicket.Course,
            Preferences = seedTicket.Preferences,
            GroupLimit = course.GroupSize, //TODO: accept custom group size.
            IsComplete = true,
        };

        // assumes all created groups are filled
        group.Members.AddRange(fullGroupTickets.Select(t => t.User));

        await groupRepository.AddAsync(group, cancellationToken);
        await ticketRepository.RemoveRangeAsync(fullGroupTickets, cancellationToken);
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

    public async Task<AddToQueueResult> QueueForMatchmakingAsync(
        string userId,
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userService.GetUser(userId);
        if (user == null)
        {
            throw new Exception($"User with ID '{userId}' does not exist.");
        }
        var course = await courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
        {
            throw new Exception($"Course with ID '{courseId}' does not exist.");
        }
        var preferences = await preferencesRepository.GetPreferencesAsync(courseId, userId);
        if (preferences == null)
        {
            throw new Exception($"Preferences do not exist.");
        }

        var ticket = new Ticket()
        {
            Course = course,
            User = user,
            Preferences = preferences,
        };
        try
        {
            await ticketRepository.AddAsync(ticket, cancellationToken);
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
        string userId,
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userService.GetUser(userId);
        if (user == null)
        {
            throw new Exception($"User with ID '{userId}' does not exist.");
        }
        var course = await courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
        {
            throw new Exception($"Course with ID '{courseId}' does not exist.");
        }

        try
        {
            var ticket = await ticketRepository.GetByCourseAsync(
                userId,
                courseId,
                cancellationToken
            );
            if (ticket == null)
            {
                return RemoveFromQueueResult.Failure;
            }
            await ticketRepository.DeleteAsync(ticket.Id, cancellationToken);
        }
        catch (Exception)
        {
            return RemoveFromQueueResult.Failure;
        }
        return RemoveFromQueueResult.Success;
    }
}

public enum AddToQueueResult
{
    Success,
    AlreadyInQueue,
    Failure,
}

public enum RemoveFromQueueResult
{
    Success,
    Failure,
}

public record class UserMatchmakingTicket : IUserMatchmakingData
{
    public required Ticket Ticket { get; init; }
    public required string UserId { get; init; }
    public LanguageFlags Languages { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public TimeSpan WaitTime { get; init; }
}
