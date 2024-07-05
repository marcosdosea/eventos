using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
	public class EventocreateModel
	{
		public EventoModel Evento { get; set; }
		public SelectList Estados { get; set; }
        public SelectList TiposEventos { get; set; }
		public SelectList AreaInteresse { get; set; }
    }
}
