using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Person> People { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }

    public void SeedDB()
    {
        if (People.Any())
        {
            return;
        }
        People.Add(new Person() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Preferences = new Preferences() { Availability = Availability.Daytime, Language = "en" } });
        SaveChanges();
    }
}

