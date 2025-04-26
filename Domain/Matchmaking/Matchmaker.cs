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

    public IEnumerable<IMatchmakingData> FilterMatchesByCriteria(
        IMatchmakingData seed,
        IEnumerable<IMatchmakingData> candidatePool
    )
    {
        return candidatePool.Where(candidate => CheckMatchCriteria(seed, candidate));
    }

    // O(N^2)
    public bool CheckGroupCompatibility(IEnumerable<IMatchmakingData> candidates)
    {
        foreach (var x in candidates)
        {
            foreach (var y in candidates)
            {
                var isCompatible = CheckMatchCriteria(x, y);
                // short circuit
                if (isCompatible == false)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public IMatchmakingData[] Match2(
        IMatchmakingData seed,
        IEnumerable<IMatchmakingData> candidates,
        int noOfGroupMembers
    )
    {
        var compatible_candidates = FilterMatchesByCriteria(seed, candidates);
        var valid_group_combinations = new List<(decimal score, IMatchmakingData[] members)>();

        foreach (
            var members in GenerateMemberCombinations(compatible_candidates, noOfGroupMembers - 1)
        )
        {
            if (CheckGroupCompatibility(members))
            {
                var average_score = members
                    .Select(member => GetNormalizedPreferenceScore(seed, member))
                    .Average();
                valid_group_combinations.Add((average_score, members));
            }
        }

        valid_group_combinations.Sort((a, b) => a.score.CompareTo(b.score));

        return valid_group_combinations.Select(g => g.members).FirstOrDefault() ?? [];
    }

    private IEnumerable<IMatchmakingData[]> GenerateMemberCombinations(
        IEnumerable<IMatchmakingData> potentials,
        int noOfGroupMembers
    )
    {
        var potentialMembers = potentials.ToArray();
        if (noOfGroupMembers == 0)
        {
            yield return Array.Empty<IMatchmakingData>();
            yield break;
        }

        for (int i = 0; i < potentialMembers.Length; i++)
        {
            var remaining = potentialMembers.Skip(i + 1).ToArray();
            foreach (var combination in GenerateMemberCombinations(remaining, noOfGroupMembers - 1))
            {
                yield return new[] { potentialMembers[i] }.Concat(combination).ToArray();
            }
        }
    }

    public (bool result, string[] errors) CheckMatchCriteriaWithErrors(
        IMatchmakingData from,
        IMatchmakingData to
    )
    {
        var errors = new List<string>();

        foreach (var critaria in options.CriteriaList)
        {
            var isCompatible = critaria.Check(from, to);
            if (isCompatible == false)
            {
                errors.Add(nameof(critaria));
            }
        }

        return (errors.Count == 0, errors.ToArray());
    }

    public bool CheckMatchCriteria(IMatchmakingData from, IMatchmakingData to)
    {
        return CheckMatchCriteriaWithErrors(from, to).result;
    }

    public decimal GetNormalizedPreferenceScore(IMatchmakingData from, IMatchmakingData to)
    {
        var score = options
            .WeightedPreferenceList.Select((pair) => pair.preference.Check(from, to) * pair.weight)
            .Sum();
        var sum_weights = options.WeightedPreferenceList.Select(pair => pair.weight).Sum();
        return score / sum_weights;
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
    public ICriteriaFunc[] CriteriaList = [new MustHaveAtLeastOneDayInCommonCritera()];
    public (decimal weight, IPreference preference)[] WeightedPreferenceList =
    [
        (1m, new DaysInCommonPreference()),
    ];
}

public interface IMatchmakingData
{
    public LanguageFlags Language { get; init; }
    public Languages[] Languages { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public TimeSpan WaitTime { get; init; }
}

public record class UserMatchmakingData : IMatchmakingData
{
    public required string UserId { get; init; }
    public required Languages[] Languages { get; init; }
    public LanguageFlags Language { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public TimeSpan WaitTime { get; init; }
}

public record class GroupMatchmakingData : IMatchmakingData
{
    public required Guid GroupId { get; init; }
    public required Languages[] Languages { get; init; }
    public LanguageFlags Language { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public TimeSpan WaitTime { get; init; }
}

public interface IPreference
{
    public decimal Check(IMatchmakingData from, IMatchmakingData to);
}

class DaysInCommonPreference : IPreference
{
    public decimal Check(IMatchmakingData from, IMatchmakingData to)
    {
        return from.Days.GetNumberOfMatchingDays(to.Days) / 7;
    }
}

// must be symmertric, e.g. can switch from and to to and get the same result.
public interface ICriteriaFunc
{
    public bool Check(IMatchmakingData from, IMatchmakingData to);
}

public class MustHaveAtLeastOneDayInCommonCritera : ICriteriaFunc
{
    public bool Check(IMatchmakingData from, IMatchmakingData to)
    {
        return from.Days.GetNumberOfMatchingDays(to.Days) > 0;
    }
}

public class MustHaveAtLeastOneLanguageInCommonCritera : ICriteriaFunc
{
    public bool Check(IMatchmakingData from, IMatchmakingData to)
    {
        return from.Language.IntersectWith(to.Language) > 0;
    }
}
