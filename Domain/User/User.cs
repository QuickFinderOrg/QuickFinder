using System.Collections;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

public class User : IdentityUser
{
    public List<Group> Groups { get; } = [];
    public UserPreferences Preferences { get; set; } = new UserPreferences();
    public IEnumerable<CoursePreferences> CoursePreferences { get; set; } = null!;
    public List<Course> Courses { get; set; } = [];
}
