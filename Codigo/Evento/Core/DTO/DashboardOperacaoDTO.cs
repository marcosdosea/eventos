namespace Core.DTO
{
    public class OcupacaoSubeventoDTO
    {
        public uint IdSubevento { get; set; }
        public string Nome { get; set; } = null!;
        public int Ocupados { get; set; }
        public int VagasOfertadas { get; set; }
        public decimal Percentual { get; set; }
    }

    public class DistribuicaoSexoDTO
    {
        public int Feminino { get; set; }
        public decimal FemininoPercentual { get; set; }
        public int Masculino { get; set; }
        public decimal MasculinoPercentual { get; set; }
        public int NaoInformado { get; set; }
    }

    public class OrigemGeograficaDTO
    {
        public string Estado { get; set; } = null!;
        public int Quantidade { get; set; }
        public decimal Percentual { get; set; }
    }

    /// <summary>
    /// Aba 2 do protótipo: Operação &amp; Presença.
    /// </summary>
    public class DashboardOperacaoDTO
    {
        public int Presentes { get; set; }
        public int Ausentes { get; set; }
        public int Total => Presentes + Ausentes;

        public int AptosCertificado { get; set; }
        public decimal FrequenciaMedia { get; set; }

        public List<OcupacaoSubeventoDTO> OcupacaoSubeventos { get; set; } = new();
        public DistribuicaoSexoDTO PorSexo { get; set; } = new();
        public List<OrigemGeograficaDTO> PorOrigem { get; set; } = new();
    }
}
