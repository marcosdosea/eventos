using Core.DTO;

namespace EventoWeb.Models
{
    public class ColaboradorModel
    {
        public PessoaModel Colaborador { get; set; } = new PessoaModel();
        public IEnumerable<ColaboradorDTO>? Colaboradores { get; set; }
    }
}