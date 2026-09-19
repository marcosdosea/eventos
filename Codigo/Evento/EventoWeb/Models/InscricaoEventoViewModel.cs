using Core;
using Core.DTO;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EventoWeb.Models
{
    public class InscricaoEventoViewModel
    {
        public EventoModel eventoNavigation { get; set; }
        public IEnumerable<Tipoinscricao> tipoInscricao { get; set; }
        public InscricaoEventoModel inscricaoNavigation { get; set; } = null;
        public IEnumerable<SubeventoOpcao> SubeventosOpcoes { get; set; }
    }

    public class SubeventoOpcao
    {
        public SubeventoEventoDTO Subevento { get; set; }
        public IEnumerable<TipoInscricaoDTO> TiposInscricao { get; set; }
    }
}