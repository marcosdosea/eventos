using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

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

        public void DeletePessoaPapel(uint idPessoa, uint idEvento)
        {
            var inscricao = _context.Inscricaopessoaeventos.FirstOrDefault(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento);
            if (inscricao != null)
            {
                _context.Inscricaopessoaeventos.Remove(inscricao);
                _context.SaveChanges();
            }
        }

        

        public IEnumerable<Inscricaopessoaevento> GetAllInscricaoEvento()
        {
            return _context.Inscricaopessoaeventos.AsNoTracking();
        }

        public IEnumerable<Pessoa> GetPessoasByPapel(int idPapel)
        {
            return _context.Inscricaopessoaeventos
                .Where(inscricao => inscricao.IdPapel == idPapel)
                .Join(_context.Pessoas,
                    inscricao => inscricao.IdPessoa,
                    pessoa => pessoa.Id,
                    (inscricao, pessoa) => pessoa)
                .ToList();
        }
    }
}