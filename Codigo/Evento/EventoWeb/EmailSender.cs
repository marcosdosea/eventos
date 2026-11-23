using Core;
using Core.Service;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

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

            return EnviarEmailAsync(email, subject, htmlMessage);
        }

        public async Task EnviarEmailAsync(string email, string assunto, string mensagemHtml)
        {
            if (_client == null || string.IsNullOrWhiteSpace(_from))
            {
                throw new InvalidOperationException("O serviço SMTP de envio de e-mail não está configurado.");
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_from),
                Subject = assunto,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            var streamsParaDispor = new List<MemoryStream>();
            try
            {
                var linkedResources = new List<LinkedResource>();
                int index = 0;

                string htmlProcessado = Regex.Replace(
                    mensagemHtml,
                    @"src\s*=\s*[""']data:(?<mime>image/[a-zA-Z0-9.+-]+);base64,(?<data>[A-Za-z0-9+/=\s]+)[""']",
                    match =>
                    {
                        var mime = match.Groups["mime"].Value.Trim().ToLowerInvariant();
                        var base64 = Regex.Replace(match.Groups["data"].Value, @"\s+", "");
                        try
                        {
                            var bytes = Convert.FromBase64String(base64);
                            var msInline = new MemoryStream(bytes);
                            streamsParaDispor.Add(msInline);

                            index++;
                            var ext = (mime.Contains("jpeg") || mime.Contains("jpg")) ? "jpg" : "png";
                            var cid = $"borala_img_{index}_{Guid.NewGuid():N}";
                            var resource = new LinkedResource(msInline, mime)
                            {
                                ContentId = cid,
                                TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                            };
                            resource.ContentType.MediaType = mime;
                            resource.ContentType.Name = $"credencial_{index}.{ext}";
                            linkedResources.Add(resource);

                            return $"src=\"cid:{cid}\"";
                        }
                        catch
                        {
                            return match.Value;
                        }
                    },
                    RegexOptions.IgnoreCase);

                if (linkedResources.Any())
                {
                    var htmlView = AlternateView.CreateAlternateViewFromString(
                        htmlProcessado,
                        Encoding.UTF8,
                        System.Net.Mime.MediaTypeNames.Text.Html);

                    foreach (var res in linkedResources)
                    {
                        htmlView.LinkedResources.Add(res);
                    }

                    mailMessage.AlternateViews.Add(htmlView);
                    mailMessage.Body = htmlProcessado;
                }
                else
                {
                    mailMessage.Body = mensagemHtml;
                }

                await _client.SendMailAsync(mailMessage);
            }
            finally
            {
                foreach (var ms in streamsParaDispor)
                {
                    ms.Dispose();
                }
            }
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
                await EnviarEmailAsync(email, assunto, mensagemHtml);
                _logger.LogInformation("E-mail de {assunto} enviado com sucesso para {Email}", assunto, email);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar e-mail de {assunto} para {Email}", assunto, email);
                return false;

            }
        }
    }
}
