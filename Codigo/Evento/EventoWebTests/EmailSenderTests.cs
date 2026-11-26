using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace EventoWeb.Tests;

[TestClass]
public class EmailSenderTests
{
    [TestMethod]
    public async Task TentarEnviarEmailAsync_SemConfiguracaoSmtp_RetornaFalseSemLancarExcecao()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:Host"] = "",
                ["Smtp:From"] = ""
            })
            .Build();

        var env = new Mock<IWebHostEnvironment>();
        var logger = new Mock<ILogger<IEmailSender>>();

        var prevHost = Environment.GetEnvironmentVariable("EMAIL_SMTP");
        var prevUser = Environment.GetEnvironmentVariable("EMAIL_USER");
        try
        {
            Environment.SetEnvironmentVariable("EMAIL_SMTP", null);
            Environment.SetEnvironmentVariable("EMAIL_USER", null);

            var sender = new EmailSender(config, env.Object, logger.Object);
            var resultado = await sender.TentarEnviarEmailAsync("participante@teste.com", "Assunto", "<p>Teste</p>");

            Assert.IsFalse(resultado);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EMAIL_SMTP", prevHost);
            Environment.SetEnvironmentVariable("EMAIL_USER", prevUser);
        }
    }

    [TestMethod]
    public async Task EnviarEmailAsync_SemConfiguracaoSmtp_LancaInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:Host"] = "",
                ["Smtp:From"] = ""
            })
            .Build();

        var env = new Mock<IWebHostEnvironment>();
        var logger = new Mock<ILogger<IEmailSender>>();

        var prevHost = Environment.GetEnvironmentVariable("EMAIL_SMTP");
        var prevUser = Environment.GetEnvironmentVariable("EMAIL_USER");
        try
        {
            Environment.SetEnvironmentVariable("EMAIL_SMTP", null);
            Environment.SetEnvironmentVariable("EMAIL_USER", null);

            var sender = new EmailSender(config, env.Object, logger.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sender.EnviarEmailAsync("participante@teste.com", "Assunto", "<p>Teste</p>"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("EMAIL_SMTP", prevHost);
            Environment.SetEnvironmentVariable("EMAIL_USER", prevUser);
        }
    }

    [TestMethod]
    public async Task TentarEnviarEmailAsync_DestinatarioVazio_RetornaFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:Host"] = "smtp.exemplo.local",
                ["Smtp:Port"] = "587",
                ["Smtp:From"] = "noreply@borala.local",
                ["Smtp:Password"] = "segredo"
            })
            .Build();

        var env = new Mock<IWebHostEnvironment>();
        var logger = new Mock<ILogger<IEmailSender>>();

        var sender = new EmailSender(config, env.Object, logger.Object);
        var resultado = await sender.TentarEnviarEmailAsync("   ", "Assunto", "<p>Teste</p>");

        Assert.IsFalse(resultado);
    }
}
