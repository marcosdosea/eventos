using Core.DTO;

namespace EventoWeb.Models;

public class AdministradorModel
{
    public PessoaSimpleDTO administrador { get; set; }
    
    public IEnumerable<PessoaSimpleDTO> administradores { get; set; }
}