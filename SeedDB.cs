using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QuickFinder.Data;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

/// <summary>
/// Fills the database with test accounts, which are used to test the matchamking and group managament.
/// </summary>
class SeedDB(
    UserService userService,
    UserManager<User> userManager,
    ApplicationDbContext db,
    MatchmakingService matchmakingService
)
{
    public async void Seed()
    {
        if (userService.HasUsers())
        {
            return;
        }

        var test_accounts = new List<Tester>(
            [
                new Tester()
                {
                    Name = "Dave",
                    Email = "dave@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Taylor",
                    Email = "taylor@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Nate",
                    Email = "nate@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Pat",
                    Email = "pat@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Chris",
                    Email = "chris@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Rami",
                    Email = "rami@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Josh",
                    Email = "josh@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "William",
                    Email = "william@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Franz",
                    Email = "franz@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Monoco",
                    Email = "monoco@example.org",
                    Password = "Password-123",
                    availability = Availability.Daytime,
                    Languages = LanguageFlags.English | LanguageFlags.French,
                },
                new Tester()
                {
                    Name = "Noco",
                    Email = "noco@example.org",
                    Password = "Password-123",
                    availability = Availability.Daytime,
                    Languages = LanguageFlags.English | LanguageFlags.French,
                },
                new Tester()
                {
                    Name = "Lune",
                    Email = "lune@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.English | LanguageFlags.French,
                },
                new Tester()
                {
                    Name = "Gustave",
                    Email = "gustave@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.English | LanguageFlags.French,
                },
                new Tester()
                {
                    Name = "Maelle",
                    Email = "maelle@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.English | LanguageFlags.French,
                },
                new Tester()
                {
                    Name = "Esquie",
                    Email = "esquie@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Friday | DaysOfTheWeek.Weekends,
                    Languages = LanguageFlags.French,
                },
                new Tester()
                {
                    Name = "François",
                    Email = "franfran@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                    Languages = LanguageFlags.French,
                },
                new Tester()
                {
                    Name = "Verso",
                    Email = "verso@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.French | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Max",
                    Email = "max@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Lewis",
                    Email = "lewis@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Fernando",
                    Email = "fernando@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Valtteri",
                    Email = "valtteri@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Michael",
                    Email = "michael@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Alain",
                    Email = "alain@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Niki",
                    Email = "niki@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "James",
                    Email = "james@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Oscar",
                    Email = "oscar@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Sebastian",
                    Email = "sebastian@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                },
                new Tester()
                {
                    Name = "Carlos",
                    Email = "carlos@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.Weekends,
                    Languages = LanguageFlags.Spanish | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Rami I.",
                    Email = "rami.i@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.All,
                    Languages = LanguageFlags.Arabic,
                },
                new Tester()
                {
                    Name = "Bruce",
                    Email = "bruce@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.All,
                    Languages = LanguageFlags.Chinese,
                },
                new Tester()
                {
                    Name = "Brandon",
                    Email = "brandon@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Days = DaysOfTheWeek.All,
                    Languages = LanguageFlags.Chinese,
                },
                new Tester()
                {
                    Name = "Janove",
                    Email = "janove@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Geir",
                    Email = "geir@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Terje",
                    Email = "terje@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Rune",
                    Email = "rune@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Helge",
                    Email = "helge@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Øyvind",
                    Email = "oyvind@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Jon",
                    Email = "jon@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
                new Tester()
                {
                    Name = "Ben",
                    Email = "ben@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                    Languages = LanguageFlags.Norwegian | LanguageFlags.English,
                },
            ]
        );

        var admin_accounts = new List<Admin>(
            [
                new Admin()
                {
                    Name = "Admin",
                    Email = "admin@quickfinder.no",
                    Password = "Replace_This_Password_22",
                    IsAdmin = new Claim(ApplicationClaimTypes.IsAdmin, true.ToString()),
                },
            ]
        );

        var TestCourse1 = new Course() { Name = "DAT120" };
        var TestCourse2 = new Course() { Name = "DAT240", GroupSize = 4 };

        db.Add(TestCourse1);
        db.Add(TestCourse2);

        foreach (var account in test_accounts)
        {
            var user = await userService.CreateUser(account.Email, account.Name, account.Password);
            user.Preferences = new UserPreferences
            {
                GlobalAvailability = account.availability,
                Language = account.Languages,
                GlobalDays = account.Days,
            };
            await userManager.UpdateAsync(user);

            await matchmakingService.QueueForMatchmakingAsync(user.Id, TestCourse1.Id);
        }

        foreach (var account in admin_accounts)
        {
            var user = await userService.CreateUser(account.Email, account.Name, account.Password);
            await userManager.AddClaimAsync(user, account.IsAdmin);
        }

        await db.SaveChangesAsync();
    }

    private record class Tester
    {
        public required string Name;
        public required string Email;
        public required string Password;
        public Availability availability = Availability.Daytime;
        public DaysOfTheWeek Days = DaysOfTheWeek.All;
        public LanguageFlags Languages = LanguageFlags.English;
    }

    private record class Admin
    {
        public required string Name;
        public required string Email;
        public required string Password;
        public required Claim IsAdmin;
    }
}
