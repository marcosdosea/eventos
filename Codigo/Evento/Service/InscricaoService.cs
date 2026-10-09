using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Util;

namespace Service
{
    public class InscricaoService : IInscricaoService
    {
        private readonly EventoContext _context;
        private readonly UserManager<UsuarioIdentity> _userManager;
        public InscricaoService(EventoContext context, UserManager<UsuarioIdentity> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        public bool IsInscrito(uint idPessoa, uint idEvento)
        {
            return _context.Inscricaopessoaeventos
                .Any(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento);
        }

        public uint CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento)
        {
            if (IsInscrito(inscricaopessoaevento.IdPessoa, inscricaopessoaevento.IdEvento))
            {
                return 0;
            }

            _context.Add(inscricaopessoaevento);
            _context.SaveChanges();
            return inscricaopessoaevento.Id;
        }

        public uint CreateInscricaoEventoLote(Inscricaopessoaevento inscricaopessoaevento)
        {
            _context.Add(inscricaopessoaevento);
            _context.SaveChanges();
            return inscricaopessoaevento.Id;
        }

        public void CreateInscricoesEmLote(IEnumerable<Inscricaopessoaevento> eventos, IEnumerable<Inscricaopessoasubevento> subeventos, bool saveChanges = true)
        {
            if (eventos != null && eventos.Any())
            {
                _context.AddRange(eventos);
            }
            if (subeventos != null && subeventos.Any())
            {
                _context.AddRange(subeventos);
            }
            
            if (saveChanges && ((eventos != null && eventos.Any()) || (subeventos != null && subeventos.Any())))
            {
                _context.SaveChanges();
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public async Task DeletePessoaPapelAsync(uint idPessoa, uint idEvento, uint idPapel, string cpf)
        {
            var pessoa = await _context.Pessoas.FirstOrDefaultAsync(p => p.Id == idPessoa && p.Cpf == cpf);
            if (pessoa == null)
            {
                throw new Exception("Pessoa não encontrada com o CPF informado.");
            }

            var inscricao = await _context.Inscricaopessoaeventos
                .FirstOrDefaultAsync(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento && i.IdPapel == idPapel);

            if (inscricao != null)
            {
                _context.Inscricaopessoaeventos.Remove(inscricao);
                await _context.SaveChangesAsync();
            }

            var existePapelUsuario = await _context.Inscricaopessoaeventos
                .AnyAsync(i => i.IdPessoa == idPessoa && i.IdPapel == idPapel);

            if (!existePapelUsuario && idPapel != 4)
            {
                RemoveUserRole(idPessoa, idPapel, cpf).GetAwaiter().GetResult();
            }
        }

        public async Task RemoveUserRole(uint idPessoa, uint idPapel, string cpf)
        {
            string cpfSemFormatacao = Methods.RemoveNaoNumericos(cpf);
            var user = await _userManager.FindByNameAsync(cpfSemFormatacao);

            if (user == null)
            {
                throw new Exception("Usuário não encontrado.");
            }

            string role = idPapel switch
            {
                1 => "GESTOR",
                2 => "GESTOR",
                3 => "COLABORADOR",
                _ => throw new ArgumentException("Papel inválido.")
            };

            if (await _userManager.IsInRoleAsync(user, role))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, role);

                if (!removeResult.Succeeded)
                {
                    throw new Exception("Erro ao remover o papel do usuário.");
                }
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


        public int GetPapelPessoaByEvento(uint idPessoa, uint idEvento)
        {
            return _context.Inscricaopessoaeventos
                .Where(i => i.IdEvento == idEvento && i.IdPessoa == idPessoa)
                .Select(i => i.IdPapel)
                .FirstOrDefault();
        }

        public IEnumerable<Inscricaopessoaevento> GetByEvento(uint idEvento)
        {
            return _context.Inscricaopessoaeventos
                .Include(i => i.IdPessoaNavigation)
                .Where(i => i.IdEvento == idEvento)
                .AsNoTracking()
                .ToList();
        }

        public void CreateInscricaoSubEvento(Inscricaopessoasubevento inscricaopessoasubevento)
        {
            _context.Add(inscricaopessoasubevento);
            _context.SaveChanges();
        }

        public IEnumerable<Inscricaopessoasubevento> GetSubByEvento(uint idEvento)
        {
            return _context.Inscricaopessoasubeventos
                .Include(i => i.IdPessoaNavigation)
                .Include(i => i.IdSubEventoNavigation)
                .Where(i => i.IdSubEventoNavigation.IdEvento == idEvento)
                .AsNoTracking()
                .ToList();
        }

        public IEnumerable<Inscricaopessoasubevento> GetAllSubEventsByUserId(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Enumerable.Empty<Inscricaopessoasubevento>();

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var subeventos = _context.Inscricaopessoasubeventos
                .Include(s => s.IdSubEventoNavigation)
                .Include(s => s.IdTipoInscricaoNavigation)
                .Include(s => s.IdPessoaNavigation)
                .Where(s => s.IdPessoaNavigation != null && (s.IdPessoaNavigation.Cpf == cpfParaBusca || s.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && s.IdPessoaNavigation.Cpf == cpfFormatado)))
                .AsNoTracking()
                .ToList();

            foreach (var sub in subeventos)
            {
                if (sub.IdSubEventoNavigation == null)
                {
                    sub.IdSubEventoNavigation = _context.Subeventos.FirstOrDefault(s => s.Id == sub.IdSubEvento)!;
                }
            }

            return subeventos;
        }

        public IEnumerable<Inscricaopessoaevento> GetAllEventsByUserId(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Enumerable.Empty<Inscricaopessoaevento>();

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var subeventosUsuario = GetAllSubEventsByUserId(username).ToList();

            var query = _context.Inscricaopessoaeventos
                .Include(i => i.IdEventoNavigation)
                .Include(i => i.IdPessoaNavigation)
                .Include(i => i.IdTipoInscricaoNavigation)
                .Where(i => i.IdPessoaNavigation != null && (i.IdPessoaNavigation.Cpf == cpfParaBusca || i.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && i.IdPessoaNavigation.Cpf == cpfFormatado)))
                .ToList();

            foreach (var inscricao in query)
            {
                inscricao.Inscricaopessoasubeventos = subeventosUsuario
                    .Where(s => (s.IdSubEventoNavigation?.IdEvento ?? _context.Subeventos.FirstOrDefault(sub => sub.Id == s.IdSubEvento)?.IdEvento) == inscricao.IdEvento)
                    .ToList();
            }

            return query;
        }

        public Inscricaopessoaevento GetGestorInEvent(string username, uint idEvento)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var query = from i in _context.Inscricaopessoaeventos.Include(i => i.IdPessoaNavigation)
                        where i.IdPessoaNavigation != null
                              && (i.IdPessoaNavigation.Cpf == cpfParaBusca || i.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && i.IdPessoaNavigation.Cpf == cpfFormatado))
                              && i.IdPapel == 2 && i.IdEvento == idEvento
                        select i;
            return query.FirstOrDefault();
        }

        public Inscricaopessoaevento GetColaboradorInEvent(string username, uint idEvento)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var query = from i in _context.Inscricaopessoaeventos.Include(i => i.IdPessoaNavigation)
                        where i.IdPessoaNavigation != null
                              && (i.IdPessoaNavigation.Cpf == cpfParaBusca || i.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && i.IdPessoaNavigation.Cpf == cpfFormatado))
                              && i.IdPapel == 3 && i.IdEvento == idEvento
                        select i;
            return query.FirstOrDefault();
        }
    }
}