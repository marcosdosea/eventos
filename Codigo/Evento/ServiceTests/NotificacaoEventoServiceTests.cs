using Core;
using Core.DTO;
using Core.Service;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Tests;

[TestClass]
public class NotificacaoEventoServiceTests
{
    private SqliteConnection _connection = null!;
    private EventoContext _context = null!;
    private FakeEmailService _emailService = null!;
    private DateTime _agoraFixo;

    [TestInitialize]
    public void Initialize()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        SqliteSchemaEvento.Criar(_connection);

        var options = new DbContextOptionsBuilder<EventoContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new EventoContext(options);
        _emailService = new FakeEmailService();
        _agoraFixo = new DateTime(2026, 11, 20, 10, 0, 0);

        SeedBase();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private void SeedBase()
    {
        _context.Estadosbrasils.AddRange(
            new Estadosbrasil { Estado = "SE", Nome = "Sergipe" },
            new Estadosbrasil { Estado = "BA", Nome = "Bahia" },
            new Estadosbrasil { Estado = "SP", Nome = "São Paulo" });

        _context.Tipoeventos.Add(new Tipoevento { Id = 1, Nome = "Congresso" });

        _context.Areainteresses.AddRange(
            new Areainteresse { Id = 1, Nome = "Tecnologia" },
            new Areainteresse { Id = 2, Nome = "Música" },
            new Areainteresse { Id = 3, Nome = "Saúde" });

        _context.Pessoas.AddRange(
            new Pessoa { Id = 1, Nome = "Ana Todo Brasil", NomeCracha = "Ana", Cpf = "11111111111", Email = "ana@teste.com" },
            new Pessoa { Id = 2, Nome = "Bruno Sergipe", NomeCracha = "Bruno", Cpf = "22222222222", Email = "bruno@teste.com" },
            new Pessoa { Id = 3, Nome = "Carla Bahia", NomeCracha = "Carla", Cpf = "33333333333", Email = "carla@teste.com" },
            new Pessoa { Id = 4, Nome = "Diego Musica", NomeCracha = "Diego", Cpf = "44444444444", Email = "diego@teste.com" },
            new Pessoa { Id = 5, Nome = "Eva Sem Email", NomeCracha = "Eva", Cpf = "55555555555", Email = "   " });

        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private NotificacaoEventoService CriarService(string? caminhoTemplate = null)
    {
        return new NotificacaoEventoService(
            _context,
            _emailService,
            caminhoTemplateHtml: caminhoTemplate,
            agora: () => _agoraFixo);
    }

    private uint CriarEvento(
        string nome = "SEMINFO 2026",
        string status = "A",
        sbyte eventoPublico = 1,
        string? estado = "SE",
        string? cidade = "Itabaiana",
        string? rua = "Av. Vereador Olímpio Grande",
        string? numero = "s/n",
        string? bairro = "Porto",
        uint[]? idsAreas = null)
    {
        var evento = new Evento
        {
            Nome = nome,
            Descricao = "Semana de Tecnologia",
            Status = status,
            EventoPublico = eventoPublico,
            Estado = estado,
            Cidade = cidade,
            Rua = rua,
            Numero = numero,
            Bairro = bairro,
            IdTipoEvento = 1,
            DataInicio = new DateTime(2026, 12, 10, 9, 0, 0),
            DataFim = new DateTime(2026, 12, 12, 18, 0, 0),
            DataInicioInscricao = new DateTime(2026, 11, 20, 8, 0, 0),
            DataFimInscricao = new DateTime(2026, 12, 9, 23, 59, 0),
            VagasOfertadas = 200,
            VagasDisponiveis = 200
        };

        var areasIds = idsAreas ?? new uint[] { 1 };
        if (areasIds.Length > 0)
        {
            var areas = _context.Areainteresses.Where(a => areasIds.Contains(a.Id)).ToList();
            foreach (var area in areas)
            {
                evento.IdAreaInteresses.Add(area);
            }
        }

        _context.Eventos.Add(evento);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return evento.Id;
    }

    private void SalvarPreferencia(uint idPessoa, uint idArea, bool todosEstados, params string[] ufs)
    {
        var pref = new Pessoaareainteresse
        {
            IdPessoa = idPessoa,
            IdAreaInteresse = idArea,
            TodosEstados = todosEstados ? (sbyte)1 : (sbyte)0
        };

        if (!todosEstados && ufs.Length > 0)
        {
            var estados = _context.Estadosbrasils.Where(e => ufs.Contains(e.Estado)).ToList();
            foreach (var estado in estados)
            {
                pref.EstadoEstadosBrasils.Add(estado);
            }
        }

        _context.Pessoaareainteresses.Add(pref);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    [TestMethod]
    public async Task NotificarNovoEvento_CorrespondenciaAreasEEstados_EnviaApenasParaInteressados()
    {
        // Ana: Tecnologia + Todo o Brasil -> deve receber
        SalvarPreferencia(1, 1, todosEstados: true);
        // Bruno: Tecnologia + SE -> deve receber
        SalvarPreferencia(2, 1, todosEstados: false, "SE");
        // Carla: Tecnologia + BA -> NÃO deve receber (evento em SE)
        SalvarPreferencia(3, 1, todosEstados: false, "BA");
        // Diego: Música + Todo o Brasil -> NÃO deve receber (área diferente)
        SalvarPreferencia(4, 2, todosEstados: true);
        // Eva: Tecnologia + SE, mas sem e-mail válido -> NÃO deve receber
        SalvarPreferencia(5, 1, todosEstados: false, "SE");

        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });
        var service = CriarService();

        var resultado = await service.NotificarNovoEventoAsync(idEvento, $"https://borala.ufs.br/Inscricao/RealizarInscricao/{idEvento}");

        Assert.IsTrue(resultado.EventoElegivel);
        Assert.IsFalse(resultado.JaDivulgadoAnteriormente);
        Assert.AreEqual(2, resultado.TotalDestinatariosElegiveis);
        Assert.AreEqual(2, resultado.Enviados);
        Assert.AreEqual(0, resultado.Falhas);

        CollectionAssert.AreEquivalent(
            new[] { "ana@teste.com", "bruno@teste.com" },
            _emailService.Enviados.Select(e => e.Email).ToArray());

        var registros = _context.Notificacaoeventos.AsNoTracking().Where(n => n.IdEvento == idEvento).ToList();
        Assert.AreEqual(2, registros.Count);
        Assert.IsTrue(registros.All(r => r.Status == Notificacaoevento.StatusEnviado && r.Tentativas == 1 && r.DataEnvio.HasValue));
    }

    [TestMethod]
    public async Task NotificarNovoEvento_MultiplasAreasCoincidentes_EnviaApenasUmEmailPorParticipante()
    {
        // Ana acompanha Tecnologia (Todo o Brasil) e Música (SE)
        SalvarPreferencia(1, 1, todosEstados: true);
        SalvarPreferencia(1, 2, todosEstados: false, "SE");
        // Bruno acompanha Tecnologia (BA - não bate) e Música (SE - bate)
        SalvarPreferencia(2, 1, todosEstados: false, "BA");
        SalvarPreferencia(2, 2, todosEstados: false, "SE");

        var idEvento = CriarEvento(nome: "Festival Tech & Som", estado: "SE", idsAreas: new uint[] { 1, 2 });
        var service = CriarService();

        var resultado = await service.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/evento/1");

        Assert.AreEqual(2, resultado.TotalDestinatariosElegiveis);
        Assert.AreEqual(2, resultado.Enviados);
        Assert.AreEqual(2, _emailService.Enviados.Count);

        var emailAna = _emailService.Enviados.Single(e => e.Email == "ana@teste.com");
        StringAssert.Contains(emailAna.Html, "Música");
        StringAssert.Contains(emailAna.Html, "Tecnologia");

        var emailBruno = _emailService.Enviados.Single(e => e.Email == "bruno@teste.com");
        StringAssert.Contains(emailBruno.Html, "Música");
        Assert.IsFalse(emailBruno.Html.Contains("Tecnologia"), "Não deve mencionar área cuja UF não correspondeu.");
    }

    [TestMethod]
    public async Task NotificarNovoEvento_EmailContemNomeDataLocalELinkDoEvento()
    {
        SalvarPreferencia(1, 1, todosEstados: true);

        var idEvento = CriarEvento(
            nome: "Congresso <Inovação> & Dados",
            estado: "SE",
            cidade: "Itabaiana",
            rua: "Av. Vereador Olímpio Grande",
            numero: "100",
            bairro: "Porto",
            idsAreas: new uint[] { 1 });

        var caminhoTemplate = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "EventoWeb", "wwwroot", "templates", "EmailNovoEventoInteresse.html"));
        Assert.IsTrue(File.Exists(caminhoTemplate), $"Template não encontrado em {caminhoTemplate}");

        var link = $"https://borala.ufs.br/Inscricao/RealizarInscricao/{idEvento}";
        var service = CriarService(caminhoTemplate);

        var resultado = await service.NotificarNovoEventoAsync(idEvento, link);

        Assert.AreEqual(1, resultado.Enviados);
        var enviado = _emailService.Enviados.Single();
        StringAssert.Contains(enviado.Assunto, "Congresso <Inovação> & Dados");
        StringAssert.Contains(enviado.Html, "Congresso &lt;Inovação&gt; &amp; Dados");
        StringAssert.Contains(enviado.Html, "10/12/2026 09:00 até 12/12/2026 18:00");
        StringAssert.Contains(enviado.Html, "Av. Vereador Olímpio Grande, 100");
        StringAssert.Contains(enviado.Html, "Itabaiana - SE");
        StringAssert.Contains(enviado.Html, link);
    }

    [TestMethod]
    public async Task NotificarNovoEvento_SemLinkNemWebsite_UsaRotaRealizarInscricaoPorSegmento()
    {
        SalvarPreferencia(1, 1, todosEstados: true);
        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });

        var service = CriarService();
        var resultado = await service.NotificarNovoEventoAsync(idEvento, string.Empty);

        Assert.AreEqual(1, resultado.Enviados);
        var enviado = _emailService.Enviados.Single();
        StringAssert.Contains(enviado.Html, $"/Inscricao/RealizarInscricao/{idEvento}");
        Assert.IsFalse(enviado.Html.Contains("?idEvento="));
    }

    [TestMethod]
    public async Task NotificarNovoEvento_EventoNaoElegivel_NaoEnviaEmails()
    {
        SalvarPreferencia(1, 1, todosEstados: true);

        var idCadastro = CriarEvento(nome: "Rascunho", status: "C", eventoPublico: 1, idsAreas: new uint[] { 1 });
        var idInativo = CriarEvento(nome: "Inativo", status: "I", eventoPublico: 1, idsAreas: new uint[] { 1 });
        var idFinalizado = CriarEvento(nome: "Finalizado", status: "F", eventoPublico: 1, idsAreas: new uint[] { 1 });
        var idPrivado = CriarEvento(nome: "Privado", status: "A", eventoPublico: 0, idsAreas: new uint[] { 1 });
        var idSemAreas = CriarEvento(nome: "Sem Areas", status: "A", eventoPublico: 1, idsAreas: Array.Empty<uint>());

        var service = CriarService();

        foreach (var id in new[] { idCadastro, idInativo, idFinalizado, idPrivado, idSemAreas })
        {
            var res = await service.NotificarNovoEventoAsync(id, $"https://borala.ufs.br/e/{id}");
            Assert.IsFalse(res.EventoElegivel, $"Evento {id} não deveria ser elegível.");
            Assert.AreEqual(0, res.Enviados);
        }

        Assert.AreEqual(0, _emailService.Enviados.Count);
        Assert.AreEqual(0, _context.Notificacaoeventos.Count());
    }

    [TestMethod]
    public async Task NotificarNovoEvento_SegundaChamadaAposDivulgacao_NaoDisparaNovamente()
    {
        SalvarPreferencia(1, 1, todosEstados: true);
        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });
        var service = CriarService();

        var primeira = await service.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/e/1");
        Assert.AreEqual(1, primeira.Enviados);
        Assert.IsFalse(primeira.JaDivulgadoAnteriormente);

        // Novo participante cadastra interesse depois, e o evento é editado
        SalvarPreferencia(2, 1, todosEstados: true);

        var segunda = await service.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/e/1");
        Assert.IsTrue(segunda.JaDivulgadoAnteriormente);
        Assert.AreEqual(0, segunda.Enviados);
        Assert.AreEqual(1, segunda.IgnoradosJaEnviados);
        Assert.AreEqual(1, _emailService.Enviados.Count);
    }

    [TestMethod]
    public async Task NotificarNovoEvento_FalhaEmDestinatarios_RegistraFalhaENaoImpedeOsDemais()
    {
        SalvarPreferencia(1, 1, todosEstados: true);
        SalvarPreferencia(2, 1, todosEstados: false, "SE");
        SalvarPreferencia(3, 1, todosEstados: true);

        _emailService.ComportamentoPorEmail["ana@teste.com"] = () => Task.FromResult(false);
        _emailService.ComportamentoPorEmail["bruno@teste.com"] = () => throw new InvalidOperationException("SMTP timeout simulado");
        _emailService.ComportamentoPorEmail["carla@teste.com"] = () => Task.FromResult(true);

        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });
        var service = CriarService();

        var resultado = await service.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/e/1");

        Assert.AreEqual(3, resultado.TotalDestinatariosElegiveis);
        Assert.AreEqual(1, resultado.Enviados);
        Assert.AreEqual(2, resultado.Falhas);
        Assert.AreEqual(2, resultado.Erros.Count);

        var registros = _context.Notificacaoeventos.AsNoTracking()
            .Where(n => n.IdEvento == idEvento)
            .OrderBy(n => n.IdPessoa)
            .ToList();

        Assert.AreEqual(3, registros.Count);
        Assert.AreEqual(Notificacaoevento.StatusFalha, registros[0].Status);
        Assert.AreEqual(1, registros[0].Tentativas);
        Assert.IsFalse(string.IsNullOrWhiteSpace(registros[0].MensagemErro));
        Assert.IsNull(registros[0].DataEnvio);

        Assert.AreEqual(Notificacaoevento.StatusFalha, registros[1].Status);
        Assert.AreEqual(1, registros[1].Tentativas);
        StringAssert.Contains(registros[1].MensagemErro, "SMTP timeout simulado");

        Assert.AreEqual(Notificacaoevento.StatusEnviado, registros[2].Status);
        Assert.AreEqual(1, registros[2].Tentativas);
        Assert.IsNotNull(registros[2].DataEnvio);
        Assert.IsNull(registros[2].MensagemErro);

        var resumo = service.ObterResumoPorEvento(idEvento);
        Assert.AreEqual(1, resumo.Enviados);
        Assert.AreEqual(2, resumo.Falhas);
        Assert.AreEqual(0, resumo.Pendentes);
    }

    [TestMethod]
    public async Task ReenviarFalhas_RetentaSomenteFalhasSemDuplicarEnviosJaConcluidos()
    {
        SalvarPreferencia(1, 1, todosEstados: true);
        SalvarPreferencia(2, 1, todosEstados: false, "SE");

        // 1ª tentativa: Ana falha, Bruno recebe
        _emailService.ComportamentoPorEmail["ana@teste.com"] = () => throw new InvalidOperationException("Falha temporária");
        _emailService.ComportamentoPorEmail["bruno@teste.com"] = () => Task.FromResult(true);

        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });
        var service = CriarService();

        var inicial = await service.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/e/1");
        Assert.AreEqual(1, inicial.Enviados);
        Assert.AreEqual(1, inicial.Falhas);
        Assert.AreEqual(1, _emailService.Enviados.Count(e => e.Email == "bruno@teste.com"));

        // SMTP volta ao normal e executa estratégia de repetição
        _emailService.ComportamentoPorEmail["ana@teste.com"] = () => Task.FromResult(true);
        _agoraFixo = _agoraFixo.AddMinutes(10);

        var reenvio = await service.ReenviarFalhasAsync(idEvento, "https://borala.ufs.br/e/1");

        Assert.AreEqual(1, reenvio.Enviados);
        Assert.AreEqual(0, reenvio.Falhas);
        Assert.AreEqual(1, reenvio.IgnoradosJaEnviados);

        // Bruno recebeu apenas 1 vez no total; Ana recebeu no reenvio
        Assert.AreEqual(1, _emailService.Enviados.Count(e => e.Email == "bruno@teste.com"));
        Assert.AreEqual(1, _emailService.Enviados.Count(e => e.Email == "ana@teste.com"));

        var regAna = _context.Notificacaoeventos.AsNoTracking().Single(n => n.IdEvento == idEvento && n.IdPessoa == 1);
        Assert.AreEqual(Notificacaoevento.StatusEnviado, regAna.Status);
        Assert.AreEqual(2, regAna.Tentativas);
        Assert.IsNull(regAna.MensagemErro);
        Assert.IsNotNull(regAna.DataEnvio);
    }

    [TestMethod]
    public async Task ReenviarFalhas_UtilizaPreferenciasVigentesECancelaQuemDeixouDeAcompanhar()
    {
        SalvarPreferencia(1, 1, todosEstados: false, "SE");
        SalvarPreferencia(2, 1, todosEstados: true);

        _emailService.ComportamentoPorEmail["ana@teste.com"] = () => Task.FromResult(false);
        _emailService.ComportamentoPorEmail["bruno@teste.com"] = () => Task.FromResult(false);

        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });
        var service = CriarService();

        var inicial = await service.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/e/1");
        Assert.AreEqual(2, inicial.Falhas);

        // Ana remove sua preferência antes do reenvio; Bruno atualiza seu e-mail
        var prefAna = _context.Pessoaareainteresses.Include(p => p.EstadoEstadosBrasils).Single(p => p.IdPessoa == 1 && p.IdAreaInteresse == 1);
        prefAna.EstadoEstadosBrasils.Clear();
        _context.Pessoaareainteresses.Remove(prefAna);

        var bruno = _context.Pessoas.Single(p => p.Id == 2);
        bruno.Email = "bruno.novo@teste.com";
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        _emailService.ComportamentoPorEmail.Clear();

        var reenvio = await service.ReenviarFalhasAsync(idEvento, "https://borala.ufs.br/e/1");

        Assert.AreEqual(1, reenvio.Enviados);
        Assert.AreEqual(0, reenvio.Falhas);
        Assert.AreEqual(1, reenvio.CanceladosPorPreferenciaAlterada);
        Assert.AreEqual(1, _emailService.Enviados.Count);
        Assert.AreEqual("bruno.novo@teste.com", _emailService.Enviados[0].Email);

        var regAna = _context.Notificacaoeventos.AsNoTracking().Single(n => n.IdEvento == idEvento && n.IdPessoa == 1);
        Assert.AreEqual(Notificacaoevento.StatusCancelado, regAna.Status);

        var regBruno = _context.Notificacaoeventos.AsNoTracking().Single(n => n.IdEvento == idEvento && n.IdPessoa == 2);
        Assert.AreEqual(Notificacaoevento.StatusEnviado, regBruno.Status);
        Assert.AreEqual("bruno.novo@teste.com", regBruno.EmailDestino);
    }

    [TestMethod]
    public async Task ReenviarTodasFalhas_ProcessaApenasEventosComFalha()
    {
        SalvarPreferencia(1, 1, todosEstados: true);

        var idEvento1 = CriarEvento(nome: "Evento Falha 1", estado: "SE", idsAreas: new uint[] { 1 });
        var idEvento2 = CriarEvento(nome: "Evento Sucesso", estado: "SE", idsAreas: new uint[] { 1 });

        var service = CriarService();

        _emailService.ComportamentoPorEmail["ana@teste.com"] = () => Task.FromResult(false);
        await service.NotificarNovoEventoAsync(idEvento1, "https://borala.ufs.br/e/1");

        _emailService.ComportamentoPorEmail["ana@teste.com"] = () => Task.FromResult(true);
        await service.NotificarNovoEventoAsync(idEvento2, "https://borala.ufs.br/e/2");

        Assert.AreEqual(1, _emailService.Enviados.Count);

        var resultados = await service.ReenviarTodasFalhasAsync(id => $"https://borala.ufs.br/e/{id}");

        Assert.AreEqual(1, resultados.Count);
        Assert.AreEqual(idEvento1, resultados[0].IdEvento);
        Assert.AreEqual(1, resultados[0].Enviados);
        Assert.AreEqual(2, _emailService.Enviados.Count);
    }

    [TestMethod]
    public async Task EventoServiceDelete_ComNotificacoesRegistradas_RemoveSemErroDeFk()
    {
        SalvarPreferencia(1, 1, todosEstados: true);
        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });

        var notificacaoService = CriarService();
        var res = await notificacaoService.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/e/1");
        Assert.AreEqual(1, res.Enviados);

        var eventoService = new EventoService(_context);
        var evento = eventoService.Get(idEvento);
        evento.Status = "I";
        eventoService.Edit(evento, new List<uint> { 1 });
        eventoService.Delete(idEvento);

        _context.ChangeTracker.Clear();
        Assert.AreEqual(0, _context.Eventos.Count(e => e.Id == idEvento));
        Assert.AreEqual(0, _context.Notificacaoeventos.Count(n => n.IdEvento == idEvento));
    }

    [TestMethod]
    public async Task NotificarNovoEventoAsync_FalhaDePersistenciaEmUmDestinatario_DesanexaEntradaEPersisteDemais()
    {
        SalvarPreferencia(1, 1, todosEstados: true);
        SalvarPreferencia(2, 1, todosEstados: false, "SE");
        var idEvento = CriarEvento(estado: "SE", idsAreas: new uint[] { 1 });

        _emailService.ComportamentoPorEmail["ana@teste.com"] = () =>
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO notificacaoevento (idEvento, idPessoa, emailDestino, status, tentativas, dataCriacao)
                VALUES ($idEvento, 1, 'ana@teste.com', 'P', 0, '2026-11-20 10:00:00');";
            cmd.Parameters.AddWithValue("$idEvento", idEvento);
            cmd.ExecuteNonQuery();
            return Task.FromResult(true);
        };

        var service = CriarService();
        var resultado = await service.NotificarNovoEventoAsync(idEvento, "https://borala.ufs.br/e/1");

        Assert.AreEqual(2, resultado.TotalDestinatariosElegiveis);
        Assert.IsTrue(resultado.Erros.Any(e => e.Contains("Pessoa 1")));

        _context.ChangeTracker.Clear();
        var registroBruno = _context.Notificacaoeventos.SingleOrDefault(n => n.IdEvento == idEvento && n.IdPessoa == 2);
        Assert.IsNotNull(registroBruno);
        Assert.AreEqual(Notificacaoevento.StatusEnviado, registroBruno.Status);
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<(string Email, string Assunto, string Html)> Enviados { get; } = new();
        public Dictionary<string, Func<Task<bool>>> ComportamentoPorEmail { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task EnviarEmailAsync(string email, string assunto, string mensagemHtml)
        {
            Enviados.Add((email, assunto, mensagemHtml));
            return Task.CompletedTask;
        }

        public Task<bool> ModeloEmailReset(string token, Pessoa pessoa, string callbackUrl) => Task.FromResult(true);

        public Task<bool> ModeloConfirmEmail(string token, Pessoa pessoa, string callbackUrl) => Task.FromResult(true);

        public Task<bool> ModeloEmail(string token, Pessoa pessoa, string callbackUrl, string email, string contato, string assunto, string caminhoTemplate)
            => Task.FromResult(true);

        public async Task<bool> TentarEnviarEmailAsync(string email, string assunto, string mensagemHtml)
        {
            if (ComportamentoPorEmail.TryGetValue(email, out var acao))
            {
                var ok = await acao();
                if (ok)
                {
                    Enviados.Add((email, assunto, mensagemHtml));
                }
                return ok;
            }

            Enviados.Add((email, assunto, mensagemHtml));
            return true;
        }
    }
}
