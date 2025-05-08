using Coravel;
using QuickFinder.Domain.Matchmaking;

namespace QuickFinder;

public static class SchedulerConfigurationProvider
{
    public const string DisableSchedulerKey = "DisableScheduler";

    public static void ConfigureScheduler(
        this IServiceProvider provider,
        IConfiguration configuration
    )
    {
        using var scope = provider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SchedulerConfiguration>>();

        if (configuration.GetValue(DisableSchedulerKey, false))
        {
            logger.LogWarning(
                "Scheduler disabled. Can be disabled using config '{key}=false'.",
                DisableSchedulerKey
            );
            return;
        }

        logger.LogInformation(
            "Scheduler enabled. Can be disabled using config '{key}=true'.",
            DisableSchedulerKey
        );

        provider
            .UseScheduler(scheduler =>
            {
                scheduler.Schedule<RunMatchmakingInvocable>().EveryThirtySeconds();
                scheduler.Schedule<DeleteUnusedGroupsInvocable>().EveryMinute();
            })
            .OnError(e =>
            {
                logger.LogError(e, "Error in scheduled task: {Message}", e.Message);
            });
    }
}

//used only for logging.
public class SchedulerConfiguration { }
