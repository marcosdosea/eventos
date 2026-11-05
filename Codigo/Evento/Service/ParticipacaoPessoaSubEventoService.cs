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
            AtualizadorFrequencia.SalvarParticipacao(_context,
                () => AtualizadorFrequencia.SubeventoSeFinalizado(_context, participacao.IdSubEvento, participacao.IdPessoa));
            return participacao.Id;
        }

        public void Update(Participacaopessoasubevento participacao)
        {
            var existing = _context.Participacaopessoasubeventos.Find(participacao.Id);
            if (existing != null)
            {
                var subeventoAnterior = _context.Entry(existing).Property(p => p.IdSubEvento).OriginalValue;
                var pessoaAnterior = _context.Entry(existing).Property(p => p.IdPessoa).OriginalValue;
                existing.IdPessoa = participacao.IdPessoa;
                existing.IdSubEvento = participacao.IdSubEvento;
                existing.Entrada = participacao.Entrada;
                existing.Saida = participacao.Saida;
                AtualizadorFrequencia.SalvarParticipacao(_context, () => {
                    AtualizadorFrequencia.SubeventoSeFinalizado(_context, subeventoAnterior, pessoaAnterior);
                    if (subeventoAnterior != existing.IdSubEvento || pessoaAnterior != existing.IdPessoa)
                        AtualizadorFrequencia.SubeventoSeFinalizado(_context, existing.IdSubEvento, existing.IdPessoa);
                });
            }
            else
            {
                participacao.IdPessoaNavigation = null!;
                participacao.IdSubEventoNavigation = null!;
                _context.Participacaopessoasubeventos.Update(participacao);
                AtualizadorFrequencia.SalvarParticipacao(_context,
                    () => AtualizadorFrequencia.SubeventoSeFinalizado(_context, participacao.IdSubEvento, participacao.IdPessoa));
            }
        }

        public void Delete(uint id)
        {
            var entity = _context.Participacaopessoasubeventos.Find(id);
            if (entity != null)
            {
                _context.Participacaopessoasubeventos.Remove(entity);
                AtualizadorFrequencia.SalvarParticipacao(_context,
                    () => AtualizadorFrequencia.SubeventoSeFinalizado(_context, entity.IdSubEvento, entity.IdPessoa));
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
