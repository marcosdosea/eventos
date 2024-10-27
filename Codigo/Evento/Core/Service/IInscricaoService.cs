using Core;
using Core.DTO;

namespace Core.Service
{
    public interface IInscricaoService
    {
        uint CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento);

        void DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel, string cpf);

        IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel);

        IEnumerable<Inscricaopessoaevento> GetByEvento(uint idEvento);

        int GetPapelPessoaByEvento(uint idPessoa, uint idEvento);

        IEnumerable<Inscricaopessoasubevento> GetSubByEvento(uint idEvento);

        IEnumerable<Inscricaopessoaevento> GetAllEventsByUserId(string username);

    }
}

