using QuickFinder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace QuickFinder;

public class User : IdentityUser
{
    public List<Group> Groups { get; } = [];
    public UserPreferences Preferences { get; set; } = new UserPreferences();
    public IEnumerable<CoursePreferences> CoursePreferences { get; set; } = null!;
}