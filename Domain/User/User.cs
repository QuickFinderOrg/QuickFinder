using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace group_finder;

public class User : IdentityUser
{
    public Criteria Criteria { get; set; } = new Criteria();
    public List<Group> Groups { get; } = [];
}
[Owned]
public record class Criteria()
{
    public Availability Availability { get; set; } = Availability.Daytime;
    public string Language = "English";
}


public enum Availability
{
    Daytime,
    Afternoons
}