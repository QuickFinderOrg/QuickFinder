using Coravel.Invocable;

namespace QuickFinder.Domain.Matchmaking;

public class RunMatchmakingInvocable(ILogger<RunMatchmakingInvocable> logger, IServiceProvider serviceProvider) : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        logger.LogInformation("Starting matchmaking job");

        var scope = serviceProvider.CreateScope();
        var matchmakingService = scope.ServiceProvider.GetRequiredService<MatchmakingService>();
        await matchmakingService.DoMatching(CancellationToken);
    }
}
