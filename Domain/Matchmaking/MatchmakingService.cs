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
    public readonly Matchmaker<Ticket> matchmaker = new Matchmaker<Ticket>(new MatchmakerConfig());

    public async Task DoMatching(CancellationToken cancellationToken = default)
    {
        var all_candidates = await ticketRepository.GetAllAsync(cancellationToken);
        // todo: handle open groups that need members.
        // pick a random candidate.
        var seedCandidate = all_candidates.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();

        if (seedCandidate == null)
        {
            logger.LogInformation("No seed candidate to begin matchmaking.");
            return;
        }

        var course = seedCandidate.Course;

        var candidates_in_course = all_candidates.Where(t => t.Course == course);
        var waitTime = DateTime.Now - seedCandidate.CreatedAt;

        var requiredScore = matchmaker.GetRequiredScore(waitTime);

        var matching_candidates = matchmaker.Match(
            seedCandidate,
            candidates_in_course,
            (int)course.GroupSize,
            requiredScore
        );

        if (matching_candidates.Length == 0)
        {
            logger.LogInformation("No match found for {username}", seedCandidate.User.UserName);
            return;
        }

        var groupTickets = new List<Ticket>([.. matching_candidates, seedCandidate]);

        var groupMembers = groupTickets.Select(t => t.User);

        logger.LogInformation(
            "New potential group in {course}: {leader}, Candidates: {candidates}",
            seedCandidate.Course.Name,
            seedCandidate.User.UserName,
            string.Join(", ", matching_candidates.Select(t => t.User.UserName))
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
        await ticketRepository.RemoveRangeAsync(groupTickets, cancellationToken);
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
        var userPrefs = user.Preferences;
        var coursePrefs = await preferencesRepository.GetCoursePreferences(courseId, userId);
        if (coursePrefs == null)
        {
            coursePrefs = new CoursePreferences();
        }

        var preferences = Preferences.From(userPrefs, coursePrefs);

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
}

public enum AddToQueueResult
{
    Success,
    AlreadyInQueue,
    Failure,
}
