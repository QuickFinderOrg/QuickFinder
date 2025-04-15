using Coravel.Invocable;

namespace QuickFinder.Domain.Matchmaking;

public class RunGroupMatchmakingInvocable(
    ILogger<RunGroupMatchmakingInvocable> logger,
    IServiceProvider serviceProvider
) : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        logger.LogInformation("Starting group matchmaking job");

        var scope = serviceProvider.CreateScope();
        var groupMatchmakingService =
            scope.ServiceProvider.GetRequiredService<GroupMatchmakingService>();
        await groupMatchmakingService.DoMatching(CancellationToken);
    }
}
