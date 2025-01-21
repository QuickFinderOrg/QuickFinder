using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity;
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

    public async void SeedDB(IServiceProvider serviceProvider)
    {
        var _userStore = new UserStore<User>(this);
        if (_userStore.Users.Any())
        {
            return;
        }
        var user = new User();


        var _userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        await _userStore.SetUserNameAsync(user, "dr.acula@bloodbank.us", CancellationToken.None);
        await _userStore.SetEmailConfirmedAsync(user, true);
        await _userStore.SetEmailAsync(user, "dr.acula@bloodbank.us", CancellationToken.None);
        var result = await _userManager.CreateAsync(user, "Hema_Globin42");


        var people = new List<Person> {
            new Person() { Id = Guid.NewGuid(), Name = "Van Hellsing", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Blade", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Nosferatu", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Dracula", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Sylvanas", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } }
        };

        AddRange(people);

        SaveChanges();
    }
}

