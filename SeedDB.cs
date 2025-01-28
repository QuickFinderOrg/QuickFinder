
using group_finder.Data;
using group_finder.Domain.Matchmaking;

namespace group_finder;

class SeedDB(UserService userService, ApplicationDbContext db, MatchmakingService matchmakingService)
{
    public async void Seed()
    {
        if (userService.HasUsers())
        {
            return;
        }

        var test_accounts = new List<Tester>([
            new Tester() { Name = "Van Helsing", Email = "van.helsing@gmail.com", Password = "Password-123" },
            new Tester() { Name = "Blade", Email = "uphill@iceskating.com", Password = "Password-123" },
            new Tester() { Name = "Nosferatu", Email = "nosferatu1922@gmail.com", Password = "Password-123", availability=Availability.Afternoons },
            new Tester() { Name = "Dracula", Email = "dr.acula@bloodbank.us", Password = "Password-123", availability=Availability.Afternoons },
            new Tester() { Name = "Sylvanas", Email = "sylvanas.windrunner@aol.com", Password = "Password-123",  availability=Availability.Afternoons },
        ]);

        var TestCourse1 = new Course() { Name = "DAT120" };
        var TestCourse2 = new Course() { Name = "DAT240" };

        db.Add(TestCourse1);
        db.Add(TestCourse2);

        foreach (var account in test_accounts)
        {
            var user = await userService.CreateUser(account.Email, account.Name, account.Password);
            await matchmakingService.AddToWaitlist(user, TestCourse1);
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
}