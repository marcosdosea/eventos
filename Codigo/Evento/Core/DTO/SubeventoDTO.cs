namespace Core.DTO
{
    public class SubeventoDTO
    {
        public uint Id { get; set; }

        public string Nome { get; set; } = null!;

        public string Descricao { get; set; } = null!;

        /// <summary>
        /// C- CADASTRO
        /// A- ABERTO
        /// F- FINALIZADO
        ///  
        /// </summary>
        public string Status { get; set; } = null!;

        public DateTime DataInicioInscricao { get; set; }

        public DateTime DataFimInscricao { get; set; }
    }
}
