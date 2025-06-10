using Core.DTO;

namespace EventoWeb.Models
{
    public class ParticipanteModel
    {
        public PessoaModel Participante { get; set; } = new PessoaModel();
        public IEnumerable<ParticipanteDTO>? Participantes { get; set; }
    }
}