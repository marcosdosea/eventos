using Core;
using Core.DTO;

namespace Core.Service
{
    public interface IInscricaoService
    {
        uint CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento);

        uint CreateInscricaoEventoLote(Inscricaopessoaevento inscricaopessoaevento);

        void CreateInscricaoSubEvento(Inscricaopessoasubevento inscricaopessoasubevento);

        void CreateInscricoesEmLote(uint idEvento, IEnumerable<Inscricaopessoaevento> eventos, IEnumerable<Inscricaopessoasubevento> subeventos, IEnumerable<uint> idsSubeventosAtualizarVagas);

        Task DeletePessoaPapelAsync(uint idPessoa, uint idEvento, uint idPapel, string cpf);

        /// <summary>
        /// Cancela as inscrições de participante (papel 4) de uma pessoa em um evento,
        /// incluindo as inscrições nos subeventos, e libera as vagas do evento e dos
        /// subeventos afetados em um único SaveChanges. Retorna a quantidade de
        /// inscrições do evento removidas.
        /// </summary>
        int CancelarInscricaoParticipante(uint idPessoa, uint idEvento);

        IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel);

        IEnumerable<Inscricaopessoaevento> GetByEvento(uint idEvento);

        int GetPapelPessoaByEvento(uint idPessoa, uint idEvento);

        IEnumerable<Inscricaopessoasubevento> GetSubByEvento(uint idEvento);

        IEnumerable<Inscricaopessoasubevento> GetAllSubEventsByUserId(string username);

        IEnumerable<Inscricaopessoaevento> GetAllEventsByUserId(string username);

        Inscricaopessoaevento GetGestorInEvent(string username, uint idEvento);

        Inscricaopessoaevento GetColaboradorInEvent(string username, uint idEvento);

        bool IsInscrito(uint idPessoa, uint idEvento);

        void UpdateNomeCracha(uint idPessoa, uint idEvento, string nomeCracha);
    }
}

