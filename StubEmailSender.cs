namespace group_finder;

public class StubEmailSender(ILogger logger) : IEmailSender
{

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        logger.LogInformation($"MAILTO={email} SUBJECT=subject BODY={body}");

        await Task.CompletedTask;
    }
}