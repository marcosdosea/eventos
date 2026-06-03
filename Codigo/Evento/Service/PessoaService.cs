using Core;
using Core.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;

namespace Service;

public class PessoaService : IPessoaService
{
    private readonly EventoContext _context;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly IInscricaoService _inscricaoService;

    public PessoaService(UserManager<UsuarioIdentity> userManager, EventoContext context, IInscricaoService inscricaoService)
    {
        _userManager = userManager;
        _context = context;
        _inscricaoService = inscricaoService;
    }

    public uint Create(Pessoa pessoa)
    {
        try
        {
            _context.Add(pessoa);
            _context.SaveChanges();
            return pessoa.Id;
        }
        catch (DbUpdateException ex)
        {
            throw new Exception($"Erro ao salvar pessoa: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    public async Task Edit(Pessoa pessoa)
    {
        try
        {
            var local = _context.Set<Pessoa>().Local.FirstOrDefault(p => p.Id == pessoa.Id);
            if (local != null)
                _context.Entry(local).State = EntityState.Detached;

            _context.Update(pessoa);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            Trace.TraceError($"Erro ao atualizar pessoa: {dbEx.InnerException?.Message ?? dbEx.Message}");
            throw new Exception("Erro no banco de dados ao atualizar pessoa.", dbEx);
        }
    }

    public void Delete(uint id)
    {
        var pessoa = _context.Pessoas.Find(id);
        if (pessoa != null)
        {
            _context.Remove(pessoa);
            _context.SaveChanges();
        }
    }

    public Pessoa Get(uint id) => _context.Pessoas.Find(id);

    public IEnumerable<Pessoa> GetAll() => _context.Pessoas.AsNoTracking();

    public Pessoa GetByCpf(string cpf)
        => _context.Pessoas.SingleOrDefault(p => p.Cpf == cpf);

    public async Task<UsuarioIdentity> CreateAsync(Pessoa pessoa)
    {
        var novoUsuario = new UsuarioIdentity
        {
            UserName = pessoa.Cpf,
            NormalizedUserName = pessoa.Cpf.Replace(".", "").Replace("-", "").ToUpper(),
            Email = pessoa.Email,
            PhoneNumber = pessoa.Telefone1,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(novoUsuario, "Temp@1234!");

        if (!result.Succeeded)
        {
            var erros = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new Exception($"Falha ao criar usuário Identity para CPF {pessoa.Cpf}: {erros}");
        }

        return novoUsuario;
    }

    public async Task CreatePessoaIdentityComPapelAsync(Pessoa pessoa, int idPapel)
    {
        await using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                uint idPessoa = pessoa.Id;
                if (GetByCpf(pessoa.Cpf) == null)
                {
                    idPessoa = Create(pessoa);
                }

                var existingUser = await _userManager.FindByNameAsync(pessoa.Cpf);
                if (existingUser == null)
                {
                    existingUser = await CreateAsync(pessoa);
                }

                if (idPapel != 1)
                {
                    uint idEventoVinculado = pessoa.Inscricaopessoaeventos.FirstOrDefault()?.IdEvento ?? 0;

                    if (idEventoVinculado > 0)
                    {
                        var novaInscricao = new Inscricaopessoaevento
                        {
                            IdPessoa = idPessoa,
                            IdEvento = idEventoVinculado,
                            IdPapel = idPapel,
                            DataInscricao = DateTime.Now,
                            Status = "S"
                        };
                        _inscricaoService.CreateInscricaoEvento(novaInscricao);
                    }
                }

                string role = idPapel switch
                {
                    1 => "ADMINISTRADOR",
                    2 => "GESTOR",
                    3 => "COLABORADOR",
                    4 => "USUARIO",
                    _ => throw new ArgumentException($"Papel inválido: {idPapel}")
                };

                var isInRole = await _userManager.IsInRoleAsync(existingUser, role);
                if (!isInRole)
                {
                    var roleResult = await _userManager.AddToRoleAsync(existingUser, role);
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                        throw new Exception($"Erro ao associar o papel '{role.ToLower()}' ao usuário no Identity: {errors}");
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Erro ao criar pessoa, inscrição ou associar papel: {ex.Message}", ex);
            }
        }
    }

    public async Task<List<Pessoa>> GetAllAdmAsync()
    {
        var admins = new List<Pessoa>();
        foreach (var pessoa in GetAll())
        {
            if (string.IsNullOrWhiteSpace(pessoa.Cpf)) continue;
            var user = await _userManager.FindByNameAsync(pessoa.Cpf);
            if (user != null && await _userManager.IsInRoleAsync(user, "ADMINISTRADOR"))
                admins.Add(pessoa);
        }
        return admins;
    }

    public async Task<List<Pessoa>> GetPessoasPorPapelNoEventoAsync(uint idEvento, int idPapel)
    {
        return await _context.Inscricaopessoaeventos
            .Where(i => i.IdEvento == idEvento && i.IdPapel == idPapel)
            .Select(i => i.IdPessoaNavigation)
            .AsNoTracking()
            .ToListAsync();
    }
}