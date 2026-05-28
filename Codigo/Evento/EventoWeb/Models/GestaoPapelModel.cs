using Core;
using Core.DTO;
using System.Collections.Generic;

namespace EventoWeb.Models
{
    public class GestaoPapelModel
    {
        public PessoaModel Pessoa { get; set; } = new PessoaModel();
        public EventoSimpleDTO Evento { get; set; } = new EventoSimpleDTO();
        public IEnumerable<Inscricaopessoaevento>? Inscricoes { get; set; }
        public int IdPapelRequisitado { get; set; }
    }
}