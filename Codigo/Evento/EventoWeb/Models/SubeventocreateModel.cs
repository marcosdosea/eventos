using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class SubeventocreateModel
    {
        public SubeventoModel Subevento { get; set; }
        public SelectList Eventos { get; set; }
        public SelectList TiposEventos { get; set; }
    }
}
