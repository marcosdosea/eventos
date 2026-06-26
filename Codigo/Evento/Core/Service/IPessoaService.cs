namespace Core.Service
{

    public interface IPessoaService
    {

        uint Create(Pessoa pessoa);
        Task Edit(Pessoa pessoa);
        bool Delete(uint id);
        Pessoa Get(uint id);
        IEnumerable<Pessoa> GetAll();
        Pessoa GetByCpf(string cpf);
        Task<bool> IsAdmAsync(Pessoa pessoa);
        Task<List<Pessoa>> GetAllAdmAsync();
        Task<List<Pessoa>> GetAllGestorAsync();
        Task<UsuarioIdentity> CreateAsync(Pessoa pessoa);
        Task <bool> CreatePessoaIdentityComPapelAsync(Pessoa pessoa, uint idEvento, int idPapel);
        Task<List<Pessoa>> GetPessoasPorPapelNoEventoAsync(uint idEvento, int idPapel);

    }
}
