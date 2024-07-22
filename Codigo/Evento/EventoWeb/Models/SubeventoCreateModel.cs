using Core;
using Core.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models;

public class SubeventoCreateModel
{ 
        public SubeventoModel Subevento { get; set; } 
        public EventoSimpleDTO Evento { get; set; } 
        public SelectList TiposEventos { get; set; }
}

