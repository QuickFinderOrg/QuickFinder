using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuickFinder.Data;

namespace QuickFinder.Domain.Matchmaking;

public class MatchmakingService(
    ApplicationDbContext db,
    ILogger<MatchmakingService> logger,
    GroupRepository groupRepository
)
{
    public readonly Matchmaker<Ticket> matchmaker = new Matchmaker<Ticket>(new MatchmakerConfig());

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
