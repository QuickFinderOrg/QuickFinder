using Coravel.Invocable;
using QuickFinder.Domain.DiscordDomain;

namespace QuickFinder.Domain.Matchmaking;

public class DeleteUnusedGroupsInvocable(
    ILogger<DeleteUnusedGroupsInvocable> logger,
    IServiceProvider serviceProvider
) : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        logger.LogInformation("Deleting unused channels");

        var scope = serviceProvider.CreateScope();
        var discordService = scope.ServiceProvider.GetRequiredService<DiscordService>();
        await discordService.DeleteUnusedGroupChannels();
    }
}
