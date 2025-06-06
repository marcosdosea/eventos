using Core;
using Core.DTO;

namespace EventoWeb.Models;

public class ColaboradorModel
{
    public PessoaModel? Colaborador { get; set; }
    
    public IEnumerable<PessoaSimpleDTO>? Colaboradores { get; set; }
}