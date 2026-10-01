using Core;
using Core.Service;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Net;
using System.Net.Mail;
using static QRCoder.PayloadGenerator.SwissQrCode;

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
            return await ModeloEmail(token, pessoa, callbackUrl, email, contato, assunto, caminhoTemplate);
        }

        public async Task<bool> ModeloConfirmEmail(String token, Pessoa pessoa, String callbackUrl)
        {
            string email = pessoa.Email;
            string contato = "https://beacons.ai/itatechjr";
            string assunto = "Confirmação de E-mail";
            string caminhoTemplate = Path.Combine(_enviroment.WebRootPath, "templates", "ConfirmaEmail.html");

            return await ModeloEmail(token, pessoa, callbackUrl, email, contato, assunto, caminhoTemplate);
        }

        public async Task<bool> ModeloEmail(String token, Pessoa pessoa, String callbackUrl, string email, string contato, string assunto, string caminhoTemplate)
        {
            try
            {
                string mensagemHtml = await File.ReadAllTextAsync(caminhoTemplate);
                mensagemHtml = mensagemHtml
                .Replace("{{Nome}}", pessoa.Nome)
                .Replace("{{LinkCallback}}", callbackUrl)
                .Replace("{{LinkContato}}", contato);
                await SendEmailAsync(email, assunto, mensagemHtml);

                if (_client == null || string.IsNullOrWhiteSpace(_from))
                {
                    return false;
                }
                _logger.LogInformation("E-mail de {assunto} enviado com sucesso para {Email}", assunto, email);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar e-mail de {assunto} para {Email}", assunto, email);

            }
        }
    }
}
