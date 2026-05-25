
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace HealthMate.Application.Manager.AccountManager
{
	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

		public async Task<string> SendEmailAsync(string emailReciever, string subject, string message)
		{
			var emailMessage = new MimeMessage();
			emailMessage.From.Add(new MailboxAddress(_configuration["EmailSettings:DisplayName"], _configuration["EmailSettings:Email"]));
			emailMessage.To.Add(new MailboxAddress("", emailReciever));
			emailMessage.Subject = subject;
			emailMessage.Body = new TextPart("plain") { Text = message};
			using (var client = new MailKit.Net.Smtp.SmtpClient())
			{
				try
				{
					// Connect to SMTP
					await client.ConnectAsync(_configuration["EmailSettings:Host"], int.Parse(_configuration["EmailSettings:Port"]), SecureSocketOptions.StartTls);
					//Authenticate
					await client.AuthenticateAsync(_configuration["EmailSettings:Email"], _configuration["EmailSettings:Password"]);
					// send email
					await client.SendAsync(emailMessage);

					return "Email Sent";
				}
				catch (Exception ex)
				{
					return ex.ToString();
				}
				finally {
					if (client.IsConnected) {
						await client.DisconnectAsync(true);
					}
				}

			}
		}
	}
}
