using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace group_finder;

public class User : IdentityUser
{
    public string Name { get; set; } = "RocketRacer";
    public Criteria Criteria { get; set; } = new Criteria();
    public Preferences Preferences { get; set; } = new Preferences();
}
[Owned]
public record class Criteria()
{
    public Availability Availability { get; set; } = Availability.Daytime;
    public string Language = "English";
}

[Owned]
public record class Preferences()
{

}

public enum Availability
{
    Daytime,
    Afternoons
}