using Core.DTO;

namespace Core.Service;

public interface IAdministradorService
{
    Task CreateAsync(Pessoa pessoa);
    
    Task<IEnumerable<PessoaSimpleDTO>> GetAdministradoresAsync();

    Task<UsuarioIdentity> GetbyCpfAsync(string cpf);
}
