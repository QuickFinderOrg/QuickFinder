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
    TicketRepository ticketRepository
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
                    Name = "Van Helsing",
                    Email = "van.helsing@gmail.com",
                    Password = "Password-123",
                },
                new Tester()
                {
                    Name = "Blade",
                    Email = "uphill@iceskating.com",
                    Password = "Password-123",
                },
                new Tester()
                {
                    Name = "Nosferatu",
                    Email = "nosferatu1922@gmail.com",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Dracula",
                    Email = "dr.acula@bloodbank.us",
                    Password = "Password-123",
                    availability = Availability.Afternoons,
                },
                new Tester()
                {
                    Name = "Sylvanas",
                    Email = "sylvanas.windrunner@aol.com",
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
            await ticketRepository.AddToWaitlist(user, TestCourse1);
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
