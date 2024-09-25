using Core.DTO;

namespace Core.Service;

public interface IAdministradorService
{
    Task CreateAsync(Pessoa pessoa);
    
    Task<IEnumerable<PessoaSimpleDTO>> GetAdministradoresAsync();

    Task DeleteAsync(string cpf);
}
