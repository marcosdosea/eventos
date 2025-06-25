using Core;
using Core.DTO;

namespace Core.Service;

public interface IColaboradorService
{
    Task CreateAsync(Pessoa pessoa);
    Task UpdateAsync(Pessoa pessoa);
    Task DeleteAsync(string cpf);
    Task<IEnumerable<PessoaSimpleDTO>> GetColaboradoresAsync();
}