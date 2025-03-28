using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace QuickFinder.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator mediator) : IdentityDbContext<User>(options)
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // ignore events if no dispatcher provided
        if (mediator == null) return result;

        // dispatch events only if save was successful
        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.Events.Count != 0)
            .ToArray();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.Events.ToArray();
            entity.Events.Clear();
            foreach (var domainEvent in events)
            {
                await mediator.Publish(domainEvent, cancellationToken);
            }
        }
        return result;
    }

}

