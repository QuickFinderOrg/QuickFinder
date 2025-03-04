using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;

namespace group_finder;

public class MailJetEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public MailJetEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;

    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        var client = new MailjetClient(
            _configuration["Mailjet:ApiKey"],
            _configuration["Mailjet:ApiSecret"]
        );

        var request = new MailjetRequest
        {
            Resource = Send.Resource
        }
        .Property(Send.FromEmail, _configuration["EmailSender"])
        .Property(Send.FromName, "QuickFinder")
        .Property(Send.Subject, subject)
        .Property(Send.HtmlPart, body)
        .Property(Send.Recipients, new JArray {
            new JObject {
                { "Email", email }
            }
        });

        await client.PostAsync(request);
    }
}