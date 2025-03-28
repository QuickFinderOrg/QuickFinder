namespace QuickFinder;

public class StubEmailSender(ILogger<StubEmailSender> logger) : IEmailSender
{

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        logger.LogInformation($"MAILTO={email} SUBJECT=subject BODY={body}");

        await Task.CompletedTask;
    }
}