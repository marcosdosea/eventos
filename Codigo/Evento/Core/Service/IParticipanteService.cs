using Core.DTO;

namespace Core.Service;

public interface IParticipanteService
{
    Task CreateAsync(Pessoa pessoa);

    Task<IEnumerable<PessoaSimpleDTO>> GetParticipantesAsync();

    Task DeleteAsync(string cpf);

    Task<Pessoa?> GetParticipanteByCpfAsync(string cpf);
}
