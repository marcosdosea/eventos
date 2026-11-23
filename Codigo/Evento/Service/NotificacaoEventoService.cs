using System.Globalization;
using System.Net.Mail;
using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Service;

public class NotificacaoEventoService : INotificacaoEventoService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly EventoContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificacaoEventoService>? _logger;
    private readonly string? _caminhoTemplateHtml;
    private readonly Func<DateTime> _agora;

    public NotificacaoEventoService(
        EventoContext context,
        IEmailService emailService,
        ILogger<NotificacaoEventoService>? logger = null,
        string? caminhoTemplateHtml = null,
        Func<DateTime>? agora = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger;
        _caminhoTemplateHtml = caminhoTemplateHtml;
        _agora = agora ?? (() => DateTime.Now);
    }

    public bool EventoElegivelParaNotificacao(Evento? evento)
    {
        if (evento == null)
        {
            return false;
        }

        return string.Equals(evento.Status?.Trim(), "A", StringComparison.OrdinalIgnoreCase)
            && evento.EventoPublico == 1
            && evento.IdAreaInteresses != null
            && evento.IdAreaInteresses.Any();
    }

    public IReadOnlyList<DestinatarioNotificacaoEventoDTO> ObterDestinatariosElegiveis(uint idEvento)
    {
        var evento = _context.Eventos
            .AsNoTracking()
            .Include(e => e.IdAreaInteresses)
            .FirstOrDefault(e => e.Id == idEvento);

        if (!EventoElegivelParaNotificacao(evento))
        {
            return Array.Empty<DestinatarioNotificacaoEventoDTO>();
        }

        return ObterDestinatariosElegiveisInterno(evento!);
    }

    public async Task<ResultadoNotificacaoEventoDTO> NotificarNovoEventoAsync(
        uint idEvento,
        string linkEvento,
        bool forcarMesmoSeJaDivulgado = false,
        CancellationToken cancellationToken = default)
    {
        var resultado = new ResultadoNotificacaoEventoDTO { IdEvento = idEvento };

        var evento = await _context.Eventos
            .AsNoTracking()
            .Include(e => e.IdAreaInteresses)
            .Include(e => e.EstadoNavigation)
            .FirstOrDefaultAsync(e => e.Id == idEvento, cancellationToken);

        if (!EventoElegivelParaNotificacao(evento))
        {
            resultado.EventoElegivel = false;
            return resultado;
        }

        resultado.EventoElegivel = true;

        var existentes = await _context.Notificacaoeventos
            .Where(n => n.IdEvento == idEvento)
            .ToListAsync(cancellationToken);

        if (!forcarMesmoSeJaDivulgado && existentes.Count > 0)
        {
            resultado.JaDivulgadoAnteriormente = true;
            resultado.IgnoradosJaEnviados = existentes.Count(n => n.Status == Notificacaoevento.StatusEnviado);
            return resultado;
        }

        var destinatarios = ObterDestinatariosElegiveisInterno(evento!);
        resultado.TotalDestinatariosElegiveis = destinatarios.Count;

        if (destinatarios.Count == 0)
        {
            return resultado;
        }

        var existentesPorPessoa = existentes.ToDictionary(n => n.IdPessoa);
        var templateHtml = await CarregarTemplateHtmlAsync(cancellationToken);
        var linkFinal = ResolverLinkEvento(evento!, linkEvento);

        foreach (var destinatario in destinatarios)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (existentesPorPessoa.TryGetValue(destinatario.IdPessoa, out var registro))
            {
                if (registro.Status == Notificacaoevento.StatusEnviado)
                {
                    resultado.IgnoradosJaEnviados++;
                    continue;
                }
            }
            else
            {
                registro = new Notificacaoevento
                {
                    IdEvento = idEvento,
                    IdPessoa = destinatario.IdPessoa,
                    EmailDestino = destinatario.Email,
                    Status = Notificacaoevento.StatusPendente,
                    Tentativas = 0,
                    DataCriacao = _agora()
                };
                _context.Notificacaoeventos.Add(registro);
                existentesPorPessoa[destinatario.IdPessoa] = registro;
            }

            await ProcessarEnvioIndividualAsync(
                evento!,
                destinatario,
                registro,
                linkFinal,
                templateHtml,
                resultado,
                cancellationToken);
        }

        return resultado;
    }

    public async Task<ResultadoNotificacaoEventoDTO> ReenviarFalhasAsync(
        uint idEvento,
        string linkEvento,
        CancellationToken cancellationToken = default)
    {
        var resultado = new ResultadoNotificacaoEventoDTO { IdEvento = idEvento };

        var evento = await _context.Eventos
            .AsNoTracking()
            .Include(e => e.IdAreaInteresses)
            .Include(e => e.EstadoNavigation)
            .FirstOrDefaultAsync(e => e.Id == idEvento, cancellationToken);

        if (!EventoElegivelParaNotificacao(evento))
        {
            resultado.EventoElegivel = false;
            return resultado;
        }

        resultado.EventoElegivel = true;

        var registros = await _context.Notificacaoeventos
            .Where(n => n.IdEvento == idEvento)
            .ToListAsync(cancellationToken);

        resultado.IgnoradosJaEnviados = registros.Count(n => n.Status == Notificacaoevento.StatusEnviado);

        var pendentesOuFalhas = registros
            .Where(n => n.Status == Notificacaoevento.StatusFalha || n.Status == Notificacaoevento.StatusPendente)
            .OrderBy(n => n.Id)
            .ToList();

        if (pendentesOuFalhas.Count == 0)
        {
            return resultado;
        }

        var destinatariosVigentes = ObterDestinatariosElegiveisInterno(evento!)
            .ToDictionary(d => d.IdPessoa);

        resultado.TotalDestinatariosElegiveis = destinatariosVigentes.Count;

        var templateHtml = await CarregarTemplateHtmlAsync(cancellationToken);
        var linkFinal = ResolverLinkEvento(evento!, linkEvento);

        foreach (var registro in pendentesOuFalhas)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!destinatariosVigentes.TryGetValue(registro.IdPessoa, out var destinatarioVigente))
            {
                registro.Status = Notificacaoevento.StatusCancelado;
                registro.DataUltimaTentativa = _agora();
                registro.MensagemErro = "Preferências vigentes do participante não correspondem mais ao evento.";
                resultado.CanceladosPorPreferenciaAlterada++;
                await SalvarComSegurancaAsync(idEvento, registro.IdPessoa, resultado, cancellationToken);
                continue;
            }

            await ProcessarEnvioIndividualAsync(
                evento!,
                destinatarioVigente,
                registro,
                linkFinal,
                templateHtml,
                resultado,
                cancellationToken);
        }

        return resultado;
    }

    public async Task<IReadOnlyList<ResultadoNotificacaoEventoDTO>> ReenviarTodasFalhasAsync(
        Func<uint, string> gerarLinkEvento,
        CancellationToken cancellationToken = default)
    {
        if (gerarLinkEvento == null)
        {
            throw new ArgumentNullException(nameof(gerarLinkEvento));
        }

        var idsEventosComFalha = await _context.Notificacaoeventos
            .AsNoTracking()
            .Where(n => n.Status == Notificacaoevento.StatusFalha || n.Status == Notificacaoevento.StatusPendente)
            .Select(n => n.IdEvento)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        var resultados = new List<ResultadoNotificacaoEventoDTO>(idsEventosComFalha.Count);

        foreach (var idEvento in idsEventosComFalha)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var link = gerarLinkEvento(idEvento);
            resultados.Add(await ReenviarFalhasAsync(idEvento, link, cancellationToken));
        }

        return resultados;
    }

    public ResumoNotificacaoEventoDTO ObterResumoPorEvento(uint idEvento)
    {
        var itens = _context.Notificacaoeventos
            .AsNoTracking()
            .Where(n => n.IdEvento == idEvento)
            .Select(n => new { n.Status, n.DataUltimaTentativa })
            .ToList();

        return new ResumoNotificacaoEventoDTO
        {
            Enviados = itens.Count(i => i.Status == Notificacaoevento.StatusEnviado),
            Falhas = itens.Count(i => i.Status == Notificacaoevento.StatusFalha),
            Pendentes = itens.Count(i => i.Status == Notificacaoevento.StatusPendente),
            UltimaTentativa = itens
                .Where(i => i.DataUltimaTentativa.HasValue)
                .Select(i => i.DataUltimaTentativa)
                .Max()
        };
    }

    private IReadOnlyList<DestinatarioNotificacaoEventoDTO> ObterDestinatariosElegiveisInterno(Evento evento)
    {
        var idsAreasEvento = evento.IdAreaInteresses
            .Select(a => a.Id)
            .Distinct()
            .ToList();

        if (idsAreasEvento.Count == 0)
        {
            return Array.Empty<DestinatarioNotificacaoEventoDTO>();
        }

        var ufEvento = NormalizarUf(evento.Estado);

        var preferencias = _context.Pessoaareainteresses
            .AsNoTracking()
            .Include(p => p.IdPessoaNavigation)
            .Include(p => p.IdAreaInteresseNavigation)
            .Include(p => p.EstadoEstadosBrasils)
            .Where(p => idsAreasEvento.Contains(p.IdAreaInteresse))
            .ToList();

        return preferencias
            .Where(p => p.IdPessoaNavigation != null
                && EmailValido(p.IdPessoaNavigation.Email)
                && CorrespondeLocalizacao(p, ufEvento))
            .GroupBy(p => p.IdPessoa)
            .Select(g =>
            {
                var pessoa = g.First().IdPessoaNavigation;
                var areas = g
                    .Select(x => x.IdAreaInteresseNavigation?.Nome)
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Select(nome => nome!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new DestinatarioNotificacaoEventoDTO
                {
                    IdPessoa = pessoa.Id,
                    Nome = string.IsNullOrWhiteSpace(pessoa.Nome) ? "Participante" : pessoa.Nome.Trim(),
                    Email = pessoa.Email.Trim(),
                    AreasInteresseCorrespondentes = areas
                };
            })
            .OrderBy(d => d.IdPessoa)
            .ToList();
    }

    private async Task ProcessarEnvioIndividualAsync(
        Evento evento,
        DestinatarioNotificacaoEventoDTO destinatario,
        Notificacaoevento registro,
        string linkEvento,
        string? templateHtml,
        ResultadoNotificacaoEventoDTO resultado,
        CancellationToken cancellationToken)
    {
        var momentoTentativa = _agora();
        registro.EmailDestino = Truncar(destinatario.Email, 100);
        registro.Tentativas++;
        registro.DataUltimaTentativa = momentoTentativa;

        var assunto = $"Novo evento de seu interesse: {evento.Nome}";
        var corpoHtml = MontarCorpoHtml(evento, destinatario, linkEvento, templateHtml);

        try
        {
            var enviado = await _emailService.TentarEnviarEmailAsync(destinatario.Email, assunto, corpoHtml);
            if (enviado)
            {
                registro.Status = Notificacaoevento.StatusEnviado;
                registro.DataEnvio = _agora();
                registro.MensagemErro = null;
                resultado.Enviados++;
            }
            else
            {
                const string erroPadrao = "Falha ao enviar e-mail de notificação (verifique a configuração SMTP).";
                registro.Status = Notificacaoevento.StatusFalha;
                registro.MensagemErro = erroPadrao;
                resultado.Falhas++;
                resultado.Erros.Add($"Pessoa {destinatario.IdPessoa} ({destinatario.Email}): {erroPadrao}");
                _logger?.LogWarning(
                    "Falha ao enviar notificação do evento {IdEvento} para pessoa {IdPessoa} ({Email}).",
                    evento.Id,
                    destinatario.IdPessoa,
                    destinatario.Email);
            }
        }
        catch (Exception ex)
        {
            var erro = Truncar(ex.Message, 500);
            registro.Status = Notificacaoevento.StatusFalha;
            registro.MensagemErro = erro;
            resultado.Falhas++;
            resultado.Erros.Add($"Pessoa {destinatario.IdPessoa} ({destinatario.Email}): {erro}");
            _logger?.LogError(
                ex,
                "Exceção ao enviar notificação do evento {IdEvento} para pessoa {IdPessoa} ({Email}).",
                evento.Id,
                destinatario.IdPessoa,
                destinatario.Email);
        }

        await SalvarComSegurancaAsync(evento.Id, destinatario.IdPessoa, resultado, cancellationToken);
    }

    private async Task SalvarComSegurancaAsync(
        uint idEvento,
        uint idPessoa,
        ResultadoNotificacaoEventoDTO resultado,
        CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var erro = Truncar($"Erro ao persistir registro de notificação: {ex.Message}", 500);
            resultado.Erros.Add($"Pessoa {idPessoa}: {erro}");
            _logger?.LogError(
                ex,
                "Erro ao persistir status de notificação do evento {IdEvento} para pessoa {IdPessoa}.",
                idEvento,
                idPessoa);
        }
    }

    private async Task<string?> CarregarTemplateHtmlAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_caminhoTemplateHtml) || !File.Exists(_caminhoTemplateHtml))
        {
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(_caminhoTemplateHtml, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Não foi possível ler template de e-mail em {Caminho}. Usando fallback HTML.", _caminhoTemplateHtml);
            return null;
        }
    }

    public static string MontarCorpoHtml(
        Evento evento,
        DestinatarioNotificacaoEventoDTO destinatario,
        string linkEvento,
        string? templateHtml = null)
    {
        var nomeParticipante = EscaparHtml(destinatario.Nome);
        var nomeEvento = EscaparHtml(evento.Nome);
        var dataFormatada = EscaparHtml(FormatarDataEvento(evento.DataInicio, evento.DataFim));
        var localFormatado = EscaparHtml(FormatarLocalEvento(evento));
        var areasTexto = destinatario.AreasInteresseCorrespondentes.Count > 0
            ? EscaparHtml(string.Join(", ", destinatario.AreasInteresseCorrespondentes))
            : "Suas áreas de interesse";
        var linkSeguro = EscaparHtml(linkEvento);

        if (!string.IsNullOrWhiteSpace(templateHtml))
        {
            return templateHtml
                .Replace("{{NomeParticipante}}", nomeParticipante)
                .Replace("{{NomeEvento}}", nomeEvento)
                .Replace("{{DataEvento}}", dataFormatada)
                .Replace("{{LocalEvento}}", localFormatado)
                .Replace("{{AreasInteresse}}", areasTexto)
                .Replace("{{LinkEvento}}", linkSeguro);
        }

        return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<body style=""font-family: Arial, sans-serif; color: #222; line-height: 1.5;"">
    <h2>Olá, {nomeParticipante}!</h2>
    <p>Um novo evento que combina com suas preferências foi publicado no <strong>Boralá</strong>:</p>
    <ul>
        <li><strong>Evento:</strong> {nomeEvento}</li>
        <li><strong>Data:</strong> {dataFormatada}</li>
        <li><strong>Local:</strong> {localFormatado}</li>
        <li><strong>Áreas de interesse:</strong> {areasTexto}</li>
    </ul>
    <p><a href=""{linkSeguro}"">Ver detalhes e inscrever-se no evento</a></p>
    <p style=""font-size: 12px; color: #666;"">Caso o botão não funcione, acesse: {linkSeguro}</p>
</body>
</html>";
    }

    public static string FormatarDataEvento(DateTime? dataInicio, DateTime? dataFim)
    {
        if (!dataInicio.HasValue && !dataFim.HasValue)
        {
            return "Data a definir";
        }

        if (dataInicio.HasValue && !dataFim.HasValue)
        {
            return dataInicio.Value.ToString("dd/MM/yyyy 'às' HH:mm", PtBr);
        }

        if (!dataInicio.HasValue && dataFim.HasValue)
        {
            return $"Até {dataFim.Value.ToString("dd/MM/yyyy 'às' HH:mm", PtBr)}";
        }

        var inicio = dataInicio!.Value;
        var fim = dataFim!.Value;

        if (inicio.Date == fim.Date)
        {
            return $"{inicio.ToString("dd/MM/yyyy", PtBr)} das {inicio.ToString("HH:mm", PtBr)} às {fim.ToString("HH:mm", PtBr)}";
        }

        return $"{inicio.ToString("dd/MM/yyyy HH:mm", PtBr)} até {fim.ToString("dd/MM/yyyy HH:mm", PtBr)}";
    }

    public static string FormatarLocalEvento(Evento evento)
    {
        var partesEndereco = new List<string>();

        if (!string.IsNullOrWhiteSpace(evento.Rua))
        {
            var rua = evento.Rua.Trim();
            if (!string.IsNullOrWhiteSpace(evento.Numero))
            {
                rua = $"{rua}, {evento.Numero.Trim()}";
            }
            partesEndereco.Add(rua);
        }

        if (!string.IsNullOrWhiteSpace(evento.Complemento))
        {
            partesEndereco.Add(evento.Complemento.Trim());
        }

        if (!string.IsNullOrWhiteSpace(evento.Bairro))
        {
            partesEndereco.Add(evento.Bairro.Trim());
        }

        var cidade = evento.Cidade?.Trim();
        var uf = NormalizarUf(evento.Estado);
        var nomeEstado = evento.EstadoNavigation?.Nome?.Trim();

        if (!string.IsNullOrWhiteSpace(cidade) && !string.IsNullOrWhiteSpace(uf))
        {
            partesEndereco.Add($"{cidade} - {uf}");
        }
        else if (!string.IsNullOrWhiteSpace(cidade))
        {
            partesEndereco.Add(cidade);
        }
        else if (!string.IsNullOrWhiteSpace(nomeEstado) && !string.IsNullOrWhiteSpace(uf))
        {
            partesEndereco.Add($"{nomeEstado} ({uf})");
        }
        else if (!string.IsNullOrWhiteSpace(uf))
        {
            partesEndereco.Add(uf);
        }

        return partesEndereco.Count > 0
            ? string.Join(" - ", partesEndereco)
            : "Local a definir";
    }

    private static bool CorrespondeLocalizacao(Pessoaareainteresse preferencia, string? ufEvento)
    {
        if (preferencia.TodosEstados == 1)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(ufEvento))
        {
            return false;
        }

        return preferencia.EstadoEstadosBrasils != null
            && preferencia.EstadoEstadosBrasils.Any(e =>
                string.Equals(NormalizarUf(e.Estado), ufEvento, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizarUf(string? uf)
    {
        return string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();
    }

    private static bool EmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email.Trim());
            return address.Address.Contains('@');
        }
        catch
        {
            return false;
        }
    }

    private static string ResolverLinkEvento(Evento evento, string? linkEvento)
    {
        if (!string.IsNullOrWhiteSpace(linkEvento))
        {
            return linkEvento.Trim();
        }

        if (!string.IsNullOrWhiteSpace(evento.Website))
        {
            var site = evento.Website.Trim();
            return site.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   site.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? site
                : $"https://{site}";
        }

        return $"/Inscricao/realizarInscricao?idEvento={evento.Id}";
    }

    private static string EscaparHtml(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        return texto
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static string Truncar(string? texto, int maxLength)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        return texto.Length <= maxLength ? texto : texto[..maxLength];
    }
}
