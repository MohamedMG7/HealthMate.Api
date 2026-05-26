namespace HealthMate.Application.Abstractions.Identity.Ports;

public interface IEmailService
{
    Task<string> SendEmailAsync(string email, string subject, string message);
}
