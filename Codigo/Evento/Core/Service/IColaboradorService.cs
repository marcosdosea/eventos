using Core.DTO;

namespace Core.Service;

public interface IColaboradorService
{
    Task CreateAsync(Pessoa pessoa);

    Task<IEnumerable<PessoaSimpleDTO>> GetColaboradoresAsync();

    Task DeleteAsync(string cpf);
}