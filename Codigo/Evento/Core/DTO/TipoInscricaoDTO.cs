namespace Core.DTO
{
    public class TipoInscricaoDTO
    {
        public uint Id { get; set; }

        public string Nome { get; set; } = null!;

        public uint IdEvento { get; set; }

        public string Descricao { get; set; } = null!;

        public decimal Valor { get; set; }

        public DateTime DataInicio { get; set; }

        public DateTime Datafim { get; set; }
    }
}