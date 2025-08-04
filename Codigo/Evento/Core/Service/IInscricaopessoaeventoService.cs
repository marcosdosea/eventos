using Core;
using System.Collections.Generic;

namespace Core.Service
{
    public interface IInscricaopessoaeventoService
    {
        void Create(Inscricaopessoaevento inscricao);
        Inscricaopessoaevento GetById(uint id);
        IEnumerable<Inscricaopessoaevento> GetAll();
        void Update(Inscricaopessoaevento inscricao);
        void Delete(uint id);

        // Verifica se uma pessoa já está inscrita em um evento
        bool PessoaJaInscrita(uint idPessoa, uint idEvento);
    }
}
