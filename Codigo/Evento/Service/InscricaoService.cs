using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

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

        public void CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento)
        {
            _context.Add(inscricaopessoaevento);
            _context.SaveChanges();
        }
        
        public void DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel, string cpf)
        {
            var inscricao = _context.Inscricaopessoaeventos
                .FirstOrDefault(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento && i.IdPapel == idPapel);

            if (inscricao != null)
            {
                _context.Inscricaopessoaeventos.Remove(inscricao);
                _context.SaveChanges();
            }

            var existePapelUsuario = _context.Inscricaopessoaeventos
                .Any(i => i.IdPessoa == idPessoa && i.IdPapel == idPapel);

            if (!existePapelUsuario && idPapel != 4)
            {
                RemoveUserRole(idPessoa, idPapel, cpf).GetAwaiter().GetResult();
            }
        }

        public async Task RemoveUserRole(uint idPessoa, uint idPapel,string cpf)
        {
            string cpfSemFormatacao = cpf.Replace(".", "").Replace("-", "");
            var user = await _userManager.FindByNameAsync(cpfSemFormatacao);

            if (user == null)
            {
                throw new Exception("Usuário não encontrado.");
            }

            string role = idPapel switch
            {
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
    }
}