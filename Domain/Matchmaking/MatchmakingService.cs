using QuickFinder.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace QuickFinder.Domain.Matchmaking;

public class MatchmakingService(
    ApplicationDbContext db,
    ILogger<MatchmakingService> logger,
    GroupRepository groupRepository
    )
{
    public static IEnumerable<KeyValuePair<decimal, ICandidate>> OrderCandidates(ICandidate seedCandidate, IEnumerable<ICandidate> candidatePool)
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
    public static ICandidate[] Match(ICandidate seedCandidate, IEnumerable<ICandidate> candidatePool, int groupSize, DateTime time)
    {
        var scoreRequirement = 0.5m;
        var groupSizeLimit = groupSize - 1; // seed candidate is already in the group
        var waitTime = time - seedCandidate.CreatedAt; // TODO: account for wait time.

        var orderedCandidates = OrderCandidates(seedCandidate, candidatePool).Where(pair => pair.Key >= scoreRequirement);

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

    public async Task<Group> CreateGroup(User user, Course course, Preferences groupPreferences)
    {
        if (await groupRepository.IsUserInGroup(user, course))
        {
            throw new Exception("User is already in group");
        }
        var group = new Group() { Preferences = groupPreferences, Course = course };
        group.Members.Add(user);

        if (course.AllowCustomSize)
        {
            group.GroupLimit = groupPreferences.GroupSize;
        }
        else
        {
            group.GroupLimit = course.GroupSize;
        }

        db.Add(group);
        await db.SaveChangesAsync();
        return group;
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

    public async Task DoMatching(CancellationToken cancellationToken = default)
    {
        var all_candidates = await db.Tickets.Include(t => t.Course).Include(t => t.Preferences).Include(t => t.User).ToListAsync(cancellationToken);
        // todo: handle open groups that need members.
        // pick a random candidate.
        var seedCandiate = all_candidates.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();


        if (seedCandiate == null)
        {
            logger.LogInformation("No seed candidate to begin matchmaking.");
            return;
        }

        var course = seedCandiate.Course;

        var candidates_in_course = all_candidates.Where(t => t.Course == course);

        var matching_candidates = MatchmakingService.Match(seedCandiate, candidates_in_course, (int)course.GroupSize, DateTime.Now);

        if (matching_candidates.Length == 0)
        {
            logger.LogInformation("No match found for {username}", seedCandiate.User.UserName);
            return;
        }

        var matchingTickets = matching_candidates.Select(candidate =>
            candidates_in_course.First(ticket => ticket == candidate));

        var matchingUsers = matching_candidates.Select(candidate =>
        candidates_in_course.First(ticket => ticket == candidate)).Select(t => t.User);

        var matchingUsernames = matchingTickets.Select(t => t.User.UserName);

        // TODO: make Match generic.

        logger.LogInformation("New potential group in {course}: {leader}, Candidates: {candidates}",
            seedCandiate.Course.Name, seedCandiate.User.UserName, string.Join(", ", matchingTickets));

        var group = new Group { Course = seedCandiate.Course, Preferences = seedCandiate.Preferences, GroupLimit = course.GroupSize, IsComplete = true };
        // assumes all created groups are filled
        db.Add(group);
        group.Members.AddRange([seedCandiate.User, .. matchingUsers]);
        group.Events.Add(new GroupFilled { Group = group });

        foreach (var ticket in matchingTickets)
        {
            db.Remove(ticket);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CoursePreferences?> GetCoursePreferences(Guid courseId, string userId)
    {
        return await db.CoursePreferences.Include(prefs => prefs.User).Include(prefs => prefs.Course).Where(prefs => prefs.UserId == userId && prefs.CourseId == courseId).SingleOrDefaultAsync();
    }

    public async Task<CoursePreferences?> CreateNewCoursePreferences(Guid courseId, string userId)
    {
        var coursePreferences = new CoursePreferences() { CourseId = courseId, UserId = userId };
        db.Add(coursePreferences);
        await db.SaveChangesAsync();
        return coursePreferences;
    }

    public async Task UpdateCoursePreferencesAsync(Guid courseId, string userId, CoursePreferences newPreferences)
    {

        await db.SaveChangesAsync();
    }

    public async Task Reset()
    {
        // TODO: remove all references
        await Task.CompletedTask;
    }

    public async Task<bool> RemoveUserFromWaitlist(string userId)
    {
        var user = await db.Users.FindAsync(userId) ?? throw new Exception("User not found");
        var tickets = await db.Tickets.Include(g => g.User).Where(t => t.User == user).ToArrayAsync();
        db.Tickets.RemoveRange(tickets);

        await db.SaveChangesAsync();

        return true;
    }

    public async Task JoinCourse(User user, Course course)
    {
        course.Members.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task LeaveCourse(User user, Course course)
    {
        if (await groupRepository.CheckIfInGroup(user, course))
        {
            var group = await db.Groups.Where(g => g.Course == course && g.Members.Contains(user)).FirstOrDefaultAsync() ?? throw new Exception("Group not found");
            await groupRepository.RemoveUserFromGroup(user.Id, group.Id);
        }
        course.Members.Remove(user);
        await db.SaveChangesAsync();
    }
}
