namespace Core.DTO
{
    /// <summary>
    /// Filtros da tela Dashboard (Aba Visão Geral &amp; Finanças / Operação &amp; Presença).
    /// Espelha o protótipo: Subevento, Tipo inscrição e período.
    /// </summary>
    public class DashboardFiltrosDTO
    {
        public uint? IdSubevento { get; set; }
        public uint? IdTipoInscricao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }
}
