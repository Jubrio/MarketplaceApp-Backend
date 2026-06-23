namespace MarketplaceApp.Application.Interfaces;

public interface IEmailService
{
    Task SendOrderStatusEmailAsync(string toEmail, string toName, string subject, string htmlBody);
}