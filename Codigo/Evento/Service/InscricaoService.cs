using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using ZstdSharp.Unsafe;

namespace Service
{
    public class InscricaoService : IInscricaoService
    {
        private readonly EventoContext _context;
        
        public InscricaoService(EventoContext context) 
        {
            _context = context;
        }

        public void CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento)
        {
            _context.Add(inscricaopessoaevento);
            _context.SaveChanges();
        }

        public void DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel)
        {
            var inscricao = _context.Inscricaopessoaeventos.FirstOrDefault(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento && i.IdPapel == idPapel);
            if (inscricao != null)
            {
                _context.Inscricaopessoaeventos.Remove(inscricao);
                _context.SaveChanges();
            }
        }
        
        public IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel)
        {
            return _context.Inscricaopessoaeventos
                .Include(i => i.IdPessoaNavigation)
                .Where(i => i.IdEvento == idEvento && i.IdPapel == idPapel)
                .AsNoTracking()
                .ToList();
        }
        
        public void RegistrarFrequenciaEvento(Inscricaopessoaevento inscricaopessoaevento, decimal frequencia)
        {
            inscricaopessoaevento.FrequenciaFinal = frequencia;
            _context.Update(inscricaopessoaevento);
            _context.SaveChanges();
        }

        public void RegistrarFrequenciaSubevento(Inscricaopessoasubevento inscricaopessoasubevento, decimal frequencia)
        {
            inscricaopessoasubevento.FrequenciaFinal = frequencia;
            _context.Update(inscricaopessoasubevento);
            _context.SaveChanges();
        }
        
        public Inscricaopessoaevento GetInscricaoByEvento(uint idEvento, uint idPessoa)
        {
            return _context.Inscricaopessoaeventos.Find(idPessoa);
        }

        public Inscricaopessoasubevento GetInscricaoBySubEvento(uint idEvento, uint idPessoa)
        {
            return _context.Inscricaopessoasubeventos.Find(idPessoa);
        }
    }
}