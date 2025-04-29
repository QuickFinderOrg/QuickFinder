namespace QuickFinder.Domain.Matchmaking;

public class MatchmakingService(
    ILogger<MatchmakingService> logger,
    GroupRepository groupRepository,
    TicketRepository ticketRepository,
    CourseRepository courseRepository,
    PreferencesRepository preferencesRepository,
    UserService userService
)
{
    public readonly Matchmaker2<UserMatchmakingData, GroupMatchmakingData> matchmaker2 = new(
        new MatchmakerConfig2()
    );

    public async Task DoMatching(CancellationToken cancellationToken = default)
    {
        var all_candidates = await ticketRepository.GetAllAsync(cancellationToken);
        // pick a random candidate.
        var seedCandidate = all_candidates.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();

        if (seedCandidate == null)
        {
            logger.LogInformation("No seed candidate to begin matchmaking.");
            return;
        }

        var course = seedCandidate.Course;

        var candidates_in_course = all_candidates
            .Where(t => t.Course == course)
            .Where(t => t.Id != seedCandidate.Id);

        var seedData = CreateUserMatchmakingData(seedCandidate);

        var candidatesData = candidates_in_course.Select(CreateUserMatchmakingData);

        var matchingMembersData = matchmaker2.Match2(
            seedData,
            candidatesData,
            (int)course.GroupSize
        );

        if (matchingMembersData.Length == 0)
        {
            logger.LogInformation("No match found for {username}", seedCandidate.User.UserName);
            return;
        }

        var desiredTicketIds = matchingMembersData
            .Select(data => (UserMatchmakingData)data)
            .Select(data => data.Id);

        var matchingCandidates = candidates_in_course.Where(ticket =>
            desiredTicketIds.Contains(ticket.Id)
        );

        var fullGroupTickets = new List<Ticket>([.. matchingCandidates, seedCandidate]);

        var groupMembers = fullGroupTickets.Select(t => t.User);

        logger.LogInformation(
            "New potential group in {course}: {leader}, Candidates: {candidates}",
            seedCandidate.Course.Name,
            seedCandidate.User.UserName,
            string.Join(", ", matchingCandidates.Select(t => t.User.UserName))
        );

        var group = new Group
        {
            Course = seedCandidate.Course,
            Preferences = seedCandidate.Preferences,
            GroupLimit = course.GroupSize, //TODO: accept custom group size.
            IsComplete = true,
        };

        // assumes all created groups are filled
        group.Members.AddRange(groupMembers);

        await groupRepository.AddAsync(group, cancellationToken);
        await ticketRepository.RemoveRangeAsync(fullGroupTickets, cancellationToken);
    }

    private static UserMatchmakingData CreateUserMatchmakingData(Ticket ticket)
    {
        var data = new UserMatchmakingData()
        {
            Id = ticket.Id,
            UserId = ticket.User.Id,
            Languages = ticket.Preferences.Language,
            Availability = ticket.Preferences.Availability,
            Days = ticket.Preferences.Days,
            WaitTime = ticket.CreatedAt - DateTime.Now,
        };
        return data;
    }

    public async Task Reset()
    {
        // TODO: remove all references
        await Task.CompletedTask;
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
