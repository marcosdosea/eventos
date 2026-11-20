namespace Core.DTO
{
    public class ParticipacaoPessoaEventoDTO
    {
        public uint Id { get; set; }
        public uint IdPessoa { get; set; }
        public uint IdEvento { get; set; }
        public DateTime Entrada { get; set; }
        public DateTime? Saida { get; set; }
    }

    public class ResultadoImportacaoParticipantesDTO
    {
        public int TotalLidos { get; set; }
        public int TotalImportados { get; set; }
        public int TotalJaRegistrados { get; set; }
        public int TotalFalhas { get; set; }
        public List<string> Inconsistencias { get; set; } = new();
    }
}
