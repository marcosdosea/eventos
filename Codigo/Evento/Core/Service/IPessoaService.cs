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
    bool CPFIsValid(string pessoaModelCpf);
    string FormataCep(string cepFormatado);
    string FormataCPF(string pessoaCpf);
}