namespace group_finder;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string body);
}