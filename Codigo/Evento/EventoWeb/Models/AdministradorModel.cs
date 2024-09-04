using Core;
using Core.DTO;

namespace EventoWeb.Models;

public class AdministradorModel
{
    public PessoaModel Administrador { get; set; }
    
    public IEnumerable<PessoaSimpleDTO>? Administradores { get; set; }
}