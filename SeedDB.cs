using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QuickFinder.Data;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

class SeedDB(
    UserService userService,
    UserManager<User> userManager,
    ApplicationDbContext db,
    IOptions<DiscordServiceOptions> discordOptions,
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
                },
                new Tester()
                {
                    Name = "Taylor",
                    Email = "taylor@example.org",
                    Password = "Password-123",
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
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Max",
                    Email = "max@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Lewis",
                    Email = "lewis@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Fernando",
                    Email = "fernando@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Valtteri",
                    Email = "valtteri@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Michael",
                    Email = "michael@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Alain",
                    Email = "alain@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Niki",
                    Email = "niki@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "James",
                    Email = "james@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Oscar",
                    Email = "oscar@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Sebastian",
                    Email = "sebastian@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Carlos",
                    Email = "carlos@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Janove",
                    Email = "janove@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Geir",
                    Email = "geir@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Terje",
                    Email = "terje@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Rune",
                    Email = "rune@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Helge",
                    Email = "helge@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Øyvind",
                    Email = "oyvind@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Jon",
                    Email = "jon@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Ben",
                    Email = "ben@example.org",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
            ]
        );

        var admin_accounts = new List<Admin>(
            [
                new Admin()
                {
                    Name = "Admin",
                    Email = "admin@quickfinder.no",
                    Password = "FerretEnjoyer-123",
                    IsAdmin = new Claim("IsAdmin", true.ToString()),
                },
            ]
        );

        var TestCourse1 = new Course() { Name = "DAT120" };
        var TestCourse2 = new Course() { Name = "DAT240" };

        var TestServer = new Server()
        {
            Id = ulong.Parse(discordOptions.Value.ServerId),
            Name = "QuickFinder Discord",
        };
        TestServer.Courses.Add(TestCourse1);

        db.Add(TestCourse1);
        db.Add(TestCourse2);

        db.Add(TestServer);

        foreach (var account in test_accounts)
        {
            var user = await userService.CreateUser(account.Email, account.Name, account.Password);
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
    }

    private record class Admin
    {
        public required string Name;
        public required string Email;
        public required string Password;
        public required Claim IsAdmin;
    }
}
