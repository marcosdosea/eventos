namespace Core.DTO
{
    public class TipoInscicaoDTO
    {
        public int Id { get; set; }
    
        public uint IdEvento { get; set; }

        public string Descricao { get; set; } = null!;

        public decimal Valor { get; set; }

        public DateTime DataInicio { get; set; }

        public DateTime Datafim { get; set; }
    }
}