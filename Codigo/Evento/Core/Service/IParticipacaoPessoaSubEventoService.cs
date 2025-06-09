using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public interface IParticipacaoPessoaSubEventoService
    {
        uint Create(Participacaopessoasubevento participacao);
        void Update(Participacaopessoasubevento participacao);
        void Delete(uint id);
        IEnumerable<Participacaopessoasubevento> GetBySubEvento(uint idSubEvento);
        IEnumerable<Participacaopessoasubevento> GetByPessoa(uint idPessoa);
    }
}
