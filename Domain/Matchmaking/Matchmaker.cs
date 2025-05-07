namespace QuickFinder.Domain.Matchmaking;

public class Matchmaker<U, G>(MatchmakerConfig options)
    where U : IUserMatchmakingData
    where G : IGroupMatchmakingData
{
    public IEnumerable<U> FilterMatchesByCriteria(
        IMatchmakingData seed,
        IEnumerable<U> candidatePool
    )
    {
        return candidatePool.Where(candidate => CheckMatchCriteria(seed, candidate));
    }

    // O(N^2)
    public bool CheckGroupCompatibility(IEnumerable<U> candidates)
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

    public U[] Match(IMatchmakingData seed, IEnumerable<U> candidates, int groupMembersToFind)
    {
        var compatible_candidates = FilterMatchesByCriteria(seed, candidates);
        var valid_group_combinations = new List<(decimal score, U[] members)>();

        foreach (
            var members in GenerateMemberCombinations(compatible_candidates, groupMembersToFind)
        )
        {
            // for existing groups, this checks the groups matchmaking data against possible candidates.
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

    private IEnumerable<U[]> GenerateMemberCombinations(
        IEnumerable<U> potentials,
        int noOfGroupMembers
    )
    {
        var potentialMembers = potentials.ToArray();
        if (noOfGroupMembers == 0)
        {
            yield return Array.Empty<U>();
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

        foreach (var criteria in options.CriteriaList)
        {
            var isCompatible = criteria.Check(from, to);
            if (isCompatible == false)
            {
                errors.Add(nameof(criteria));
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
}

public record class MatchmakerConfig
{
    public ICriteriaFunc[] CriteriaList =
    [
        new MustHaveAtLeastOneDayInCommonCriteria(),
        new MustHaveAtLeastOneLanguageInCommonCriteria(),
        new MustHaveAtLeastOneCommonStudyLocationCriteria(),
    ];
    public (decimal weight, IPreference preference)[] WeightedPreferenceList =
    [
        (1m, new DaysInCommonPreference()),
    ];
}

public interface IMatchmakingData
{
    public LanguageFlags Languages { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public StudyLocation StudyLocation { get; init; }
    public TimeSpan WaitTime { get; init; }
}

public interface IUserMatchmakingData : IMatchmakingData
{
    public string UserId { get; init; }
    public new LanguageFlags Languages { get; init; }
    public new Availability Availability { get; init; }
    public new DaysOfTheWeek Days { get; init; }
    public new StudyLocation StudyLocation { get; init; }
    public new TimeSpan WaitTime { get; init; }
}

public record class DefaultUserMatchmakingData : IUserMatchmakingData
{
    public Guid Id;
    public required string UserId { get; init; }
    public LanguageFlags Languages { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public StudyLocation StudyLocation { get; init; }
    public TimeSpan WaitTime { get; init; }
}

public interface IGroupMatchmakingData : IMatchmakingData
{
    public Guid GroupId { get; init; }
}

public record class DefaultGroupMatchmakingData : IGroupMatchmakingData
{
    public required Guid GroupId { get; init; }
    public LanguageFlags Languages { get; init; }
    public Availability Availability { get; init; }
    public DaysOfTheWeek Days { get; init; }
    public StudyLocation StudyLocation { get; init; }
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

// must be symmetric, e.g. can switch from and to to and get the same result.
public interface ICriteriaFunc
{
    public bool Check(IMatchmakingData from, IMatchmakingData to);
}

public class MustHaveAtLeastOneDayInCommonCriteria : ICriteriaFunc
{
    public bool Check(IMatchmakingData from, IMatchmakingData to)
    {
        return from.Days.GetNumberOfMatchingDays(to.Days) > 0;
    }
}

public class MustHaveAtLeastOneLanguageInCommonCriteria : ICriteriaFunc
{
    public bool Check(IMatchmakingData from, IMatchmakingData to)
    {
        return from.Languages.IntersectWith(to.Languages) > 0;
    }
}

public class MustHaveAtLeastOneCommonStudyLocationCriteria : ICriteriaFunc
{
    public bool Check(IMatchmakingData from, IMatchmakingData to)
    {
        return from.StudyLocation.IntersectWith(to.StudyLocation) > 0;
    }
}
