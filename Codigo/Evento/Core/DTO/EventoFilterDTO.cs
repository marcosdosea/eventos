using System;

namespace Core.DTO
{
    public class EventoFilterDTO
    {
        public string? TermoBusca { get; set; }
        public uint? IdAreaInteresse { get; set; }
        public uint? IdTipoEvento { get; set; }
        public DateTime? Data { get; set; }
        public string? Estado { get; set; }
        public string? Cidade { get; set; }
    }
}
