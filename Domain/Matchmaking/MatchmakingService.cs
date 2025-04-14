using MediatR;
using Microsoft.EntityFrameworkCore;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class MatchmakingService(
    ApplicationDbContext db,
    ILogger<MatchmakingService> logger,
    GroupRepository groupRepository
)
{
    public static IEnumerable<KeyValuePair<decimal, ICandidate>> OrderCandidates(
        ICandidate seedCandidate,
        IEnumerable<ICandidate> candidatePool
    )
    {
        var potentialMembers = new List<KeyValuePair<decimal, ICandidate>>();

        foreach (var candidate in candidatePool)
        {
            if (candidate.Id == seedCandidate.Id)
            {
                continue; // skip the seed candidate
            }
            var score = GetScore(seedCandidate.Preferences, candidate.Preferences);
            if (score > 0)
            {
                potentialMembers.Add(KeyValuePair.Create(score, candidate));
            }
        }

        var sortedList = potentialMembers.OrderByDescending(pair => pair.Key).ToList();
        return sortedList;
    }

    /// <summary>
    /// Returns the group members that match the seed candidate's preferences.
    /// The seed candidate is not included in the result.
    /// </summary>
    /// <param name="seedCandidate"></param>
    /// <param name="candidatePool"></param>
    /// <param name="groupSize"></param>
    /// <returns></returns>
    public static ICandidate[] Match(
        ICandidate seedCandidate,
        IEnumerable<ICandidate> candidatePool,
        int groupSize,
        DateTime time
    )
    {
        var scoreRequirement = 0.5m;
        var groupSizeLimit = groupSize - 1; // seed candidate is already in the group
        var waitTime = time - seedCandidate.CreatedAt; // TODO: account for wait time.

        var orderedCandidates = OrderCandidates(seedCandidate, candidatePool)
            .Where(pair => pair.Key >= scoreRequirement);

        var sortedList = orderedCandidates.Select(pair => pair.Value).ToList();

        var bestCandidates = sortedList.Take(groupSizeLimit).ToArray();
        return bestCandidates;
    }

    /// <summary>
    /// Get the score between two candidates.
    /// The score is a number between 0 and 1, where 0 means no match and 1 means perfect match.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public static decimal GetScore(IPreferences from, IPreferences to)
    {
        var languageWeight = 1;
        var availabilityWeight = 1;
        var daysWeight = 1;
        var groupSizeWeight = 1;
        var weights = languageWeight + availabilityWeight + daysWeight + groupSizeWeight;

        var languageScore = Preferences.GetLanguageScore(from, to) * languageWeight;
        var availabilityScore = Preferences.GetAvailabilityScore(from, to) * availabilityWeight;
        var daysScore = Preferences.GetDaysScore(from, to) * daysWeight;
        var groupSizeScore = Preferences.GetGroupSizeScore(from, to) * groupSizeWeight;

        return (languageScore + availabilityScore + daysScore + groupSizeScore) / weights;
    }

    public async Task DoMatching(CancellationToken cancellationToken = default)
    {
        var all_candidates = await db
            .Tickets.Include(t => t.Course)
            .Include(t => t.Preferences)
            .Include(t => t.User)
            .ToListAsync(cancellationToken);
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

        var matching_candidates = MatchmakingService.Match(
            seedCandidate,
            candidates_in_course,
            (int)course.GroupSize,
            DateTime.Now
        );

        if (matching_candidates.Length == 0)
        {
            logger.LogInformation("No match found for {username}", seedCandidate.User.UserName);
            return;
        }

        var matchingTickets = matching_candidates.Select(candidate =>
            candidates_in_course.First(ticket => ticket == candidate)
        );

        Ticket[] groupTickets = [seedCandidate, .. matchingTickets];

        var groupMembers = groupTickets.Select(t => t.User);

        var matchingUsernames = matchingTickets.Select(t => t.User.UserName);

        // TODO: make Match generic.

        logger.LogInformation(
            "New potential group in {course}: {leader}, Candidates: {candidates}",
            seedCandidate.Course.Name,
            seedCandidate.User.UserName,
            string.Join(", ", matchingTickets)
        );

        var group = new Group
        {
            Course = seedCandidate.Course,
            Preferences = seedCandidate.Preferences,
            GroupLimit = course.GroupSize,
            IsComplete = true,
        };
        // assumes all created groups are filled
        group.Members.AddRange(groupMembers);

        await groupRepository.AddAsync(group, cancellationToken);

        foreach (var ticket in groupTickets)
        {
            db.Remove(ticket);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Reset()
    {
        // TODO: remove all references
        await Task.CompletedTask;
    }
}
