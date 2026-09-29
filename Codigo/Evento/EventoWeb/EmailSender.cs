using Core;
using Core.Service;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace EventoWeb
{
    public class EmailSender : IEmailService,IEmailSender
    {
        private readonly SmtpClient? _client;
        private readonly string _from;
        private readonly IWebHostEnvironment _enviroment;
        private readonly ILogger<IEmailSender> _logger;

        public EmailSender(IConfiguration configuration, IWebHostEnvironment enviroment, ILogger<IEmailSender> logger)
        {
            _enviroment = enviroment;
            _logger = logger;

            _from = Environment.GetEnvironmentVariable("EMAIL_USER")
                ?? configuration["Smtp:From"]
                ?? configuration["Smtp:Username"]
                ?? string.Empty;

            var host = Environment.GetEnvironmentVariable("EMAIL_SMTP")
                ?? configuration["Smtp:Host"];
            var portStr = Environment.GetEnvironmentVariable("EMAIL_PORT")
                ?? configuration["Smtp:Port"];
            var pass = Environment.GetEnvironmentVariable("EMAIL_PASS")
                ?? configuration["Smtp:Password"]
                ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(host))
            {
                int port = int.TryParse(portStr, out var parsedPort) && parsedPort > 0 ? parsedPort : 587;
                _client = new SmtpClient
                {
                    Host = host,
                    Port = port,
                    Credentials = new NetworkCredential(_from, pass),
                    EnableSsl = true
                };
            }
        }
   
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (_client == null || string.IsNullOrWhiteSpace(_from))
            {
                _logger.LogWarning("Configuração SMTP ausente. E-mail para {Email} ({Subject}) não pôde ser enviado.", email, subject);
                return Task.CompletedTask;
            }

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
