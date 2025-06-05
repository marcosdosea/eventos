using System.Collections.Generic;
using System.Threading.Tasks;
using Core;

namespace Core.Service
{
    public interface IParticipacaoPessoaEventoService
    {
        Task<List<Participacaopessoaevento>> GetAllAsync();
        Task<Participacaopessoaevento> GetByIdAsync(uint id);
        Task<Participacaopessoaevento> AddAsync(Participacaopessoaevento entity);
        Task<bool> UpdateAsync(Participacaopessoaevento entity);
        Task<bool> DeleteAsync(uint id);
    }
}
