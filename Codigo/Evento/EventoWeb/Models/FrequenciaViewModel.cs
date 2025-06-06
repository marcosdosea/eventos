using Core;
using Core.DTO;

namespace EventoWeb.Models
{
    public class FrequenciaViewModel
    {
        public EventoSimpleDTO Evento { get; set; }
        public Subevento? SubEvento { get; set; }
        public IEnumerable<Participacaopessoaevento> Frequencias { get; set; }
    }
} 