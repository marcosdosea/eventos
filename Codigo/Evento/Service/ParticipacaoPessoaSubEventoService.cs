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
        private readonly ISubeventoService _subeventoService;

        public ParticipacaoPessoaSubEventoService(EventoContext context, ISubeventoService? subeventoService = null)
        {
            _context = context;
            _subeventoService = subeventoService ?? new SubeventoService(context);
        }

        private void RecalcularFrequenciaSeFinalizado(uint idSubEvento, uint idPessoa)
        {
            var subevento = _context.Subeventos.Find(idSubEvento);
            if (subevento == null) return;
            var evento = _context.Eventos.Find(subevento.IdEvento);
            if (subevento.Status == "F" || evento?.Status == "F")
            {
                _subeventoService.AtualizarFrequenciaFinal(idSubEvento, idPessoa, saveChanges: false);
            }
        }

        public uint Create(Participacaopessoasubevento participacao)
        {
            _context.Participacaopessoasubeventos.Add(participacao);
            RecalcularFrequenciaSeFinalizado(participacao.IdSubEvento, participacao.IdPessoa);
            _context.SaveChanges();
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
                RecalcularFrequenciaSeFinalizado(subeventoAnterior, pessoaAnterior);
                if (subeventoAnterior != existing.IdSubEvento || pessoaAnterior != existing.IdPessoa)
                    RecalcularFrequenciaSeFinalizado(existing.IdSubEvento, existing.IdPessoa);
                _context.SaveChanges();
            }
            else
            {
                participacao.IdPessoaNavigation = null!;
                participacao.IdSubEventoNavigation = null!;
                _context.Participacaopessoasubeventos.Update(participacao);
                RecalcularFrequenciaSeFinalizado(participacao.IdSubEvento, participacao.IdPessoa);
                _context.SaveChanges();
            }
        }

        public void Delete(uint id)
        {
            var entity = _context.Participacaopessoasubeventos.Find(id);
            if (entity != null)
            {
                _context.Participacaopessoasubeventos.Remove(entity);
                RecalcularFrequenciaSeFinalizado(entity.IdSubEvento, entity.IdPessoa);
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
