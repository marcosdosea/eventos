using Core;
using Core.DTO;

namespace EventoWeb.Models;

public class ParticipanteModel
{
    public PessoaModel Participante { get; set; }

    public IEnumerable<PessoaSimpleDTO>? Participantes { get; set; }
}
