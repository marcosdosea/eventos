using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Core;
using EventoWeb;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace EventoWebTests.Controllers;

[TestClass]
public class BipouApiControllerTests
{
    private ApiFactory factory = null!;
    private HttpClient client = null!;
    private const string Api = "/api/bipou/v1";
    private const string Senha = "BipouTeste123";

    [TestInitialize]
    public async Task Preparar()
    {
        factory = new ApiFactory();
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = false
        });
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        foreach (var (cpf, role) in new[] { ("11144477735", "GESTOR"), ("93541134780", "COLABORADOR"), ("52998224725", "ADMINISTRADOR"), ("12345678909", "USUARIO") })
        {
            var user = new UsuarioIdentity { UserName = cpf, Email = cpf + "@example.test", EmailConfirmed = true };
            Assert.IsTrue((await users.CreateAsync(user, Senha)).Succeeded);
            Assert.IsTrue((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        var db = scope.ServiceProvider.GetRequiredService<EventoContext>();
        await db.Database.EnsureCreatedAsync();
        db.Papels.AddRange(new Papel { Id = 2, Nome = "Gestor" }, new Papel { Id = 3, Nome = "Colaborador" }, new Papel { Id = 4, Nome = "Participante" });
        db.Tipoeventos.Add(new Tipoevento { Id = 1, Nome = "Curso" });
        db.Pessoas.AddRange(Pessoa(1, "11144477735"), Pessoa(2, "93541134780"), Pessoa(3, "52998224725"), Pessoa(4, "12345678909"));
        db.Eventos.AddRange(Evento(1, "A"), Evento(2, "A"), Evento(3, "F"));
        db.Subeventos.AddRange(Subevento(1, 1), Subevento(2, 2), Subevento(3, 1, "F"));
        var inscricaoPendente = Inscricao(4, 1, 4, "S");
        inscricaoPendente.ValorTotal = 10;
        // O cadastro de gestor/colaborador existente grava S ao atribuir o papel.
        db.Inscricaopessoaeventos.AddRange(Inscricao(1, 1, 2, "S"), Inscricao(2, 1, 3, "S"), Inscricao(1, 3, 2),
            Inscricao(1, 2, 3), Inscricao(2, 2, 3, "C"),
            Inscricao(3, 1, 4), Inscricao(3, 1, 4), inscricaoPendente);
        db.Inscricaopessoasubeventos.AddRange(new Inscricaopessoasubevento { IdPessoa = 3, IdSubEvento = 1, IdPapel = 4, Status = "A" },
            new Inscricaopessoasubevento { IdPessoa = 3, IdSubEvento = 2, IdPapel = 4, Status = "A" });
        await db.SaveChangesAsync();
    }

    [TestCleanup]
    public void Limpar() { client.Dispose(); factory.Dispose(); }

    [TestMethod]
    public async Task Sessao_BloqueiaEmailNaoConfirmadoSegundoFatorLockoutETokenExpirado()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        var user = (await users.FindByNameAsync("11144477735"))!;
        user.EmailConfirmed = false; await users.UpdateAsync(user);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Api + "/sessao", new { usuario = user.UserName, senha = Senha })).StatusCode);
        user.EmailConfirmed = true; user.TwoFactorEnabled = true; await users.UpdateAsync(user);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Api + "/sessao", new { usuario = user.UserName, senha = Senha })).StatusCode);
        user.TwoFactorEnabled = false; user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5); await users.UpdateAsync(user);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Api + "/sessao", new { usuario = user.UserName, senha = Senha })).StatusCode);
        var options = factory.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(IdentityConstants.BearerScheme);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.UserName!), new Claim(ClaimTypes.Role, "GESTOR") }, IdentityConstants.BearerScheme));
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1) }, IdentityConstants.BearerScheme);
        client.DefaultRequestHeaders.Authorization = new("Bearer", options.BearerTokenProtector.Protect(ticket));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(Api + "/eventos")).StatusCode);
    }

    [TestMethod]
    public async Task Sessao_LimitaTentativasPorOrigem()
    {
        HttpResponseMessage response = null!;
        for (var i = 0; i < 11; i++)
            response = await client.PostAsJsonAsync(Api + "/sessao", new { usuario = "inexistente", senha = Senha });
        Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [TestMethod]
    public async Task Sessao_RevogacaoDePapelGlobalTemEfeitoMesmoComTokenEmitido()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        var user = (await users.FindByNameAsync("11144477735"))!;
        Assert.IsTrue((await users.AddToRoleAsync(user, "COLABORADOR")).Succeeded);
        await Autenticar("11144477735");
        Assert.AreEqual(2, (await Json("/eventos")).GetArrayLength());

        Assert.IsTrue((await users.RemoveFromRoleAsync(user, "GESTOR")).Succeeded);
        var restantes = await Json("/eventos");
        Assert.AreEqual(1, restantes.GetArrayLength());
        Assert.AreEqual(2, restantes[0].GetProperty("idEvento").GetInt32());
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync(Api + "/eventos/1/participantes")).StatusCode);

        Assert.IsTrue((await users.RemoveFromRoleAsync(user, "COLABORADOR")).Succeeded);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync(Api + "/eventos")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync(Api + "/eventos/2/participantes")).StatusCode);
    }

    [TestMethod]
    public async Task Operador_CpfDaContaComMascaraEncontraVinculoSemMascara()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        var user = (await users.FindByNameAsync("11144477735"))!;
        Assert.IsTrue((await users.SetUserNameAsync(user, "111.444.777-35")).Succeeded);
        await Autenticar("111.444.777-35");
        Assert.AreEqual(1, (await Json("/eventos")).GetArrayLength());
    }

    [TestMethod]
    public async Task Autenticacao_RejeitaAnonimoTokenInvalidoSenhaErradaEAdministrador()
    {
        foreach (var path in new[] { "/eventos", "/eventos/1/subeventos", "/eventos/1/participantes", "/eventos/1/participantes/export-carga" })
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(Api + path)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalido");
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(Api + "/eventos")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Api + "/sessao", new { usuario = "11144477735", senha = "errada" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Api + "/sessao", new { usuario = "52998224725", senha = Senha })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Api + "/sessao", new { usuario = "12345678909", senha = Senha })).StatusCode);
    }

    [DataTestMethod]
    [DataRow("11144477735")]
    [DataRow("93541134780")]
    public async Task Operador_ConsultaApenasEventoAtivoComVinculoEParticipantesConfirmados(string usuario)
    {
        await Autenticar(usuario);
        var eventos = await Json("/eventos");
        Assert.AreEqual(1, eventos.GetArrayLength());
        Assert.AreEqual(1, eventos[0].GetProperty("idEvento").GetInt32());
        var subs = await Json("/eventos/1/subeventos");
        Assert.AreEqual(1, subs.GetArrayLength());
        var pessoas = await Json("/eventos/1/participantes?subEventoId=1&atualizadoApos=2026-11-06T00:00:00Z");
        Assert.AreEqual(1, pessoas.GetArrayLength());
        Assert.AreEqual(3, pessoas[0].GetProperty("idPessoa").GetInt32());
        Assert.AreEqual("52998224725", pessoas[0].GetProperty("cpf").GetString());
        Assert.AreEqual("Cracha 3", pessoas[0].GetProperty("nomeCracha").GetString());
        Assert.AreEqual(1, pessoas[0].GetProperty("subeventosInscritos").GetArrayLength());
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync(Api + "/eventos/1/participantes?subEventoId=2")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.GetAsync(Api + "/eventos/1/participantes?atualizadoApos=abc")).StatusCode);
        foreach (var path in new[] { "/subeventos", "/participantes", "/participantes/export-carga" })
            Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync(Api + "/eventos/2" + path)).StatusCode);
    }

    [TestMethod]
    public async Task Etag_RefleteMudancaDeNomeCancelamentoERevogacaoAntesDe304()
    {
        await Autenticar("11144477735");
        var inicial = await client.GetAsync(Api + "/eventos/1/participantes");
        var etag = inicial.Headers.ETag!.ToString();
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", "W/\"outro\", " + etag);
        Assert.AreEqual(HttpStatusCode.NotModified, (await client.GetAsync(Api + "/eventos/1/participantes")).StatusCode);
        await Alterar(db => db.Pessoas.Find((uint)3)!.Nome = "Nome corrigido");
        var alterada = await client.GetAsync(Api + "/eventos/1/participantes");
        Assert.AreEqual(HttpStatusCode.OK, alterada.StatusCode);
        Assert.AreNotEqual(etag, alterada.Headers.ETag!.ToString());
        await Alterar(db => db.Inscricaopessoasubeventos.Single(i => i.IdPessoa == 3 && i.IdSubEvento == 1).Status = "C");
        Assert.AreEqual(0, (await Json("/eventos/1/participantes"))[0].GetProperty("subeventosInscritos").GetArrayLength());
        await Alterar(db => db.Inscricaopessoaeventos.Where(i => i.IdPessoa == 3 && i.IdEvento == 1).ToList().ForEach(i => i.Status = "C"));
        Assert.AreEqual(0, (await Json("/eventos/1/participantes")).GetArrayLength());
        await Alterar(db => db.Inscricaopessoaeventos.Single(i => i.IdPessoa == 1 && i.IdEvento == 1).Status = "A");
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(Api + "/eventos/1/participantes")).StatusCode);
        await Alterar(db => db.Inscricaopessoaeventos.Single(i => i.IdPessoa == 1 && i.IdEvento == 1).Status = "C");
        client.DefaultRequestHeaders.Remove("If-None-Match");
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", "*");
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync(Api + "/eventos/1/participantes")).StatusCode);
    }

    [TestMethod]
    public async Task Participantes_SolicitacoesGratuitasEntramEPendenciasPagasFicamFora()
    {
        await Alterar(db =>
        {
            for (uint id = 5; id <= 12; id++)
                db.Pessoas.Add(Pessoa(id, id.ToString("D11")));

            var pagaPendente = Inscricao(6, 1, 4, "S");
            pagaPendente.ValorTotal = 25;
            db.Inscricaopessoaeventos.AddRange(
                Inscricao(5, 1, 4, "S"), pagaPendente, Inscricao(7, 1, 4, "C"),
                Inscricao(8, 1, 4, "S"), Inscricao(9, 1, 4),
                Inscricao(10, 1, 4, "S"), Inscricao(10, 1, 4),
                Inscricao(11, 1, 4, "S"), Inscricao(12, 1, 4));
            db.Inscricaopessoasubeventos.AddRange(
                new Inscricaopessoasubevento { IdPessoa = 8, IdSubEvento = 1, IdPapel = 4, Status = "S", Valor = 30 },
                new Inscricaopessoasubevento { IdPessoa = 9, IdSubEvento = 1, IdPapel = 4, Status = "S", Valor = 30 },
                new Inscricaopessoasubevento { IdPessoa = 11, IdSubEvento = 1, IdPapel = 4, Status = "S", Valor = 0 },
                new Inscricaopessoasubevento { IdPessoa = 12, IdSubEvento = 1, IdPapel = 4, Status = "C", Valor = 0 });
        });

        await Autenticar("11144477735");
        var participantes = await Json("/eventos/1/participantes");
        var ids = participantes.EnumerateArray().Select(p => p.GetProperty("idPessoa").GetInt32()).ToArray();
        CollectionAssert.AreEqual(new[] { 3, 5, 9, 10, 11, 12 }, ids);
        Assert.AreEqual("S", participantes.EnumerateArray().Single(p => p.GetProperty("idPessoa").GetInt32() == 5).GetProperty("statusInscricao").GetString());
        Assert.AreEqual("A", participantes.EnumerateArray().Single(p => p.GetProperty("idPessoa").GetInt32() == 10).GetProperty("statusInscricao").GetString());
        Assert.AreEqual(0, participantes.EnumerateArray().Single(p => p.GetProperty("idPessoa").GetInt32() == 9).GetProperty("subeventosInscritos").GetArrayLength());
        Assert.AreEqual(0, participantes.EnumerateArray().Single(p => p.GetProperty("idPessoa").GetInt32() == 12).GetProperty("subeventosInscritos").GetArrayLength());
        var subevento = await Json("/eventos/1/participantes?subEventoId=1");
        CollectionAssert.AreEqual(new[] { 3, 11 }, subevento.EnumerateArray().Select(p => p.GetProperty("idPessoa").GetInt32()).ToArray());

        var pacote = await client.GetAsync(Api + "/eventos/1/participantes/export-carga");
        await using var stream = new GZipStream(await pacote.Content.ReadAsStreamAsync(), CompressionMode.Decompress);
        var carga = (await JsonDocument.ParseAsync(stream)).RootElement.GetProperty("participantes");
        CollectionAssert.AreEqual(ids, carga.EnumerateArray().Select(p => p.GetProperty("idPessoa").GetInt32()).ToArray());
    }

    [TestMethod]
    public async Task Participantes_MudancaDeCustoOuCancelamentoAtualizaCargaEEtag()
    {
        await Autenticar("11144477735");
        var inicial = await client.GetAsync(Api + "/eventos/1/participantes");
        var etag = inicial.Headers.ETag!.ToString();
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", etag);
        Assert.AreEqual(HttpStatusCode.NotModified, (await client.GetAsync(Api + "/eventos/1/participantes")).StatusCode);

        // A mesma inscrição solicitada passa a ser confirmada pela regra de valor zero.
        await Alterar(db => db.Inscricaopessoaeventos.Single(i => i.IdPessoa == 4 && i.IdEvento == 1).ValorTotal = 0);
        var liberada = await client.GetAsync(Api + "/eventos/1/participantes");
        Assert.AreEqual(HttpStatusCode.OK, liberada.StatusCode);
        Assert.AreNotEqual(etag, liberada.Headers.ETag!.ToString());
        var lista = await liberada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("S", lista.EnumerateArray().Single(p => p.GetProperty("idPessoa").GetInt32() == 4).GetProperty("statusInscricao").GetString());

        client.DefaultRequestHeaders.Remove("If-None-Match");
        client.DefaultRequestHeaders.IfNoneMatch.Add(liberada.Headers.ETag!);
        await Alterar(db => db.Inscricaopessoaeventos.Single(i => i.IdPessoa == 4 && i.IdEvento == 1).Status = "C");
        var cancelada = await client.GetAsync(Api + "/eventos/1/participantes");
        Assert.AreEqual(HttpStatusCode.OK, cancelada.StatusCode);
        Assert.AreNotEqual(liberada.Headers.ETag, cancelada.Headers.ETag);
        var listaCancelada = await cancelada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsFalse(listaCancelada.EnumerateArray().Any(p => p.GetProperty("idPessoa").GetInt32() == 4));
    }

    [TestMethod]
    public async Task Compactacao_ListaGzipBrotliEExportacaoOffline()
    {
        await Autenticar("11144477735");
        foreach (var encoding in new[] { "gzip", "br" })
        {
            client.DefaultRequestHeaders.AcceptEncoding.Clear();
            client.DefaultRequestHeaders.AcceptEncoding.Add(new(encoding));
            var resposta = await client.GetAsync(Api + "/eventos/1/participantes");
            Assert.IsTrue(resposta.Content.Headers.ContentEncoding.Contains(encoding));
            await using var bytes = await resposta.Content.ReadAsStreamAsync();
            await using Stream descompactado = encoding == "gzip" ? new GZipStream(bytes, CompressionMode.Decompress) : new BrotliStream(bytes, CompressionMode.Decompress);
            Assert.AreEqual(1, (await JsonDocument.ParseAsync(descompactado)).RootElement.GetArrayLength());
        }
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        var pacote = await client.GetAsync(Api + "/eventos/1/participantes/export-carga");
        Assert.AreEqual("application/gzip", pacote.Content.Headers.ContentType!.MediaType);
        await using var stream = new GZipStream(await pacote.Content.ReadAsStreamAsync(), CompressionMode.Decompress);
        var carga = (await JsonDocument.ParseAsync(stream)).RootElement;
        Assert.AreEqual(1, carga.GetProperty("versao").GetInt32());
        Assert.AreEqual(1, carga.GetProperty("participantes").GetArrayLength());
        Assert.AreEqual(1, carga.GetProperty("subeventos").GetArrayLength());
        client.DefaultRequestHeaders.IfNoneMatch.Add(pacote.Headers.ETag!);
        Assert.AreEqual(HttpStatusCode.NotModified, (await client.GetAsync(Api + "/eventos/1/participantes/export-carga")).StatusCode);
    }

    [TestMethod]
    public async Task Carga_ExportaMaisDeCincoMilInscritosEmMenosDeDoisSegundos()
    {
        await Autenticar("11144477735");
        await Alterar(db =>
        {
            for (uint id = 10; id < 5011; id++)
            {
                db.Pessoas.Add(Pessoa(id, id.ToString("D11")));
                db.Inscricaopessoaeventos.Add(Inscricao(id, 1, 4));
            }
        });
        // Aquece o pipeline/JIT antes de medir a carga de dados.
        await client.GetAsync(Api + "/eventos");
        var tempo = Stopwatch.StartNew();
        var response = await client.GetAsync(Api + "/eventos/1/participantes/export-carga");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        tempo.Stop();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        await using var gzip = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
        var carga = (await JsonDocument.ParseAsync(gzip)).RootElement;
        Assert.AreEqual(5002, carga.GetProperty("participantes").GetArrayLength());
        Assert.IsTrue(tempo.Elapsed < TimeSpan.FromSeconds(2), $"Carga: {tempo.Elapsed.TotalMilliseconds:F0} ms");
        Console.WriteLine($"Carga de 5002 participantes: {tempo.Elapsed.TotalMilliseconds:F0} ms, {bytes.Length} bytes (gzip), {factory.Provider}");
    }

    private async Task Autenticar(string usuario)
    {
        var response = await client.PostAsJsonAsync(Api + "/sessao", new { usuario, senha = Senha });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }

    private async Task<JsonElement> Json(string caminho)
    {
        var response = await client.GetAsync(Api + caminho);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task Alterar(Action<EventoContext> acao)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventoContext>();
        acao(db); await db.SaveChangesAsync();
    }

    private static Pessoa Pessoa(uint id, string cpf) => new() { Id = id, Cpf = cpf, Nome = "Pessoa " + id, NomeCracha = "Cracha " + id, Email = id + "@example.test" };
    private static Evento Evento(uint id, string status) => new() { Id = id, Nome = "Evento " + id, Status = status };
    private static Inscricaopessoaevento Inscricao(uint pessoa, uint evento, int papel, string status = "A")
        => new() { IdPessoa = pessoa, IdEvento = evento, IdPapel = papel, Status = status, DataInscricao = new DateTime(2026, 11, 6) };
    private static Subevento Subevento(uint id, uint evento, string status = "A") => new()
    {
        Id = id, IdEvento = evento, Nome = "Curso " + id, Descricao = "Teste", Status = status, IdTipoEvento = 1,
        DataInicio = new DateTime(2026, 11, 6, 8, 0, 0), DataFim = new DateTime(2026, 11, 6, 10, 0, 0),
        DataInicioInscricao = new DateTime(2026, 11, 1), DataFimInscricao = new DateTime(2026, 11, 5)
    };

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly string identityDatabase = Guid.NewGuid().ToString();
        // Opcional: conexão ao servidor MySQL local, sem banco selecionado.
        private readonly string? mysqlServer = Environment.GetEnvironmentVariable("BORALA_TEST_MYSQL_SERVER");
        private readonly string mysqlEvento = "borala_test_evento_" + Guid.NewGuid().ToString("N");
        private readonly string mysqlIdentity = "borala_test_identity_" + Guid.NewGuid().ToString("N");
        private string? eventoConnection;
        private string? identityConnection;
        private bool disposed;
        public string Provider => eventoConnection == null ? "SQLite" : "MySQL";

        public ApiFactory()
        {
            if (mysqlServer == null)
            {
                connection.Open();
                Service.Tests.SqliteSchemaEvento.Criar(connection);
                return;
            }
            var builder = new MySqlConnectionStringBuilder(mysqlServer);
            if ((builder.Server != "127.0.0.1" && builder.Server != "localhost") || !string.IsNullOrEmpty(builder.Database))
                throw new InvalidOperationException("Informe um servidor MySQL local sem banco selecionado.");
            using var server = new MySqlConnection(mysqlServer);
            server.Open();
            using var command = server.CreateCommand();
            try
            {
                command.CommandText = $"CREATE DATABASE `{mysqlEvento}`; CREATE DATABASE `{mysqlIdentity}`;";
                command.ExecuteNonQuery();
                builder.Database = mysqlEvento; eventoConnection = builder.ConnectionString;
                builder.Database = mysqlIdentity; identityConnection = builder.ConnectionString;
                var options = new DbContextOptionsBuilder<IdentityContext>().UseMySQL(identityConnection).Options;
                using var identity = new IdentityContext(options);
                identity.Database.EnsureCreated();
            }
            catch
            {
                command.CommandText = $"DROP DATABASE IF EXISTS `{mysqlEvento}`; DROP DATABASE IF EXISTS `{mysqlIdentity}`;";
                command.ExecuteNonQuery();
                throw;
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<EventoContext>>();
                services.RemoveAll<DbContextOptions<IdentityContext>>();
                services.AddDbContext<EventoContext>(o => { if (eventoConnection == null) o.UseSqlite(connection); else o.UseMySQL(eventoConnection); });
                services.AddDbContext<IdentityContext>(o => { if (identityConnection == null) o.UseInMemoryDatabase(identityDatabase); else o.UseMySQL(identityConnection); });
                services.AddLogging(o => o.AddFilter("Microsoft", LogLevel.Warning));
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            base.Dispose(disposing);
            if (!disposing) return;
            connection.Dispose();
            if (eventoConnection != null)
            {
                MySqlConnection.ClearAllPools();
                using var server = new MySqlConnection(mysqlServer); server.Open();
                using var command = server.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS `{mysqlEvento}`; DROP DATABASE IF EXISTS `{mysqlIdentity}`;";
                command.ExecuteNonQuery();
            }
        }
    }
}
