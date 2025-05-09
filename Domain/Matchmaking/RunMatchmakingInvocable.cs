using Coravel.Invocable;

namespace QuickFinder.Domain.Matchmaking;

public class RunMatchmakingInvocable(
    ILogger<RunMatchmakingInvocable> logger,
    IServiceProvider serviceProvider
) : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        logger.LogInformation("Starting matchmaking job");

        var scope = serviceProvider.CreateScope();
        var groupMatchmakingService =
            scope.ServiceProvider.GetRequiredService<GroupMatchmakingService>();
        var matchmakingService = scope.ServiceProvider.GetRequiredService<MatchmakingService>();

        await groupMatchmakingService.DoMatching(CancellationToken);
        await matchmakingService.DoMatching(CancellationToken);
    }
}
