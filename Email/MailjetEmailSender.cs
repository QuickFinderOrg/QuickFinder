using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace QuickFinder.Email;

public class MailJetEmailSender(IOptions<MailjetOptions> options) : IEmailSender
{
    private readonly MailjetOptions _options = options.Value;

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        var client = new MailjetClient(_options.Id, _options.Secret);

        var request = new MailjetRequest { Resource = Send.Resource }
            .Property(Send.FromEmail, _options.SenderEmail ?? "quickfinder@example.com")
            .Property(Send.FromName, "QuickFinder")
            .Property(Send.Subject, subject)
            .Property(Send.HtmlPart, body)
            .Property(Send.Recipients, new JArray { new JObject { { "Email", email } } });

        await client.PostAsync(request);
    }
}

public class MailjetOptions
{
    public const string Mailjet = "Mailjet";

    public string Id { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}
