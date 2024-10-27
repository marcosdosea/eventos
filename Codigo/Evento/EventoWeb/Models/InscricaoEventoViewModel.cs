using Core;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class InscricaoEventoViewModel
    {
        public EventoModel eventoNavigation { get; set; }
        public IEnumerable<Tipoinscricao> tipoInscricao { get; set; }
        public InscricaoEventoModel inscricaoNavigation { get; set; } = null;
    }
}