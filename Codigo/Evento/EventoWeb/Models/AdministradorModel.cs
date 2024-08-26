using Core.DTO;

namespace EventoWeb.Models;

public class AdministradorModel
{
    public PessoaGestorDTO administrador { get; set; }
    
    public IEnumerable<PessoaGestorDTO> administradores { get; set; }
}