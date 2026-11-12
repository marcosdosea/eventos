using Core;
using Core.DTO;
using Core.Service;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Tests;

[TestClass]
public class SincronizacaoFrequenciaServiceTests
{
    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3);

    private SqliteConnection _connection = null!;
    private EventoContext _context = null!;
    private IEventoService _eventoService = null!;
    private ISubeventoService _subeventoService = null!;
    private IDistributedCache _cache = null!;
    private ISincronizacaoFrequenciaService _service = null!;

    [TestInitialize]
    public void Preparar()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteSchemaEvento.Criar(_connection);

        var options = new DbContextOptionsBuilder<EventoContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new EventoContext(options);
        _eventoService = new EventoService(_context);
        _subeventoService = new SubeventoService(_context);
        _cache = CriarCacheEmMemoria();
        _service = new SincronizacaoFrequenciaService(_context, _eventoService, _subeventoService, _cache);

        SemearBase();
    }

    [TestCleanup]
    public void Limpar()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [TestMethod]
    public async Task SincronizarLoteAsync_EntradaESaidaEvento_RegistraParticipacaoECalculaFrequenciaFinal()
    {
        var lote1 = new[]
        {
            new BipouLeituraFrequenciaDTO("l-1", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("l-2", 3, 1, null, "SAIDA", DataHora(2026, 11, 6, 10, 0), "dev-1")
        };

        var res1 = await _service.SincronizarLoteAsync(lote1, new uint[] { 1 });

        CollectionAssert.AreEqual(new[] { "l-1", "l-2" }, res1.Confirmados.ToArray());
        Assert.AreEqual(0, res1.Rejeitados.Count);

        var participacoes = await _context.Participacaopessoaeventos.AsNoTracking()
            .Where(p => p.IdEvento == 1 && p.IdPessoa == 3)
            .ToListAsync();
        Assert.AreEqual(1, participacoes.Count);
        Assert.AreEqual(new DateTime(2026, 11, 6, 8, 0, 0), participacoes[0].Entrada);
        Assert.AreEqual(new DateTime(2026, 11, 6, 10, 0, 0), participacoes[0].Saida);

        var inscricao = await _context.Inscricaopessoaeventos.AsNoTracking()
            .SingleAsync(i => i.IdEvento == 1 && i.IdPessoa == 3);
        Assert.AreEqual(50m, inscricao.FrequenciaFinal);

        // Segundo período: 10:30 às 12:30 (clampado para 12:00 = +1,5h -> total 3,5h de 4h = 87,5%)
        var lote2 = new[]
        {
            new BipouLeituraFrequenciaDTO("l-3", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 10, 30), "dev-1"),
            new BipouLeituraFrequenciaDTO("l-4", 3, 1, null, "SAIDA", DataHora(2026, 11, 6, 12, 30), "dev-1")
        };

        var res2 = await _service.SincronizarLoteAsync(lote2, new uint[] { 1 });
        Assert.AreEqual(2, res2.Confirmados.Count);
        Assert.AreEqual(2, await _context.Participacaopessoaeventos.CountAsync(p => p.IdEvento == 1 && p.IdPessoa == 3));

        var inscricaoAtualizada = await _context.Inscricaopessoaeventos.AsNoTracking()
            .SingleAsync(i => i.IdEvento == 1 && i.IdPessoa == 3);
        Assert.AreEqual(87.5m, inscricaoAtualizada.FrequenciaFinal);
    }

    [TestMethod]
    public async Task SincronizarLoteAsync_EntradaESaidaSubevento_AtualizaFrequenciaSubeventoSemAfetarEvento()
    {
        var lote = new[]
        {
            new BipouLeituraFrequenciaDTO("sub-1", 3, 1, 1, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("sub-2", 3, 1, 1, "SAIDA", DataHora(2026, 11, 6, 10, 0), "dev-1")
        };

        var resultado = await _service.SincronizarLoteAsync(lote, new uint[] { 1 });

        CollectionAssert.AreEqual(new[] { "sub-1", "sub-2" }, resultado.Confirmados.ToArray());
        Assert.AreEqual(0, resultado.Rejeitados.Count);

        var participacaoSub = await _context.Participacaopessoasubeventos.AsNoTracking()
            .SingleAsync(p => p.IdSubEvento == 1 && p.IdPessoa == 3);
        Assert.AreEqual(new DateTime(2026, 11, 6, 8, 0, 0), participacaoSub.Entrada);
        Assert.AreEqual(new DateTime(2026, 11, 6, 10, 0, 0), participacaoSub.Saida);

        var inscricaoSub = await _context.Inscricaopessoasubeventos.AsNoTracking()
            .SingleAsync(i => i.IdSubEvento == 1 && i.IdPessoa == 3);
        Assert.AreEqual(100m, inscricaoSub.FrequenciaFinal);

        var inscricaoEvento = await _context.Inscricaopessoaeventos.AsNoTracking()
            .SingleAsync(i => i.IdEvento == 1 && i.IdPessoa == 3);
        Assert.AreEqual(0m, inscricaoEvento.FrequenciaFinal);
    }

    [TestMethod]
    public async Task SincronizarLoteAsync_ReenvioComMesmoLeituraIdOuAposReinicioCache_GaranteIdempotenciaEstrita()
    {
        var lote = new[]
        {
            new BipouLeituraFrequenciaDTO("uuid-1", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("uuid-2", 3, 1, null, "SAIDA", DataHora(2026, 11, 6, 10, 0), "dev-1")
        };

        var primeiro = await _service.SincronizarLoteAsync(lote, new uint[] { 1 });
        Assert.AreEqual(2, primeiro.Confirmados.Count);

        // Reenvio imediato (atinge o cache distribuído)
        var segundo = await _service.SincronizarLoteAsync(lote, new uint[] { 1 });
        CollectionAssert.AreEqual(new[] { "uuid-1", "uuid-2" }, segundo.Confirmados.ToArray());
        Assert.AreEqual(0, segundo.Rejeitados.Count);
        Assert.AreEqual(1, await _context.Participacaopessoaeventos.CountAsync(p => p.IdEvento == 1 && p.IdPessoa == 3));

        // Simula reinício do servidor com cache limpo (garantido pela chave natural no banco)
        var servicoPosReinicio = new SincronizacaoFrequenciaService(
            _context, _eventoService, _subeventoService, CriarCacheEmMemoria());

        var terceiro = await servicoPosReinicio.SincronizarLoteAsync(lote, new uint[] { 1 });
        CollectionAssert.AreEqual(new[] { "uuid-1", "uuid-2" }, terceiro.Confirmados.ToArray());
        Assert.AreEqual(0, terceiro.Rejeitados.Count);
        Assert.AreEqual(1, await _context.Participacaopessoaeventos.CountAsync(p => p.IdEvento == 1 && p.IdPessoa == 3));
    }

    [TestMethod]
    public async Task SincronizarLoteAsync_LeituraIdDuplicadoNoMesmoLote_ProcessaApenasUmaVez()
    {
        var lote = new[]
        {
            new BipouLeituraFrequenciaDTO("dup-1", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("dup-1", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1")
        };

        var resultado = await _service.SincronizarLoteAsync(lote, new uint[] { 1 });

        CollectionAssert.AreEqual(new[] { "dup-1" }, resultado.Confirmados.ToArray());
        Assert.AreEqual(0, resultado.Rejeitados.Count);
        Assert.AreEqual(1, await _context.Participacaopessoaeventos.CountAsync(p => p.IdEvento == 1 && p.IdPessoa == 3));
    }

    [TestMethod]
    public async Task SincronizarLoteAsync_SaidaAntesDeEntradaNoMesmoLote_OrdenaCronologicamenteEProcessa()
    {
        var loteForaDeOrdem = new[]
        {
            new BipouLeituraFrequenciaDTO("ord-2", 3, 1, null, "SAIDA", DataHora(2026, 11, 6, 12, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("ord-1", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1")
        };

        var resultado = await _service.SincronizarLoteAsync(loteForaDeOrdem, new uint[] { 1 });

        Assert.AreEqual(2, resultado.Confirmados.Count);
        Assert.AreEqual(0, resultado.Rejeitados.Count);

        var inscricao = await _context.Inscricaopessoaeventos.AsNoTracking()
            .SingleAsync(i => i.IdEvento == 1 && i.IdPessoa == 3);
        Assert.AreEqual(100m, inscricao.FrequenciaFinal);
    }

    [TestMethod]
    public async Task SincronizarLoteAsync_ItensInvalidosESemEntradaAberta_RejeitaIsoladamenteSemAbortarValidos()
    {
        var loteMisto = new[]
        {
            new BipouLeituraFrequenciaDTO("ok-1", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("err-saida", 4, 1, null, "SAIDA", DataHora(2026, 11, 6, 9, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("err-sub", 3, 1, 2, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("err-cancelado", 5, 1, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1"),
            new BipouLeituraFrequenciaDTO("err-perm", 3, 2, null, "ENTRADA", DataHora(2026, 11, 6, 8, 0), "dev-1")
        };

        var resultado = await _service.SincronizarLoteAsync(loteMisto, new uint[] { 1 });

        CollectionAssert.AreEqual(new[] { "ok-1" }, resultado.Confirmados.ToArray());
        Assert.AreEqual(4, resultado.Rejeitados.Count);
        Assert.AreEqual("ENTRADA_ABERTA_NAO_ENCONTRADA", resultado.Rejeitados.Single(r => r.LeituraId == "err-saida").Codigo);
        Assert.AreEqual("SUBEVENTO_INVALIDO", resultado.Rejeitados.Single(r => r.LeituraId == "err-sub").Codigo);
        Assert.AreEqual("PARTICIPANTE_NAO_INSCRITO", resultado.Rejeitados.Single(r => r.LeituraId == "err-cancelado").Codigo);
        Assert.AreEqual("SEM_PERMISSAO_EVENTO", resultado.Rejeitados.Single(r => r.LeituraId == "err-perm").Codigo);

        Assert.AreEqual(1, await _context.Participacaopessoaeventos.CountAsync());
    }

    [TestMethod]
    public async Task SincronizarLoteAsync_SaidaAnteriorAEntradaAberta_RejeitaComCodigoEspecifico()
    {
        await _service.SincronizarLoteAsync(
            new[] { new BipouLeituraFrequenciaDTO("e-1", 3, 1, null, "ENTRADA", DataHora(2026, 11, 6, 10, 0), "dev-1") },
            new uint[] { 1 });

        var resultado = await _service.SincronizarLoteAsync(
            new[] { new BipouLeituraFrequenciaDTO("s-ant", 3, 1, null, "SAIDA", DataHora(2026, 11, 6, 9, 0), "dev-1") },
            new uint[] { 1 });

        Assert.AreEqual(0, resultado.Confirmados.Count);
        Assert.AreEqual(1, resultado.Rejeitados.Count);
        Assert.AreEqual("SAIDA_ANTERIOR_A_ENTRADA", resultado.Rejeitados[0].Codigo);

        var participacao = await _context.Participacaopessoaeventos.AsNoTracking().SingleAsync();
        Assert.IsNull(participacao.Saida);
    }

    private void SemearBase()
    {
        _context.Papels.AddRange(
            new Papel { Id = 2, Nome = "Gestor" },
            new Papel { Id = 3, Nome = "Colaborador" },
            new Papel { Id = 4, Nome = "Participante" });

        _context.Tipoeventos.Add(new Tipoevento { Id = 1, Nome = "Curso" });

        _context.Pessoas.AddRange(
            new Pessoa { Id = 3, Cpf = "52998224725", Nome = "Participante Ativo", Email = "p3@example.test" },
            new Pessoa { Id = 4, Cpf = "12345678909", Nome = "Participante Gratuito", Email = "p4@example.test" },
            new Pessoa { Id = 5, Cpf = "99988877766", Nome = "Participante Cancelado", Email = "p5@example.test" });

        _context.Eventos.AddRange(
            new Evento
            {
                Id = 1,
                Nome = "Evento Principal",
                Status = "A",
                DataInicio = new DateTime(2026, 11, 6, 8, 0, 0),
                DataFim = new DateTime(2026, 11, 6, 12, 0, 0),
                CargaHoraria = 4
            },
            new Evento
            {
                Id = 2,
                Nome = "Outro Evento",
                Status = "A",
                DataInicio = new DateTime(2026, 11, 6, 8, 0, 0),
                DataFim = new DateTime(2026, 11, 6, 12, 0, 0),
                CargaHoraria = 4
            });

        _context.Subeventos.AddRange(
            new Subevento
            {
                Id = 1,
                IdEvento = 1,
                Nome = "Minicurso A",
                Descricao = "Teste",
                Status = "A",
                IdTipoEvento = 1,
                CargaHoraria = 2,
                DataInicio = new DateTime(2026, 11, 6, 8, 0, 0),
                DataFim = new DateTime(2026, 11, 6, 10, 0, 0),
                DataInicioInscricao = new DateTime(2026, 11, 1),
                DataFimInscricao = new DateTime(2026, 11, 5)
            },
            new Subevento
            {
                Id = 2,
                IdEvento = 2,
                Nome = "Minicurso Outro Evento",
                Descricao = "Teste",
                Status = "A",
                IdTipoEvento = 1,
                CargaHoraria = 2,
                DataInicio = new DateTime(2026, 11, 6, 8, 0, 0),
                DataFim = new DateTime(2026, 11, 6, 10, 0, 0),
                DataInicioInscricao = new DateTime(2026, 11, 1),
                DataFimInscricao = new DateTime(2026, 11, 5)
            });

        _context.Inscricaopessoaeventos.AddRange(
            new Inscricaopessoaevento { IdPessoa = 3, IdEvento = 1, IdPapel = 4, Status = "A", ValorTotal = 0, DataInscricao = new DateTime(2026, 11, 1) },
            new Inscricaopessoaevento { IdPessoa = 4, IdEvento = 1, IdPapel = 4, Status = "S", ValorTotal = 0, DataInscricao = new DateTime(2026, 11, 1) },
            new Inscricaopessoaevento { IdPessoa = 5, IdEvento = 1, IdPapel = 4, Status = "C", ValorTotal = 0, DataInscricao = new DateTime(2026, 11, 1) });

        _context.Inscricaopessoasubeventos.Add(
            new Inscricaopessoasubevento { IdPessoa = 3, IdSubEvento = 1, IdPapel = 4, Status = "A", Valor = 0, DataInscricao = new DateTime(2026, 11, 1) });

        _context.SaveChanges();
    }

    private static DateTimeOffset DataHora(int ano, int mes, int dia, int hora, int minuto)
        => new(ano, mes, dia, hora, minuto, 0, OffsetBrasilia);

    private static IDistributedCache CriarCacheEmMemoria()
        => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

