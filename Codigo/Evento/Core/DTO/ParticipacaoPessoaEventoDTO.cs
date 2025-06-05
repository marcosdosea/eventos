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
}
