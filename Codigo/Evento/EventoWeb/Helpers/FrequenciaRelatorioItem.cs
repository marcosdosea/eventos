namespace EventoWeb.Helpers
{
    /// <summary>
    /// Modelo consolidado por participante para geração de relatórios gerenciais
    /// de frequência em planilha eletrônica (XLSX / CSV).
    /// </summary>
    public sealed class FrequenciaRelatorioItem
    {
        public string? Cpf { get; set; }
        public string? NomeCompleto { get; set; }
        public string? Email { get; set; }
        public string? CursoOuSubevento { get; set; }
        public decimal CargaHorariaTotal { get; set; }
        public decimal TotalHorasPresentes { get; set; }
        public decimal PercentualFrequencia { get; set; }
        public string SituacaoFrequencia { get; set; } = "INSUFICIENTE";
        public string HistoricoEntradasSaidas { get; set; } = string.Empty;

        public bool AptoParaCertificado =>
            string.Equals(SituacaoFrequencia, "APTO PARA CERTIFICADO", StringComparison.OrdinalIgnoreCase);
    }
}
