using HealthMate.Application.Abstractions.Identity.Ports;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace HealthMate.Infrastructure.Identity.Services;

public sealed class EmailService : IEmailService
{
    private readonly IConfiguration configuration;

    public EmailService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<string> SendEmailAsync(string emailReciever, string subject, string message)
    {
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(configuration["EmailSettings:DisplayName"], configuration["EmailSettings:Email"]));
        emailMessage.To.Add(new MailboxAddress(string.Empty, emailReciever));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart("plain") { Text = message };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            await client.ConnectAsync(configuration["EmailSettings:Host"], int.Parse(configuration["EmailSettings:Port"]), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(configuration["EmailSettings:Email"], configuration["EmailSettings:Password"]);
            await client.SendAsync(emailMessage);

            return "Email Sent";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
