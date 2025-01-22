
using group_finder.Data;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace group_finder;

class SeedDB(UserService userService, ApplicationDbContext db)
{
    public async void Seed()
    {
        var _userStore = new UserStore<User>(db);
        if (_userStore.Users.Any())
        {
            return;
        }

        await userService.CreateUser();


        var people = new List<Person> {
            new Person() { Id = Guid.NewGuid(), Name = "Van Hellsing", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Blade", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Daytime, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Nosferatu", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Dracula", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } },
            new Person() { Id = Guid.NewGuid(), Name = "Sylvanas", UserId = Guid.NewGuid(), Criteria = new Criteria() { Availability = Availability.Afternoons, Language = "en" }, Preferences = new Preferences() { } }
        };

        db.AddRange(people);

        await db.SaveChangesAsync();
    }
}