﻿namespace Core.DTO
{
    public class EventoDTO
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

        public DateTime DataInicio { get; set; }

        public DateTime DataFim { get; set; }
    }
    public class EventoSimpleDTO
    {
        public uint Id { get; set; }

        public string Nome { get; set; } = null!;
    }
}
