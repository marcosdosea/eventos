using Core;
using Core.DTO;

namespace Core.Service
{
    public interface IInscricaoService
    {
        void CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento);

        void DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel);

        IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel);

        IEnumerable<Inscricaopessoaevento> GetByEvento(uint idEvento);

        IEnumerable<Inscricaopessoasubevento> GetSubByEvento(uint idEvento);

    }
}

