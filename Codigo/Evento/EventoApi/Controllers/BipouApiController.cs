using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Core;
using Core.DTO;
using Core.Service;
using EventoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace EventoApi.Controllers;

[ApiController]
[Route("api/bipou/v1")]
[Authorize(Policy = "BipouOperador")]
public class BipouApiController : ControllerBase
{
    private readonly IEventoService eventos;
    private readonly ISubeventoService subeventos;
    private readonly IInscricaoService inscricoes;
    private readonly ISincronizacaoFrequenciaService sincronizacaoFrequencia;
    private readonly UserManager<UsuarioIdentity> usuarios;
    private readonly SignInManager<UsuarioIdentity> login;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaximoLeiturasPorLote = 500;

    public BipouApiController(IEventoService eventos, ISubeventoService subeventos, IInscricaoService inscricoes,
        ISincronizacaoFrequenciaService sincronizacaoFrequencia,
        UserManager<UsuarioIdentity> usuarios, SignInManager<UsuarioIdentity> login)
    {
        this.eventos = eventos;
        this.subeventos = subeventos;
        this.inscricoes = inscricoes;
        this.sincronizacaoFrequencia = sincronizacaoFrequencia;
        this.usuarios = usuarios;
        this.login = login;
    }

    [HttpPost("sessao")]
    [AllowAnonymous]
    [EnableRateLimiting("bipou-sessao")]
    public async Task<IActionResult> CriarSessao(BipouSessaoModel model)
    {
        Response.Headers.CacheControl = "no-store";
        var usuario = await usuarios.FindByNameAsync(model.Usuario);
        if (usuario == null && model.Usuario.Contains('@'))
            usuario = await usuarios.FindByEmailAsync(model.Usuario);
        if (usuario == null)
            return Unauthorized();

        var resultado = await login.CheckPasswordSignInAsync(usuario, model.Senha, lockoutOnFailure: true);
        // Uma conta com segundo fator não pode contorná-lo pela API de operador.
        if (!resultado.Succeeded || await usuarios.GetTwoFactorEnabledAsync(usuario))
            return Unauthorized();
        var principal = await login.CreateUserPrincipalAsync(usuario);
        if (!principal.IsInRole("GESTOR") && !principal.IsInRole("COLABORADOR"))
            return Forbid(IdentityConstants.BearerScheme);
        return SignIn(principal, IdentityConstants.BearerScheme);
    }

    [HttpGet("eventos")]
    public async Task<IActionResult> GetEventos(CancellationToken cancellationToken)
    {
        var permitidos = await EventosPermitidos(cancellationToken);
        return permitidos == null ? Forbid(IdentityConstants.BearerScheme) : Responder(permitidos);
    }

    [HttpGet("eventos/{idEvento:long:min(1):max(4294967295)}/subeventos")]
    public async Task<IActionResult> GetSubeventos(uint idEvento, CancellationToken cancellationToken)
    {
        if (await EventoPermitido(idEvento, cancellationToken) == null) return Forbid(IdentityConstants.BearerScheme);
        return Responder(await subeventos.GetSubeventosBipouAsync(idEvento, cancellationToken));
    }

    [HttpGet("eventos/{idEvento:long:min(1):max(4294967295)}/participantes")]
    public async Task<IActionResult> GetParticipantes(uint idEvento, [FromQuery] uint? subEventoId,
        [FromQuery] DateTimeOffset? atualizadoApos, CancellationToken cancellationToken)
    {
        if (await EventoPermitido(idEvento, cancellationToken) == null) return Forbid(IdentityConstants.BearerScheme);
        if (subEventoId.HasValue && !(await subeventos.GetSubeventosBipouAsync(idEvento, cancellationToken))
            .Any(s => s.IdSubEvento == subEventoId.Value)) return NotFound();
        // Sem histórico de alterações no modelo, o cliente sempre substitui a carga completa.
        Response.Headers["X-Bipou-Carga-Completa"] = "true";
        return Responder(await inscricoes.GetParticipantesBipouAsync(idEvento, subEventoId, cancellationToken));
    }

    [HttpGet("eventos/{idEvento:long:min(1):max(4294967295)}/participantes/export-carga")]
    public async Task<IActionResult> ExportarCarga(uint idEvento, CancellationToken cancellationToken)
    {
        var evento = await EventoPermitido(idEvento, cancellationToken);
        if (evento == null) return Forbid(IdentityConstants.BearerScheme);
        var carga = new BipouCargaDTO(1, evento,
            await subeventos.GetSubeventosBipouAsync(idEvento, cancellationToken),
            await inscricoes.GetParticipantesBipouAsync(idEvento, cancellationToken: cancellationToken));
        return Responder(carga, $"bipou-evento-{idEvento}.json.gz");
    }

    [HttpPost("frequencias/lote")]
    public async Task<IActionResult> SincronizarFrequenciasLote(
        [FromBody] BipouLoteFrequenciaRequestDTO? request,
        CancellationToken cancellationToken)
    {
        if (request?.Leituras == null || request.Leituras.Count == 0)
            return BadRequest(new { mensagem = "Informe ao menos uma leitura de frequência para sincronização." });

        if (request.Leituras.Count > MaximoLeiturasPorLote)
            return BadRequest(new { mensagem = $"O lote excede o limite máximo de {MaximoLeiturasPorLote} leituras por requisição." });

        var permitidos = await EventosPermitidos(cancellationToken);
        if (permitidos == null)
            return Forbid(IdentityConstants.BearerScheme);

        var eventosPermitidosIds = permitidos.Select(e => e.IdEvento).ToHashSet();
        var idsEventosInformados = request.Leituras
            .Where(l => l != null && l.IdEvento > 0)
            .Select(l => l.IdEvento)
            .ToList();

        if (idsEventosInformados.Count > 0 && idsEventosInformados.All(id => !eventosPermitidosIds.Contains(id)))
            return Forbid(IdentityConstants.BearerScheme);

        var resultado = await sincronizacaoFrequencia.SincronizarLoteAsync(
            request.Leituras,
            eventosPermitidosIds,
            cancellationToken);

        return Ok(resultado);
    }

    private async Task<List<BipouEventoDTO>?> EventosPermitidos(CancellationToken cancellationToken)
    {
        // Tokens podem permanecer válidos após mudança de papel ou bloqueio da conta.
        var usuario = await usuarios.GetUserAsync(User);
        if (usuario == null || await usuarios.IsLockedOutAsync(usuario) ||
            !await login.CanSignInAsync(usuario) || await usuarios.GetTwoFactorEnabledAsync(usuario))
            return null;
        var papeis = await usuarios.GetRolesAsync(usuario);
        var gestor = User.IsInRole("GESTOR") && papeis.Contains("GESTOR");
        var colaborador = User.IsInRole("COLABORADOR") && papeis.Contains("COLABORADOR");
        if (!gestor && !colaborador) return null;
        return await eventos.GetEventosBipouAsync(usuario.UserName ?? string.Empty, gestor, colaborador, cancellationToken);
    }

    private async Task<BipouEventoDTO?> EventoPermitido(uint id, CancellationToken cancellationToken)
        => (await EventosPermitidos(cancellationToken))?.FirstOrDefault(e => e.IdEvento == id);

    private IActionResult Responder<T>(T dados, string? arquivo = null)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(dados, JsonOptions);
        var etag = new EntityTagHeaderValue('"' + Convert.ToHexString(SHA256.HashData(json)) + '"', isWeak: true);
        Response.Headers.ETag = etag.ToString();
        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.Vary = "Authorization, Accept-Encoding";
        if (EntityTagHeaderValue.TryParseList(Request.Headers.IfNoneMatch.Where(t => t != null).Select(t => t!).ToList(), out var tags) &&
            tags.Any(t => t.Tag == "*" || t.Compare(etag, useStrongComparison: false)))
            return StatusCode(StatusCodes.Status304NotModified);
        if (arquivo == null) return File(json, "application/json; charset=utf-8");
        using var saida = new MemoryStream();
        using (var gzip = new GZipStream(saida, CompressionLevel.Fastest, leaveOpen: true)) gzip.Write(json);
        return File(saida.ToArray(), "application/gzip", arquivo);
    }
}

