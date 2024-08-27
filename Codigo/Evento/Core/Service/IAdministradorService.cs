using Core.DTO;

namespace Core.Service;

public interface IAdministradorService
{
    Task CreateAsync(Pessoa pessoa);
    
    Task<IEnumerable<PessoaGestorDTO>> GetAdministradoresAsync();

    Task<UsuarioIdentity> GetbyCpfAsync(string cpf);
}
