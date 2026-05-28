using Core.DTO;

namespace Core.Service
{
    /// <summary>
    /// Gerencia dados de Pessoa e seu vínculo com o Identity.
    /// NÃO existe mais IColaboradorService nem IParticipanteService:
    /// os papéis (Gestor, Colaborador, Participante) são atribuídos via
    /// IInscricaoService (vínculo no evento) + IPessoaService (Identity).
    /// </summary>
    public interface IPessoaService
    {
        // --- CRUD básico ---
        uint Create(Pessoa pessoa);
        Task Edit(Pessoa pessoa);
        void Delete(uint id);
        Pessoa Get(uint id);
        IEnumerable<Pessoa> GetAll();
        Pessoa GetByCpf(string cpf);

        // --- Identity ---
        /// <summary>
        /// Cria o usuário Identity para a pessoa (senha temporária).
        /// </summary>
        Task<UsuarioIdentity> CreateAsync(Pessoa pessoa);

        /// <summary>
        /// Cria a Pessoa (se não existir), cria o usuário Identity (se não existir)
        /// e atribui o papel (role) correspondente ao idPapel no Identity.
        /// A inscrição no evento é feita separadamente via IInscricaoService.
        /// </summary>
        Task CreatePessoaIdentityComPapelAsync(Pessoa pessoa, int idPapel);

        // --- Consultas específicas ---
        Task<List<Pessoa>> GetAllAdmAsync();
        Task<List<Pessoa>> GetPessoasPorPapelNoEventoAsync(uint idEvento, int idPapel);
    }
}
