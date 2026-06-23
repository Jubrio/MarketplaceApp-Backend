using MarketplaceApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace MarketplaceApp.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration config)
    {
        var section = config.GetSection("EmailSettings");
        _smtpHost = section["SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(section["SmtpPort"] ?? "587");
        _smtpUser = section["SmtpUser"] ?? "";
        _smtpPass = section["SmtpPass"] ?? "";
        _fromEmail = section["FromEmail"] ?? _smtpUser;
        _fromName = section["FromName"] ?? "Marketplace";

        if (string.IsNullOrWhiteSpace(_fromEmail))
            _fromEmail = "noreply@marketplace.com";
    }

    public async Task SendOrderStatusEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        Console.WriteLine($"[EMAIL DEBUG] toEmail: '{toEmail}', toName: '{toName}'");

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            Console.WriteLine("[EMAIL DEBUG] toEmail est vide, abandon.");
            return;
        }
        if (string.IsNullOrWhiteSpace(toName))
        {
            Console.WriteLine("[EMAIL DEBUG] toName est vide, remplacé par 'Client'.");
            toName = "Client";
        }

        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_smtpUser, _smtpPass),
            Timeout = 10000 
        };

        var mail = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(toEmail, toName));

        await client.SendMailAsync(mail);
        Console.WriteLine("[EMAIL DEBUG] Email envoyé avec succès.");
    }
}