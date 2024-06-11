using Core;
using Core.DTO;

namespace Core.Service
{ 
        public interface IInscricaoService
        {
                void CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento); 
                
                void DeletePessoaPapel(uint idPessoa, uint idEvento);

                IEnumerable<Inscricaopessoaevento> GetInscricaoPessoaEvento(uint id);
                
        }
}

