using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore; 

namespace Service
{
    public class ParticipacaoPessoaSubEventoService : IParticipacaoPessoaSubEventoService
    {
        private readonly EventoContext _context;

        public ParticipacaoPessoaSubEventoService(EventoContext context)
        {
            _context = context;
        }

        public uint Create(Participacaopessoasubevento participacao)
        {
            _context.Participacaopessoasubeventos.Add(participacao);
            _context.SaveChanges();
            return participacao.Id;
        }

        public void Update(Participacaopessoasubevento participacao)
        {
            _context.Participacaopessoasubeventos.Update(participacao);
            _context.SaveChanges();
        }

        public void Delete(uint id)
        {
            var entity = _context.Participacaopessoasubeventos.Find(id);
            if (entity != null)
            {
                _context.Participacaopessoasubeventos.Remove(entity);
                _context.SaveChanges();
            }
        }

        public IEnumerable<Participacaopessoasubevento> GetBySubEvento(uint idSubEvento)
        {
            return _context.Participacaopessoasubeventos
                .Include(p => p.IdPessoaNavigation)
                .Include(p => p.IdSubEventoNavigation)
                .Where(p => p.IdSubEvento == idSubEvento)
                .AsNoTracking()
                .ToList();
        }

        public IEnumerable<Participacaopessoasubevento> GetByPessoa(uint idPessoa)
        {
            return _context.Participacaopessoasubeventos
                .Include(p => p.IdPessoaNavigation)
                .Include(p => p.IdSubEventoNavigation)
                .Where(p => p.IdPessoa == idPessoa)
                .AsNoTracking()
                .ToList();
        }
    }
}
