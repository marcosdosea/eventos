using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;

namespace Service;

/// <summary>
/// Serviço responsável pela ingestão idempotente de leituras de frequência
/// em lote do aplicativo Bipou e pelo recálculo da frequência final.
/// </summary>
public class SincronizacaoFrequenciaService : ISincronizacaoFrequenciaService
{
    private const int PapelParticipante = 4;
    private const string PrefixoCacheLeitura = "bipou:leitura:";
    private static readonly TimeSpan DuracaoCacheLeitura = TimeSpan.FromDays(7);
    private static readonly byte[] MarcadorCache = [1];

    private readonly EventoContext _context;
    private readonly IEventoService _eventoService;
    private readonly ISubeventoService _subeventoService;
    private readonly IDistributedCache _cache;

    public SincronizacaoFrequenciaService(
        EventoContext context,
        IEventoService eventoService,
        ISubeventoService subeventoService,
        IDistributedCache cache)
    {
        _context = context;
        _eventoService = eventoService;
        _subeventoService = subeventoService;
        _cache = cache;
    }

    /// <summary>
    /// Processa um lote de leituras de entrada/saída, garantindo idempotência por leituraId
    /// (via cache distribuído + chave natural) e atualizando a frequência final das inscrições afetadas.
    /// </summary>
    public async Task<BipouLoteFrequenciaResultadoDTO> SincronizarLoteAsync(
        IReadOnlyCollection<BipouLeituraFrequenciaDTO> leituras,
        IReadOnlyCollection<uint> eventosPermitidosIds,
        CancellationToken cancellationToken = default)
    {
        if (leituras == null || leituras.Count == 0)
            return new BipouLoteFrequenciaResultadoDTO([], []);

        var eventosPermitidos = eventosPermitidosIds?.ToHashSet() ?? [];
        var confirmados = new List<string>();
        var confirmadosSet = new HashSet<string>(StringComparer.Ordinal);
        var rejeitados = new List<BipouLeituraErroDTO>();
        var vistosNoLote = new HashSet<string>(StringComparer.Ordinal);
        var candidatos = new List<LeituraCandidata>();

        var indice = 0;
        foreach (var leitura in leituras)
        {
            indice++;
            if (leitura == null || string.IsNullOrWhiteSpace(leitura.LeituraId))
            {
                rejeitados.Add(new BipouLeituraErroDTO(
                    string.Empty,
                    "LEITURA_ID_INVALIDO",
                    "O identificador leituraId é obrigatório."));
                continue;
            }

            var leituraId = leitura.LeituraId.Trim();
            if (!vistosNoLote.Add(leituraId))
                continue;

            if (await JaProcessadaNoCacheAsync(leituraId, cancellationToken))
            {
                AdicionarConfirmado(confirmados, confirmadosSet, leituraId);
                continue;
            }

            var erroValidacao = ValidarLeitura(leitura, leituraId, eventosPermitidos, out var tipoAcao);
            if (erroValidacao != null)
            {
                rejeitados.Add(erroValidacao);
                continue;
            }

            uint? idSubEvento = leitura.IdSubEvento is > 0 ? leitura.IdSubEvento : null;
            candidatos.Add(new LeituraCandidata(
                IndiceOriginal: indice,
                LeituraId: leituraId,
                IdPessoa: leitura.IdPessoa,
                IdEvento: leitura.IdEvento,
                IdSubEvento: idSubEvento,
                TipoAcao: tipoAcao,
                DataHora: NormalizarDataHora(leitura.DataHoraLida)));
        }

        if (candidatos.Count == 0)
            return new BipouLoteFrequenciaResultadoDTO(confirmados, rejeitados);

        var ordenadas = candidatos
            .OrderBy(c => c.DataHora)
            .ThenBy(c => c.IndiceOriginal)
            .ToList();

        var novasParaCache = new List<string>();
        await using IDbContextTransaction? transacao = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await ProcessarCandidatosAsync(ordenadas, confirmados, confirmadosSet, rejeitados, novasParaCache, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        if (transacao != null)
            await transacao.CommitAsync(cancellationToken);

        await RegistrarNoCacheAsync(novasParaCache, cancellationToken);

        return new BipouLoteFrequenciaResultadoDTO(confirmados, rejeitados);
    }

    private async Task ProcessarCandidatosAsync(
        List<LeituraCandidata> ordenadas,
        List<string> confirmados,
        HashSet<string> confirmadosSet,
        List<BipouLeituraErroDTO> rejeitados,
        List<string> novasParaCache,
        CancellationToken cancellationToken)
    {
        var idsEventos = ordenadas.Select(c => c.IdEvento).Distinct().ToList();
        var idsPessoas = ordenadas.Select(c => c.IdPessoa).Distinct().ToList();
        var idsSubeventos = ordenadas
            .Where(c => c.IdSubEvento.HasValue)
            .Select(c => c.IdSubEvento!.Value)
            .Distinct()
            .ToList();

        var subeventosPorId = idsSubeventos.Count == 0
            ? new Dictionary<uint, Subevento>()
            : await _context.Subeventos.AsNoTracking()
                .Where(s => idsSubeventos.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        var inscricoesEvento = await _context.Inscricaopessoaeventos.AsNoTracking()
            .Where(i => idsEventos.Contains(i.IdEvento) && idsPessoas.Contains(i.IdPessoa) && i.IdPapel == PapelParticipante && i.Status != "C")
            .Select(i => new { i.IdEvento, i.IdPessoa, i.Status, i.ValorTotal })
            .ToListAsync(cancellationToken);

        var subinscricoesPessoaEvento = await _context.Inscricaopessoasubeventos.AsNoTracking()
            .Where(s => idsEventos.Contains(s.IdSubEventoNavigation.IdEvento) && idsPessoas.Contains(s.IdPessoa) && s.Status != "C")
            .Select(s => new { s.IdSubEventoNavigation.IdEvento, s.IdPessoa, s.Valor })
            .ToListAsync(cancellationToken);

        var somaSubPorEventoPessoa = subinscricoesPessoaEvento
            .GroupBy(s => (s.IdEvento, s.IdPessoa))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Valor));

        var elegiveisEvento = inscricoesEvento
            .Where(i => i.Status == "A" ||
                (i.Status == "S" && i.ValorTotal + somaSubPorEventoPessoa.GetValueOrDefault((i.IdEvento, i.IdPessoa), 0m) <= 0))
            .Select(i => (i.IdEvento, i.IdPessoa))
            .ToHashSet();

        var elegiveisSubevento = idsSubeventos.Count == 0
            ? new HashSet<(uint IdSubEvento, uint IdPessoa)>()
            : (await _context.Inscricaopessoasubeventos.AsNoTracking()
                .Where(s => idsSubeventos.Contains(s.IdSubEvento) && idsPessoas.Contains(s.IdPessoa) && s.IdPapel == PapelParticipante && s.Status != "C")
                .Select(s => new { s.IdSubEvento, s.IdPessoa, s.Status, s.Valor })
                .ToListAsync(cancellationToken))
                .Where(s => s.Status == "A" || (s.Status == "S" && s.Valor <= 0))
                .Select(s => (s.IdSubEvento, s.IdPessoa))
                .ToHashSet();

        var participacoesEvento = ordenadas.Any(c => !c.IdSubEvento.HasValue)
            ? await _context.Participacaopessoaeventos
                .Where(p => idsEventos.Contains(p.IdEvento) && idsPessoas.Contains(p.IdPessoa))
                .ToListAsync(cancellationToken)
            : [];

        var participacoesSubevento = idsSubeventos.Count == 0
            ? []
            : await _context.Participacaopessoasubeventos
                .Where(p => idsSubeventos.Contains(p.IdSubEvento) && idsPessoas.Contains(p.IdPessoa))
                .ToListAsync(cancellationToken);

        var eventosAfetados = new HashSet<(uint IdEvento, uint IdPessoa)>();
        var subeventosAfetados = new HashSet<(uint IdSubEvento, uint IdPessoa)>();

        foreach (var item in ordenadas)
        {
            if (item.IdSubEvento.HasValue)
            {
                var idSub = item.IdSubEvento.Value;
                if (!subeventosPorId.TryGetValue(idSub, out var sub) || sub.IdEvento != item.IdEvento || sub.Status != "A")
                {
                    rejeitados.Add(new BipouLeituraErroDTO(
                        item.LeituraId,
                        "SUBEVENTO_INVALIDO",
                        "Subevento não encontrado, inativo ou não pertence ao evento informado."));
                    continue;
                }

                if (!elegiveisSubevento.Contains((idSub, item.IdPessoa)))
                {
                    rejeitados.Add(new BipouLeituraErroDTO(
                        item.LeituraId,
                        "PARTICIPANTE_NAO_INSCRITO_SUBEVENTO",
                        "Participante não possui inscrição elegível neste subevento."));
                    continue;
                }

                var doPar = participacoesSubevento.Where(p => p.IdSubEvento == idSub && p.IdPessoa == item.IdPessoa);
                var resultado = RegistrarParticipacao(
                    item,
                    doPar,
                    p => p.Id,
                    p => p.Entrada,
                    p => p.Saida,
                    (p, saida) => p.Saida = saida,
                    entrada =>
                    {
                        var nova = new Participacaopessoasubevento
                        {
                            IdPessoa = item.IdPessoa,
                            IdSubEvento = idSub,
                            Entrada = entrada
                        };
                        _context.Participacaopessoasubeventos.Add(nova);
                        participacoesSubevento.Add(nova);
                    });

                TratarResultadoRegistro(resultado, item.LeituraId, confirmados, confirmadosSet, rejeitados, novasParaCache,
                    () => subeventosAfetados.Add((idSub, item.IdPessoa)));
            }
            else
            {
                if (!elegiveisEvento.Contains((item.IdEvento, item.IdPessoa)))
                {
                    rejeitados.Add(new BipouLeituraErroDTO(
                        item.LeituraId,
                        "PARTICIPANTE_NAO_INSCRITO",
                        "Participante não possui inscrição elegível neste evento."));
                    continue;
                }

                var doPar = participacoesEvento.Where(p => p.IdEvento == item.IdEvento && p.IdPessoa == item.IdPessoa);
                var resultado = RegistrarParticipacao(
                    item,
                    doPar,
                    p => p.Id,
                    p => p.Entrada,
                    p => p.Saida,
                    (p, saida) => p.Saida = saida,
                    entrada =>
                    {
                        var nova = new Participacaopessoaevento
                        {
                            IdPessoa = item.IdPessoa,
                            IdEvento = item.IdEvento,
                            Entrada = entrada
                        };
                        _context.Participacaopessoaeventos.Add(nova);
                        participacoesEvento.Add(nova);
                    });

                TratarResultadoRegistro(resultado, item.LeituraId, confirmados, confirmadosSet, rejeitados, novasParaCache,
                    () => eventosAfetados.Add((item.IdEvento, item.IdPessoa)));
            }
        }

        foreach (var (idEvento, idPessoa) in eventosAfetados)
        {
            _eventoService.AtualizarFrequenciaFinal(idEvento, idPessoa, saveChanges: false);
        }

        foreach (var (idSubEvento, idPessoa) in subeventosAfetados)
        {
            _subeventoService.AtualizarFrequenciaFinal(idSubEvento, idPessoa, saveChanges: false);
        }
    }

    private static ResultadoRegistroParticipacao RegistrarParticipacao<T>(
        LeituraCandidata item,
        IEnumerable<T> registrosExistentes,
        Func<T, uint> getId,
        Func<T, DateTime> getEntrada,
        Func<T, DateTime?> getSaida,
        Action<T, DateTime> setSaida,
        Action<DateTime> adicionarNovaEntrada)
    {
        var lista = registrosExistentes.ToList();

        if (item.TipoAcao == TipoAcaoEntrada)
        {
            if (lista.Any(p => getEntrada(p) == item.DataHora))
                return ResultadoRegistroParticipacao.Idempotente;

            adicionarNovaEntrada(item.DataHora);
            return ResultadoRegistroParticipacao.Alterado;
        }

        var abertas = lista
            .Where(p => !getSaida(p).HasValue)
            .OrderByDescending(getEntrada)
            .ThenByDescending(getId)
            .ToList();

        var abertaValida = abertas.FirstOrDefault(p => getEntrada(p) <= item.DataHora);
        if (abertaValida != null)
        {
            setSaida(abertaValida, item.DataHora);
            return ResultadoRegistroParticipacao.Alterado;
        }

        if (abertas.Count > 0)
            return ResultadoRegistroParticipacao.SaidaAnteriorAEntrada;

        if (lista.Any(p => getSaida(p) == item.DataHora))
            return ResultadoRegistroParticipacao.Idempotente;

        return ResultadoRegistroParticipacao.EntradaAbertaNaoEncontrada;
    }

    private static void TratarResultadoRegistro(
        ResultadoRegistroParticipacao resultado,
        string leituraId,
        List<string> confirmados,
        HashSet<string> confirmadosSet,
        List<BipouLeituraErroDTO> rejeitados,
        List<string> novasParaCache,
        Action aoAlterar)
    {
        switch (resultado)
        {
            case ResultadoRegistroParticipacao.Alterado:
                aoAlterar();
                novasParaCache.Add(leituraId);
                AdicionarConfirmado(confirmados, confirmadosSet, leituraId);
                break;
            case ResultadoRegistroParticipacao.Idempotente:
                novasParaCache.Add(leituraId);
                AdicionarConfirmado(confirmados, confirmadosSet, leituraId);
                break;
            case ResultadoRegistroParticipacao.SaidaAnteriorAEntrada:
                rejeitados.Add(new BipouLeituraErroDTO(
                    leituraId,
                    "SAIDA_ANTERIOR_A_ENTRADA",
                    "A data/hora da saída não pode ser anterior ao registro de entrada em aberto."));
                break;
            case ResultadoRegistroParticipacao.EntradaAbertaNaoEncontrada:
                rejeitados.Add(new BipouLeituraErroDTO(
                    leituraId,
                    "ENTRADA_ABERTA_NAO_ENCONTRADA",
                    "Não há registro de entrada em aberto para registrar a saída."));
                break;
        }
    }

    private static BipouLeituraErroDTO? ValidarLeitura(
        BipouLeituraFrequenciaDTO leitura,
        string leituraId,
        HashSet<uint> eventosPermitidos,
        out string tipoAcaoNormalizado)
    {
        tipoAcaoNormalizado = string.Empty;

        if (leitura.IdPessoa == 0)
            return new BipouLeituraErroDTO(leituraId, "PESSOA_INVALIDA", "O identificador idPessoa deve ser maior que zero.");

        if (leitura.IdEvento == 0)
            return new BipouLeituraErroDTO(leituraId, "EVENTO_INVALIDO", "O identificador idEvento deve ser maior que zero.");

        if (leitura.DataHoraLida == default)
            return new BipouLeituraErroDTO(leituraId, "DATA_HORA_INVALIDA", "A dataHoraLida informada é inválida.");

        var acao = leitura.TipoAcao?.Trim().ToUpperInvariant();
        if (acao != TipoAcaoEntrada && acao != TipoAcaoSaida)
            return new BipouLeituraErroDTO(leituraId, "TIPO_ACAO_INVALIDO", "O campo tipoAcao deve ser ENTRADA ou SAIDA.");

        if (!eventosPermitidos.Contains(leitura.IdEvento))
            return new BipouLeituraErroDTO(leituraId, "SEM_PERMISSAO_EVENTO", "Operador não possui permissão para sincronizar frequências neste evento.");

        tipoAcaoNormalizado = acao;
        return null;
    }

    private async Task<bool> JaProcessadaNoCacheAsync(string leituraId, CancellationToken cancellationToken)
        => await _cache.GetAsync(ObterChaveCache(leituraId), cancellationToken) != null;

    private async Task RegistrarNoCacheAsync(IEnumerable<string> leituraIds, CancellationToken cancellationToken)
    {
        var opcoes = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DuracaoCacheLeitura };
        foreach (var id in leituraIds.Distinct(StringComparer.Ordinal))
        {
            await _cache.SetAsync(ObterChaveCache(id), MarcadorCache, opcoes, cancellationToken);
        }
    }

    private static string ObterChaveCache(string leituraId) => $"{PrefixoCacheLeitura}{leituraId}";

    private static void AdicionarConfirmado(List<string> confirmados, HashSet<string> set, string leituraId)
    {
        if (set.Add(leituraId))
            confirmados.Add(leituraId);
    }

    private static readonly TimeZoneInfo FusoHorarioPadrao = ObterFusoHorarioPadrao();

    private static TimeZoneInfo ObterFusoHorarioPadrao()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local;
            }
        }
    }

    private static DateTime NormalizarDataHora(DateTimeOffset dataHoraLida)
    {
        var horarioBrasil = TimeZoneInfo.ConvertTime(dataHoraLida, FusoHorarioPadrao).DateTime;
        return new DateTime(
            horarioBrasil.Year,
            horarioBrasil.Month,
            horarioBrasil.Day,
            horarioBrasil.Hour,
            horarioBrasil.Minute,
            horarioBrasil.Second,
            DateTimeKind.Unspecified);
    }

    private const string TipoAcaoEntrada = "ENTRADA";
    private const string TipoAcaoSaida = "SAIDA";

    private sealed record LeituraCandidata(
        int IndiceOriginal,
        string LeituraId,
        uint IdPessoa,
        uint IdEvento,
        uint? IdSubEvento,
        string TipoAcao,
        DateTime DataHora);

    private enum ResultadoRegistroParticipacao
    {
        Alterado,
        Idempotente,
        SaidaAnteriorAEntrada,
        EntradaAbertaNaoEncontrada
    }
}
