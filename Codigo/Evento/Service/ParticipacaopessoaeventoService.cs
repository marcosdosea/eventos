using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Microsoft.EntityFrameworkCore;

namespace Core.Service
{
    public class ParticipacaoPessoaEventoService : IParticipacaoPessoaEventoService
    {
        private readonly EventoContext _context;

        public ParticipacaoPessoaEventoService(EventoContext context)
        {
            _context = context;
        }

        public async Task<List<Participacaopessoaevento>> GetAllAsync()
        {
            return await _context.Participacaopessoaeventos.ToListAsync();
        }

        public async Task<Participacaopessoaevento> GetByIdAsync(uint id)
        {
            return await _context.Participacaopessoaeventos.FindAsync(id);
        }

        public async Task<Participacaopessoaevento> AddAsync(Participacaopessoaevento entity)
        {
            var result = (await _context.Participacaopessoaeventos.AddAsync(entity)).Entity;
            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<bool> UpdateAsync(Participacaopessoaevento entity)
        {
            var existing = await _context.Participacaopessoaeventos.FindAsync(entity.Id);
            if (existing == null)
                return false;

            existing.IdPessoa = entity.IdPessoa;
            existing.IdEvento = entity.IdEvento;
            existing.Entrada = entity.Entrada;
            existing.Saida = entity.Saida;

            _context.Participacaopessoaeventos.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(uint id)
        {
            var existing = await _context.Participacaopessoaeventos.FindAsync(id);
            if (existing == null)
                return false;

            _context.Participacaopessoaeventos.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
