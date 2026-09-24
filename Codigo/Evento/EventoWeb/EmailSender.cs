using Core;
using Core.Service;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace EventoWeb
{
    public class EmailSender : IEmailService,IEmailSender
    {
        private readonly SmtpClient _client;
        private readonly string _from;
        private readonly IWebHostEnvironment _enviroment;
        private readonly ILogger<IEmailSender> _logger;

        public EmailSender(IConfiguration configuration, IWebHostEnvironment enviroment, ILogger<IEmailSender> logger)
        {
            _from = Environment.GetEnvironmentVariable("EMAIL_USER");
            _client = new SmtpClient
            {
                Host = Environment.GetEnvironmentVariable("EMAIL_SMTP"),
                Port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT")),
                Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("EMAIL_USER"), Environment.GetEnvironmentVariable("EMAIL_PASS")),
                EnableSsl = true
            };
            _enviroment = enviroment;
            _logger = logger;
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
        public async Task<bool> ModeloEmailReset(String token,Pessoa pessoa, String callbackUrl)
        {
            string email = pessoa.Email;
            string contato = "https://beacons.ai/itatechjr";
            string assunto = "Redefinição de Senha";
            string caminhoTemplate = Path.Combine(_enviroment.WebRootPath, "templates", "EmailRedefinicaoSenha.html");
            try
            {
                string mensagemHtml = await File.ReadAllTextAsync(caminhoTemplate);
                mensagemHtml = mensagemHtml
                .Replace("{{Nome}}", pessoa.Nome)
                .Replace("{{LinkCallback}}", callbackUrl)
                .Replace("{{LinkContato}}", contato);
                await SendEmailAsync(email, assunto, mensagemHtml);
              
              _logger.LogInformation("E-mail de redefinição de senha enviado com sucesso para {Email}", email);
              return true;

            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Erro ao enviar e-mail de redefinição de senha para {Email}", email);
               return false;

            }
        }
    }
}
