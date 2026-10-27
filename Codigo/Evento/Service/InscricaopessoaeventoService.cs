using Core;
using Core.Service;
using System.Collections.Generic;
using System.Linq;

namespace Service
{
    public class InscricaopessoaeventoService : IInscricaopessoaeventoService
    {
        private readonly EventoContext _context;
        private readonly IEventoService _eventoService;
        private readonly ISubeventoService _subeventoService;

        public InscricaopessoaeventoService(EventoContext context, IEventoService eventoService, ISubeventoService subeventoService)
        {
            _context = context;
            _eventoService = eventoService;
            _subeventoService = subeventoService;
        }

        public void Create(Inscricaopessoaevento inscricao)
        {
            if (PessoaJaInscrita(inscricao.IdPessoa, inscricao.IdEvento))
                throw new System.Exception("Esta pessoa já está inscrita neste evento.");

            inscricao.DataInscricao = System.DateTime.Now;
            _context.Inscricaopessoaeventos.Add(inscricao);
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
            if (inscricao == null)
                return;

            var idEvento = inscricao.IdEvento;
            var idPessoa = inscricao.IdPessoa;

            var pagamentos = _context.Pagamentos
                .Where(p => p.IdInscricaoPessoaEvento == id)
                .ToList();
            if (pagamentos.Count > 0)
            {
                _context.Pagamentos.RemoveRange(pagamentos);
            }

            var idsSubeventosDoEvento = _context.Subeventos
                .Where(s => s.IdEvento == idEvento)
                .Select(s => s.Id)
                .ToList();

            var inscricoesSub = idsSubeventosDoEvento.Count > 0
                ? _context.Inscricaopessoasubeventos
                    .Where(s => s.IdPessoa == idPessoa && idsSubeventosDoEvento.Contains(s.IdSubEvento))
                    .ToList()
                : new List<Inscricaopessoasubevento>();

            var idsSubAfetados = inscricoesSub.Select(s => s.IdSubEvento).Distinct().ToList();

            if (inscricoesSub.Count > 0)
            {
                _context.Inscricaopessoasubeventos.RemoveRange(inscricoesSub);
            }

            _context.Inscricaopessoaeventos.Remove(inscricao);

            _eventoService.AtualizarVagasDisponiveis(idEvento, saveChanges: false);
            foreach (var idSub in idsSubAfetados)
            {
                _subeventoService.AtualizarVagasDisponiveis(idSub, saveChanges: false);
            }

            _context.SaveChanges();
        }

        public bool PessoaJaInscrita(uint idPessoa, uint idEvento)
        {
            return _context.Inscricaopessoaeventos.Any(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento);
        }
    }
}
