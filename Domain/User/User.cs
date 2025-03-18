using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace group_finder;

public class User : IdentityUser
{
    public Preferences Preferences { get; set; } = new Preferences();
    public List<Group> Groups { get; } = [];
}
[Owned]
public record class Preferences() : IEnumerable<KeyValuePair<string, object>>
{
    public Availability Availability { get; set; } = Availability.Daytime;
    public string Language = "English";
    public uint GroupSize { get; set; } = 2;

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        yield return new KeyValuePair<string, object>(nameof(Availability), Availability);
        yield return new KeyValuePair<string, object>(nameof(Language), Language);
        yield return new KeyValuePair<string, object>(nameof(GroupSize), GroupSize);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public enum Availability
{
    Daytime,
    Afternoons
}