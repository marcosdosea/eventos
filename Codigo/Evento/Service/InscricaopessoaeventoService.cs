using Core; // Para acessar EventoContext e entidades
using Core.Service; // Para acessar a interface
using System.Collections.Generic;
using System.Linq;

namespace Service
{
    public class InscricaopessoaeventoService : IInscricaopessoaeventoService
    {
        private readonly EventoContext _context;

        public InscricaopessoaeventoService(EventoContext context)
        {
            _context = context;
        }

        public void Create(Inscricaopessoaevento inscricao)
        {
            // Evitar inscrição duplicada para a mesma pessoa e evento
            if (PessoaJaInscrita(inscricao.IdPessoa, inscricao.IdEvento))
                throw new System.Exception("Esta pessoa já está inscrita neste evento.");

            inscricao.DataInscricao = System.DateTime.Now;
            _context.Inscricaopessoaeventos.Add(inscricao); // <- Usando DbSet no plural!
            _context.SaveChanges();
        }

        public Inscricaopessoaevento GetById(uint id)
        {
            return _context.Inscricaopessoaeventos.Find(id);
        }

        public IEnumerable<Inscricaopessoaevento> GetAll()
        {
            return _context.Inscricaopessoaeventos.ToList();
        }

        public void Update(Inscricaopessoaevento inscricao)
        {
            _context.Inscricaopessoaeventos.Update(inscricao);
            _context.SaveChanges();
        }

        public void Delete(uint id)
        {
            var inscricao = GetById(id);
            if (inscricao != null)
            {
                _context.Inscricaopessoaeventos.Remove(inscricao);
                _context.SaveChanges();
            }
        }

        public bool PessoaJaInscrita(uint idPessoa, uint idEvento)
        {
            return _context.Inscricaopessoaeventos.Any(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento);
        }
    }
}
