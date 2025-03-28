using MailKit.Net.Smtp;
using MimeKit;

namespace QuickFinder;



public class SMTPEmailSender(ILogger<StubEmailSender> logger) : IEmailSender
{

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        logger.LogInformation($"MAILTO={email} SUBJECT=subject BODY={body}");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Joey Tribbiani", "joey@friends.com"));
        message.To.Add(new MailboxAddress("Mrs. Chanandler Bong", "chandler@friends.com"));
        message.Subject = "How you doin'?";

        message.Body = new TextPart("plain")
        {
            Text = @"Hey Chandler,

I just wanted to let you know that Monica and I were going to go play some paintball, you in?

-- Joey"
        };

        using (var client = new SmtpClient())
        {
            client.Connect("localhost", 25, false);

            client.Send(message);
            client.Disconnect(true);
        }

        await Task.CompletedTask;
    }
}