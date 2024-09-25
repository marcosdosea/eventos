using Core.DTO;

namespace Core.Service;

public interface IPessoaService

{
    uint Create(Pessoa pessoa);
    void Edit(Pessoa pessoa);
    void Delete(uint id);
    Pessoa Get(uint id);
    IEnumerable<Pessoa> GetAll();
    Pessoa GetByCpf(string nome); 
    Task CreatePessoaPapelAsync(Pessoa pessoa, uint idEvento, int idPapel);
    Task<UsuarioIdentity> CreateAsync(Pessoa pessoa);


}