using Microsoft.Extensions.Options;

namespace QuickFinder.Domain.Matchmaking;

public class MatchmakingBackgroundService(ILogger<MatchmakingBackgroundService> logger, IServiceProvider serviceProvider, IOptions<MatchmakingOptions> options) : IHostedService, IDisposable
{
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MatchmakingBackgroundService is starting. Interval: {Interval}", options.Value.IntervalTimeSpan);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        Task.Run(() => DoLongRunningWork(_cts.Token), stoppingToken); // Pass the token

        return Task.CompletedTask;
    }

    private async Task DoLongRunningWork(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting matchmaking job");

                var scope = serviceProvider.CreateScope();
                var matchmakingService = scope.ServiceProvider.GetRequiredService<MatchmakingService>();
                await matchmakingService.DoMatching(cancellationToken);

                await Task.Delay((int)options.Value.IntervalTimeSpan.TotalMilliseconds, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogTrace("Long running work was cancelled.");
        }
        finally
        {
            // Always dispose of resources
            logger.LogTrace("Finally disposing of resources");
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MyBackgroundService is stopping.");

        // Cancel any background work
        _cts?.Cancel();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
