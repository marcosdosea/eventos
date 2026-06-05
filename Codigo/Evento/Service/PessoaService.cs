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
            throw new Exception($"Erro ao salvar pessoa no banco de dados: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro inesperado ao criar pessoa: {ex.Message}", ex);
        }
    }

    public void Edit(Pessoa pessoa)
    {
        _context.Update(pessoa);
        _context.SaveChanges();
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

    public Pessoa Get(uint id)
    {
        return _context.Pessoas.Find(id);
    }

    public IEnumerable<Pessoa> GetAll()
    {
        return _context.Pessoas.AsNoTracking();
    }

    public async Task<List<Pessoa>> GetAllAdmAsync()
    {
        var listaPessoa = GetAll();
        var listaAdministradores = new List<Pessoa>();

        if (listaPessoa == null)
            return listaAdministradores;

        foreach (var pessoa in listaPessoa)
        {
            if (pessoa == null || string.IsNullOrWhiteSpace(pessoa.Cpf))
                continue;

            var userAdm = await _userManager.FindByNameAsync(pessoa.Cpf);
            if (userAdm == null)
                continue;

            var isAdmin = await _userManager.IsInRoleAsync(userAdm, "ADMINISTRADOR");
            if (isAdmin)
                listaAdministradores.Add(pessoa);
        }

        return listaAdministradores;
    }

    public Pessoa GetByCpf(string cpf)
    {
        var query = from pessoa in _context.Pessoas
                    where pessoa.Cpf == cpf
                    select pessoa;

        return query.SingleOrDefault();
    }

    public async Task<List<Pessoa>> GetPessoasPorPapelNoEventoAsync(uint idEvento, int idPapel)
    {
        var pessoas = await (from inscricao in _context.Inscricaopessoaeventos
                             join pessoa in _context.Pessoas on inscricao.IdPessoa equals pessoa.Id
                             where inscricao.IdEvento == idEvento && inscricao.IdPapel == idPapel
                             select pessoa).AsNoTracking().ToListAsync();

        return pessoas;
    }

    public async Task<UsuarioIdentity> CreateAsync(Pessoa pessoa)
    {
        var newUser = new UsuarioIdentity
        {
            UserName = pessoa.Cpf,
            NormalizedUserName = pessoa.Cpf.Replace(".", "").Replace("-", "").ToUpper(),
            Email = pessoa.Email,
            PhoneNumber = pessoa.Telefone1,
            EmailConfirmed = true
        };

        string defaultPassword = "TempPassword!2024";

        var result = await _userManager.CreateAsync(newUser, defaultPassword);

        if (result.Succeeded)
        {
            return newUser;
        }
        else
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new Exception($"Falha ao criar o usuário Identity para CPF {pessoa.Cpf}: {errors}");
        }
    }

    public async Task CreatePessoaPapelAsync(Pessoa pessoa, uint idEvento, int idPapel)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                uint idPessoa = pessoa.Id;
                var pessoaExistente = GetByCpf(pessoa.Cpf);

                if (pessoaExistente == null)
                {
                    idPessoa = Create(pessoa);
                }
                else
                {
                    idPessoa = pessoaExistente.Id;
                }

                var existingUser = await _userManager.FindByNameAsync(pessoa.Cpf);
                if (existingUser == null)
                {
                    existingUser = await CreateAsync(pessoa);
                }

                if (idPapel != 1)
                {
                    var novaInscricao = new Inscricaopessoaevento
                    {
                        IdPessoa = idPessoa,
                        IdEvento = idEvento,
                        IdPapel = idPapel,
                        DataInscricao = DateTime.Now,
                        Status = "S"
                    };
                    _inscricaoService.CreateInscricaoEvento(novaInscricao);
                }

                string role = idPapel switch
                {
                    1 => "ADMINISTRADOR",
                    2 => "GESTOR",
                    3 => "COLABORADOR",
                    4 => "PARTICIPANTE",
                    _ => throw new ArgumentException("Papel inválido.")
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
}