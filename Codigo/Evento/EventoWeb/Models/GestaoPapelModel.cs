using Core;
using Core.DTO;

namespace EventoWeb.Models;

public class GestaoPapelModel
{
    public PessoaModel Pessoa { get; set; }
    
    public EventoSimpleDTO Evento { get; set; }
    public IEnumerable<Inscricaopessoaevento>? Inscricoes { get; set; }
    
}