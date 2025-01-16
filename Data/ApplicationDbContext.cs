using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Person> People { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<WaitingPerson> WaitingPeople { get; set; } = null!;

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

        var people = new List<Person> {
            new Person() { Id = Guid.NewGuid(), Name = "Van Hellsing", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Blade", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Nosferatu", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Dracula", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Sylvanas", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } }
        };

        AddRange(people);

        // add all to waitlist/matchmaking pool
        foreach (var person in people)
        {
            Add(new WaitingPerson() { Id = Guid.NewGuid(), Person = person });
        }

        SaveChanges();
    }
}

