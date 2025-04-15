namespace QuickFinder.Domain.Matchmaking;

public class GroupMatchmakingService(
    ILogger<GroupMatchmakingService> logger,
    GroupRepository groupRepository,
    TicketRepository ticketRepository,
    GroupTicketRepository groupTicketRepository,
    CourseRepository courseRepository
)
{
    public readonly Matchmaker<ICandidate> matchmaker = new Matchmaker<ICandidate>(
        new MatchmakerConfig()
    );

    public async Task DoMatching(CancellationToken cancellationToken = default)
    {
        var group_candidates = await groupTicketRepository.GetAllAsync(cancellationToken);

        // pick a random candidate.
        var seedGroupCandidate = group_candidates.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();

        if (seedGroupCandidate == null)
        {
            logger.LogInformation("No seed group candidate to begin matchmaking.");
            return;
        }

        var group = seedGroupCandidate.Group;
        var course = seedGroupCandidate.Course;

        var user_candidates = await ticketRepository.GetAllAsync(cancellationToken);

        var candidates_in_course = user_candidates
            .Where(t => t.Course == course)
            .Where(t => !group.Members.Contains(t.User));
        var waitTime = DateTime.Now - seedGroupCandidate.CreatedAt;

        var requiredScore = matchmaker.GetRequiredScore(waitTime);

        var matching_candidates = matchmaker.Match(
            seedGroupCandidate,
            candidates_in_course,
            groupSize: 2, // Only look for one member at a time.
            requiredScore
        );

        var newMemberCandidate = matching_candidates.FirstOrDefault();

        if (newMemberCandidate == null)
        {
            logger.LogInformation(
                "No match found for group {username}",
                seedGroupCandidate.Group.Name
            );
            return;
        }

        var newMemberTicket = (Ticket)newMemberCandidate;

        logger.LogInformation(
            "New potential group in {course}: {candidate}",
            seedGroupCandidate.Course.Name,
            newMemberTicket.User.UserName
        );

        group.Members.Add(newMemberTicket.User);
        await groupRepository.UpdateAsync(group, cancellationToken);

        await ticketRepository.RemoveRangeAsync([newMemberTicket], cancellationToken);
        logger.LogInformation(
            "Group {group} found member {name}. Removed user ticket.",
            group.Name,
            newMemberTicket.User.UserName
        );

        if (group.Members.Count >= group.GroupLimit)
        {
            await groupTicketRepository.RemoveRangeAsync([seedGroupCandidate], cancellationToken);
            logger.LogInformation("Group {group} filled. Removed group ticket.", group.Name);
        }
    }

    public async Task Reset()
    {
        // TODO: remove all references
        await Task.CompletedTask;
    }

    public async Task<AddToQueueResult> QueueForMatchmakingAsync(
        Guid groupId,
        Guid courseId,
        CancellationToken cancellationToken = default
    )
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new Exception($"Group with ID '{groupId}' does not exist.");
        }
        var course = await courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
        {
            throw new Exception($"Course with ID '{courseId}' does not exist.");
        }

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
}
