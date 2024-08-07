using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace EventoWeb
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpClient _client;
        private readonly string _from;

        public EmailSender(IConfiguration configuration)
        {
            _from = configuration["Smtp:From"];
            _client = new SmtpClient
            {
                Host = configuration["Smtp:Host"],
                Port = int.Parse(configuration["Smtp:Port"]),
                Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]),
                EnableSsl = true
            };
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_from),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            return _client.SendMailAsync(mailMessage);
        }
    }
}
