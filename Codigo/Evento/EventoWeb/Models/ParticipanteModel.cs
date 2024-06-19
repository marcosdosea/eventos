using Core;
using Core.DTO;

namespace EventoWeb.Models;

public class ParticipanteModel
{
    public Pessoa Pessoa {  get; set; }

    public EventoSimpleDTO Evento { get; set; }

    public IEnumerable<Inscricaopessoaevento> Inscricoes { get; set; }
}
