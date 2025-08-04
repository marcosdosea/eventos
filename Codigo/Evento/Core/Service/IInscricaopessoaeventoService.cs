using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;


namespace Core.Service
{
    public interface IInscricaopessoaeventoService
    {
        void Create(Inscricaopessoaevento inscricao);
        Inscricaopessoaevento GetById(int id);
        IEnumerable<Inscricaopessoaevento> GetAll();
        void Update(Inscricaopessoaevento inscricao);
        void Delete(int id);
        //Verifica se a pessoa já está inscrita no evento
        bool PessoaJaInscrita(int pessoaId, int eventoId);
    }
}
