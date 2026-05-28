using Core;

namespace Core.Service
{
    /// <summary>
    /// Interface unificada de inscrições.
    /// Absorve IInscricaopessoaeventoService (eliminado por redundância).
    /// </summary>
    public interface IInscricaoService
    {
        // --- Inscrição em Evento ---
        uint CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento);
        void CreateInscricaoSubEvento(Inscricaopessoasubevento inscricaopessoasubevento);

        // --- Atribuição / Remoção de Papel no Evento ---
        Task AtribuirPapelNoEventoAsync(Pessoa pessoa, uint idEvento, int idPapel);
        Task DeletePessoaPapelAsync(uint idPessoa, uint idEvento, uint idPapel, string cpf);

        // --- Consultas por Evento ---
        IEnumerable<Inscricaopessoaevento> GetByEvento(uint idEvento);
        IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel);
        IEnumerable<Inscricaopessoasubevento> GetSubByEvento(uint idEvento);

        // --- Consultas por Usuário ---
        IEnumerable<Inscricaopessoaevento> GetAllEventsByUserId(string username);
        int GetPapelPessoaByEvento(uint idPessoa, uint idEvento);

        // --- Verificações de papel no evento (usadas em autorização) ---
        Inscricaopessoaevento GetGestorInEvent(string username, uint idEvento);
        Inscricaopessoaevento GetColaboradorInEvent(string username, uint idEvento);

        // --- CRUD direto de inscrição (admin) ---
        Inscricaopessoaevento GetById(uint id);
        IEnumerable<Inscricaopessoaevento> GetAll();
        void Update(Inscricaopessoaevento inscricao);
        void Delete(uint id);
        bool PessoaJaInscrita(uint idPessoa, uint idEvento);
    }
}
