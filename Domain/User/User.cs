using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace group_finder;

public class User : IdentityUser
{
    public Preferences Preferences { get; set; } = new Preferences();
    public List<Group> Groups { get; } = [];
}
[Owned]
public record class Preferences()
{
    public Availability Availability { get; set; } = Availability.Daytime;
    public string Language = "English";
    public uint GroupSize { get; set; } = 2;
}


public enum Availability
{
    Daytime,
    Afternoons
}