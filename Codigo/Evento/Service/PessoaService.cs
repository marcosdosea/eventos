using Core;
using Core.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Service;

/// <summary>
/// Gerencia dados de Pessoa e seu vínculo com o ASP.NET Identity.
/// Colaborador e Participante NÃO são entidades separadas: são papéis
/// atribuídos via Inscricaopessoaevento + roles do Identity.
/// </summary>
public class PessoaService : IPessoaService
{
    private readonly EventoContext _context;
    private readonly UserManager<UsuarioIdentity> _userManager;

    public PessoaService(UserManager<UsuarioIdentity> userManager, EventoContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // =========================================================================
    // CRUD BÁSICO
    // =========================================================================

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

    // =========================================================================
    // IDENTITY
    // =========================================================================

    /// <summary>
    /// Cria o usuário Identity para a pessoa com senha temporária.
    /// </summary>
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

        // ATENÇÃO: senha temporária — implemente fluxo de redefinição no primeiro login.
        var result = await _userManager.CreateAsync(novoUsuario, "Temp@1234!");

        if (!result.Succeeded)
        {
            var erros = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new Exception($"Falha ao criar usuário Identity para CPF {pessoa.Cpf}: {erros}");
        }

        return novoUsuario;
    }

    /// <summary>
    /// Garante que a Pessoa existe no banco e no Identity, e atribui a role
    /// correspondente ao idPapel. A inscrição no evento é feita separadamente
    /// via IInscricaoService.AtribuirPapelNoEventoAsync.
    /// </summary>
    public async Task CreatePessoaIdentityComPapelAsync(Pessoa pessoa, int idPapel)
    {
        // 1. Garante Pessoa no banco
        if (GetByCpf(pessoa.Cpf) == null)
            Create(pessoa);

        // 2. Garante usuário Identity
        var usuarioExistente = await _userManager.FindByNameAsync(pessoa.Cpf);
        if (usuarioExistente == null)
            usuarioExistente = await CreateAsync(pessoa);

        // 3. Atribui role no Identity
        string role = idPapel switch
        {
            1 => "ADMINISTRADOR",
            2 => "GESTOR",
            3 => "COLABORADOR",
            4 => "USUARIO",
            _ => throw new ArgumentException($"Papel inválido: {idPapel}")
        };

        if (!await _userManager.IsInRoleAsync(usuarioExistente, role))
        {
            var roleResult = await _userManager.AddToRoleAsync(usuarioExistente, role);
            if (!roleResult.Succeeded)
            {
                var erros = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new Exception($"Erro ao atribuir role '{role}': {erros}");
            }
        }
    }

    // =========================================================================
    // CONSULTAS ESPECÍFICAS
    // =========================================================================

    /// <summary>
    /// Retorna todas as pessoas com role ADMINISTRADOR no Identity.
    /// </summary>
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

    /// <summary>
    /// Retorna todas as pessoas inscritas em um evento com um papel específico.
    /// </summary>
    public async Task<List<Pessoa>> GetPessoasPorPapelNoEventoAsync(uint idEvento, int idPapel)
    {
        return await _context.Inscricaopessoaeventos
            .Where(i => i.IdEvento == idEvento && i.IdPapel == idPapel)
            .Select(i => i.IdPessoaNavigation)
            .AsNoTracking()
            .ToListAsync();
    }
}
