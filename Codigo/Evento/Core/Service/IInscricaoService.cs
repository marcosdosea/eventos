using Core;
using Core.DTO;

namespace Core.Service
{ 
        public interface IInscricaoService
        {
                void CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento); 
                
                void DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel);

                IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel);

                void RegistrarFrequenciaEvento(Inscricaopessoaevento inscricao,decimal frequencia);

                void RegistrarFrequenciaSubevento(Inscricaopessoasubevento inscricao,decimal frequencia);

                Inscricaopessoaevento GetInscricaoByEvento(uint idEvento, uint idPessoa);
                
                Inscricaopessoasubevento GetInscricaoBySubEvento(uint idSubEvento, uint idPessoa);
        }
}

