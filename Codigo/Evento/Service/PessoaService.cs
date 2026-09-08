using Core;
using Core.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Net.Mail;
using System.Text.RegularExpressions;
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
    public async Task<bool> VerificaEdit(Pessoa pessoaAtualizada)
    {
        if (pessoaAtualizada == null) return false;
        var pessoaAtual = GetByCpf(pessoaAtualizada.Cpf);
        
        if(pessoaAtual != null) {

            

            if(pessoaAtual.Nome != pessoaAtualizada.Nome || pessoaAtual.Email != pessoaAtualizada.Email || pessoaAtual.Telefone1 != pessoaAtualizada.Telefone1 || pessoaAtual.Telefone2 != pessoaAtualizada.Telefone2)
            {
                pessoaAtualizada.Id = pessoaAtual.Id;
                pessoaAtualizada.Sexo = pessoaAtual.Sexo;

                try
                {
                    await Edit(pessoaAtualizada);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
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

    public async Task<bool> Delete(uint id)
    {
        try
        {
            var pessoa = _context.Pessoas.Find(id);
            

            if (pessoa != null)
            {
                var existingUser = await _userManager.FindByNameAsync(pessoa.Cpf);
                if (existingUser != null)
                {
                    if (await _userManager.IsInRoleAsync(existingUser, "ADMINISTRADOR"))
                    {
                        var administradores = await GetAllAdmAsync();

                        if (administradores.Count() == 1 && administradores.Any(a => a.Id == id))
                        {
                            return false;

                        }

                        await _userManager.RemoveFromRoleAsync(existingUser, "ADMINISTRADOR");
                        var roles = await _userManager.GetRolesAsync(existingUser);
                        if (roles.Count == 0)
                        {
                            return await CreatePessoaIdentityComPapelAsync(pessoa, 0,4);
                        }
                        return true;
                    }
                }

                _context.Remove(pessoa);
                _context.SaveChanges();
                return true;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Erro ao deletar pessoa com ID {id}: {ex.Message}");

            return false;
        }


        return false;
    }

    /// <summary>
    /// Obtém todas as pessoas que possuem o papel de "GESTOR" no sistema Identity.
    /// </summary>
    /// <returns>listaGestores</returns>
    public async Task<List<Pessoa>> GetAllGestorAsync()
    {
        var listaPessoa = GetAll();
        var listaGestores = new List<Pessoa>();

        if (listaPessoa == null)
            return listaGestores;

        foreach (var pessoa in listaPessoa)
        {
            if (pessoa == null || string.IsNullOrWhiteSpace(pessoa.Cpf))
                continue;

            var userGestor = await _userManager.FindByNameAsync(pessoa.Cpf);
            if (userGestor == null)
                continue;

            var isGestor = await _userManager.IsInRoleAsync(userGestor, "GESTOR");
            if (isGestor)
                listaGestores.Add(pessoa);
        }

        return listaGestores;
    }

    /// <summary>
    /// Obtém uma pessoa específica por cpf
    /// </summary>
    /// <param name="cpf">dados de pessoa</param>
    /// <returns></returns>
    public Pessoa Get(uint id) => _context.Pessoas.Find(id);

    public IEnumerable<Pessoa> GetAll() => _context.Pessoas.AsNoTracking();

    public Pessoa GetByCpf(string cpf)
        => _context.Pessoas.SingleOrDefault(p => p.Cpf == cpf);

    public async Task<bool>IsAdmAsync(Pessoa pessoa)
    {
        var user = await _userManager.FindByNameAsync(pessoa.Cpf);

        if (user == null)
            return false;

        return await _userManager.IsInRoleAsync(user, "ADMINISTRADOR");
    }
    public bool ValidaEmail(String email)
    {
        var valida = new EmailAddressAttribute();
        var endereco = new MailAddress(email);
        String dominio = endereco.Host;
        if(!valida.IsValid(email)) return false;
        if(Regex.IsMatch(dominio, @"\.\p{L}") == false) return false;
      
        return true;
    }
    public async Task<bool> EmailExist(String email, String cpf)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;
        if (cpf != user.UserName)
        {
            return true;
        }
        return false;
    }
    public bool EmailConfirmado(string email)
    {
        var user = _userManager.FindByEmailAsync(email).Result;
        if (user == null)
            return false;
        var isConfirmed = _userManager.IsEmailConfirmedAsync(user).Result;
        return isConfirmed;
    }
    public async Task<string> GerarTokenAsync(String cpf)
    {
        String token = "";
        var user = await _userManager.FindByNameAsync(cpf);

        if (user == null)
            return token;
        token = await _userManager.GeneratePasswordResetTokenAsync(user);

        return token;



    }
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

    public async Task<bool> CreatePessoaIdentityComPapelAsync(Pessoa pessoa, uint idEvento, int idPapel)
    {
        if (pessoa == null)
            throw new ArgumentNullException(nameof(pessoa));

        // Fonte única de verdade para papéis; lança ArgumentException se inválido.
        var role = PapelMap.ToRole(idPapel);

        // ensure-Pessoa: reaproveita cadastro existente ou prepara novo para a transação.
        var existente = GetByCpf(pessoa.Cpf);
        if (existente != null)
        {
            pessoa = existente;
        }

        // ensure-Identity.
        var existingUser = await _userManager.FindByNameAsync(pessoa.Cpf);
        if (existingUser == null)
        {
            existingUser = await CreateAsync(pessoa);
        }

        var isRelational = _context.Database.IsRelational();

        if (isRelational)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (GetByCpf(pessoa.Cpf) == null)
                {
                    _context.Add(pessoa);
                    await _context.SaveChangesAsync();
                }

                if (idEvento > 0)
                {
                    var jaInscrito = await _context.Inscricaopessoaeventos
                        .AnyAsync(i => i.IdPessoa == pessoa.Id && i.IdEvento == idEvento && i.IdPapel == idPapel);
                    if (!jaInscrito)
                    {
                        _context.Inscricaopessoaeventos.Add(new Inscricaopessoaevento
                        {
                            IdPessoa = pessoa.Id,
                            IdEvento = idEvento,
                            IdPapel = idPapel,
                            NomeCracha = !string.IsNullOrWhiteSpace(pessoa.NomeCracha)
                                ? pessoa.NomeCracha
                                : (pessoa.Nome?.Length > 20 ? pessoa.Nome.Substring(0, 20) : pessoa.Nome ?? "Participante"),
                            DataInscricao = DateTime.UtcNow,
                            Status = "S"
                        });
                    }
                }

                if (!await _userManager.IsInRoleAsync(existingUser, role))
                {
                    var roleResult = await _userManager.AddToRoleAsync(existingUser, role);
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                        throw new Exception($"Erro ao associar o papel '{role.ToLower()}' ao usuário no Identity: {errors}");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Provedor não-relacional (ex.: InMemory em testes): sem transação.
        if (GetByCpf(pessoa.Cpf) == null)
        {
            _context.Add(pessoa);
            await _context.SaveChangesAsync();
        }

        if (idEvento > 0)
        {
            var jaInscrito = await _context.Inscricaopessoaeventos
                .AnyAsync(i => i.IdPessoa == pessoa.Id && i.IdEvento == idEvento && i.IdPapel == idPapel);
            if (!jaInscrito)
            {
                _context.Inscricaopessoaeventos.Add(new Inscricaopessoaevento
                {
                    IdPessoa = pessoa.Id,
                    IdEvento = idEvento,
                    IdPapel = idPapel,
                    NomeCracha = !string.IsNullOrWhiteSpace(pessoa.NomeCracha) ? pessoa.NomeCracha : pessoa.Nome,
                    DataInscricao = DateTime.UtcNow,
                    Status = "S"
                });
            }
        }

        if (!await _userManager.IsInRoleAsync(existingUser, role))
        {
            var roleResult = await _userManager.AddToRoleAsync(existingUser, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new Exception($"Erro ao associar o papel '{role.ToLower()}' ao usuário no Identity: {errors}");
            }
        }

        await _context.SaveChangesAsync();
        return true;
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