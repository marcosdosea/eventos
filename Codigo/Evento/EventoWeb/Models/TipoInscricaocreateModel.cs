using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class TipoInscricaocreateModel
    {
        public TipoInscricaoModel TipoInscricao { get; set; }
        public SelectList Evento { get; set; }

    }
}
