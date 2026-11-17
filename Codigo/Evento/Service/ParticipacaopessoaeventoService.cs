using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Microsoft.EntityFrameworkCore;
using Service;

namespace Core.Service
{
    public class ParticipacaoPessoaEventoService : IParticipacaoPessoaEventoService
    {
        private readonly EventoContext _context;
        private readonly IEventoService _eventoService;

        public ParticipacaoPessoaEventoService(EventoContext context, IEventoService? eventoService = null)
        {
            _context = context;
            _eventoService = eventoService ?? new EventoService(context);
        }

        private void RecalcularFrequenciaSeFinalizado(uint idEvento, uint idPessoa)
        {
            var evento = _context.Eventos.Find(idEvento);
            if (evento?.Status == "F")
            {
                _eventoService.AtualizarFrequenciaFinal(idEvento, idPessoa, saveChanges: false);
            }
        }

        public async Task<List<Participacaopessoaevento>> GetAllAsync()
        {
            return await _context.Participacaopessoaeventos
                .Include(p => p.IdPessoaNavigation)
                .ToListAsync();
        }

        public async Task<List<Participacaopessoaevento>> GetByEventoAsync(uint idEvento)
        {
            return await _context.Participacaopessoaeventos
                .Include(p => p.IdPessoaNavigation)
                .Where(p => p.IdEvento == idEvento)
                .OrderByDescending(p => p.Entrada)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Participacaopessoaevento> GetByIdAsync(uint id)
        {
            return await _context.Participacaopessoaeventos.FindAsync(id);
        }

        public async Task<Participacaopessoaevento> AddAsync(Participacaopessoaevento entity)
        {
            var result = (await _context.Participacaopessoaeventos.AddAsync(entity)).Entity;
            RecalcularFrequenciaSeFinalizado(entity.IdEvento, entity.IdPessoa);
            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<bool> UpdateAsync(Participacaopessoaevento entity)
        {
            var existing = await _context.Participacaopessoaeventos.FindAsync(entity.Id);
            if (existing == null)
                return false;

            var eventoAnterior = _context.Entry(existing).Property(p => p.IdEvento).OriginalValue;
            var pessoaAnterior = _context.Entry(existing).Property(p => p.IdPessoa).OriginalValue;
            existing.IdPessoa = entity.IdPessoa;
            existing.IdEvento = entity.IdEvento;
            existing.Entrada = entity.Entrada;
            existing.Saida = entity.Saida;

            _context.Participacaopessoaeventos.Update(existing);
            RecalcularFrequenciaSeFinalizado(eventoAnterior, pessoaAnterior);
            if (eventoAnterior != existing.IdEvento || pessoaAnterior != existing.IdPessoa)
                RecalcularFrequenciaSeFinalizado(existing.IdEvento, existing.IdPessoa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(uint id)
        {
            var existing = await _context.Participacaopessoaeventos.FindAsync(id);
            if (existing == null)
                return false;

            _context.Participacaopessoaeventos.Remove(existing);
            RecalcularFrequenciaSeFinalizado(existing.IdEvento, existing.IdPessoa);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
