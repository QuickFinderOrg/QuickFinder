namespace QuickFinder.Domain.Matchmaking;

/// <summary>
/// Contains the main business logic for matchmaking. Stateless.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="options"></param>
public class Matchmaker<T>(MatchmakerConfig options)
    where T : ICandidate
{
    public IEnumerable<KeyValuePair<decimal, T>> OrderCandidates(
        ICandidate seedCandidate,
        IEnumerable<T> candidatePool
    )
    {
        var potentialMembers = new List<KeyValuePair<decimal, T>>();

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
    public T[] Match(
        T seedCandidate,
        IEnumerable<T> candidatePool,
        int groupSize,
        decimal scoreRequirement
    )
    {
        var groupSizeLimit = groupSize - 1; // seed candidate is already in the group

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
    public decimal GetScore(IPreferences from, IPreferences to)
    {
        var languageWeight = options.languageWeight;
        var availabilityWeight = options.availabilityWeight;
        var daysWeight = options.daysWeight;
        var groupSizeWeight = options.groupSizeWeight;
        var weights = languageWeight + availabilityWeight + daysWeight + groupSizeWeight;

        var languageScore = Preferences.GetLanguageScore(from, to) * languageWeight;
        var availabilityScore = Preferences.GetAvailabilityScore(from, to) * availabilityWeight;
        var daysScore = Preferences.GetDaysScore(from, to) * daysWeight;
        var groupSizeScore = Preferences.GetGroupSizeScore(from, to) * groupSizeWeight;

        return (languageScore + availabilityScore + daysScore + groupSizeScore) / weights;
    }

    public decimal GetRequiredScore(TimeSpan timeInQueue)
    {
        if (timeInQueue < TimeSpan.FromHours(1))
        {
            // t0
            return 0.9m;
        }
        else if (timeInQueue < TimeSpan.FromHours(6))
        {
            // t1
            return 0.7m;
        }
        else if (timeInQueue < TimeSpan.FromHours(12))
        {
            // t2
            return 0.6m;
        }
        else
        {
            // t3
            return 0.5m;
        }
    }
}

public record class MatchmakerConfig
{
    public readonly decimal languageWeight = 1;
    public readonly decimal availabilityWeight = 1;
    public readonly decimal daysWeight = 1;
    public readonly decimal groupSizeWeight = 1;
}
