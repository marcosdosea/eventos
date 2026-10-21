using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EventoWeb.Models;

/// <summary>
/// ViewModel da tela Dashboard (Issue #727).
/// Espelha o protótipo de alta fidelidade: header + filtros + 2 abas.
/// Aba ativa controlada por query (?aba=financas|operacao).
/// </summary>
public class DashboardEventoViewModel
{
    public uint IdEvento { get; set; }
    public string NomeEvento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Filtros (protótipo: subevento, tipo, período)
    public uint? IdSubevento { get; set; }
    public uint? IdTipoInscricao { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string Aba { get; set; } = "financas";

    public DashboardFinanceiroDTO Financeiro { get; set; } = new();
    public DashboardOperacaoDTO Operacao { get; set; } = new();

    /// <summary>Linhas do CSV (cabeçalho + dados) exibidas na pré-visualização antes do download.</summary>
    public List<string[]> CsvPreview { get; set; } = new();

    [ValidateNever]
    public SelectList Subeventos { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());

    [ValidateNever]
    public SelectList TiposInscricao { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
}
