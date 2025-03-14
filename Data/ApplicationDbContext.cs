using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace group_finder.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Channel> DiscordChannels { get; set; } = null!;
    public DbSet<Server> DiscordServers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }

}

