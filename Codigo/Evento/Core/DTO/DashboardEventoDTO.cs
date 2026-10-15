namespace Core.DTO
{
    /// <summary>
    /// Agregado do caso de uso Consultar Dashboard Evento (Issue #727).
    /// Uma instância = as duas abas do protótipo já com filtros aplicados.
    /// </summary>
    public class DashboardEventoDTO
    {
        public uint IdEvento { get; set; }
        public string NomeEvento { get; set; } = null!;
        public string Status { get; set; } = null!;

        public DashboardFiltrosDTO Filtros { get; set; } = new();
        public DashboardFinanceiroDTO Financeiro { get; set; } = new();
        public DashboardOperacaoDTO Operacao { get; set; } = new();
    }
}
