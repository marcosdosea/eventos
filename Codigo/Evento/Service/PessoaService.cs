using System.Text.RegularExpressions;
using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Service;

public class PessoaService : IPessoaService
{
    /// <summary>
    /// Manter dados de pessoa no banco de dados
    /// </summary>
    private readonly EventoContext _context;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly IInscricaoService _inscricaoService;

    public PessoaService(UserManager<UsuarioIdentity> userManager, EventoContext context,IInscricaoService inscricaoService)
    {
        _userManager = userManager;
        _context = context;
        _inscricaoService = inscricaoService;
    }
    /// <summary>
    /// Insere uma nova pessoa na base de dados
    /// </summary>
    /// <param name="pessoa">dados de pessoa</param>
    /// <returns></returns>
    public uint Create(Pessoa pessoa)
    {
        _context.Add(pessoa);
        _context.SaveChanges();
        return pessoa.Id;
    }
    
    /// <summary>
    /// Edita uma pessoa na base de dados
    /// </summary>
    /// <param name="pessoa">dados de pessoa</param>
    /// <returns></returns>
    public void Edit(Pessoa pessoa)
    {
        _context.Update(pessoa);
        _context.SaveChanges();
    }
    /// <summary>
    /// Exclui uma pessoa na base de dados
    /// </summary>
    /// <param name="id">dados de pessoa</param>
    /// <returns></returns>
    public void Delete(uint id)
    {
        var pessoa = _context.Pessoas.Find(id);
        _context.Remove(pessoa);
        _context.SaveChanges();
    }
    /// <summary>
    /// Obtém uma pessoa específica por id
    /// </summary>
    /// <param name="id">dados de pessoa</param>
    /// <returns></returns>
    public Pessoa Get(uint id)
    {
        return _context.Pessoas.Find(id);
    }
    /// <summary>
    /// Obtém todas pessoas
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Pessoa> GetAll()
    {
        return _context.Pessoas.AsNoTracking();
    }
    
    /// <summary>
    /// Obtém uma pessoa específica por cpf
    /// </summary>
    /// <param name="cpf">dados de pessoa</param>
    /// <returns></returns>
    public Pessoa GetByCpf(string cpf)
    {
        var query = from pessoa in _context.Pessoas
            where pessoa.Cpf == cpf
            select pessoa;

        return query.SingleOrDefault(); 
    }

    public async Task<UsuarioIdentity> CreateAsync(Pessoa pessoa)
    {
        var newUser = new UsuarioIdentity
        {
            UserName = pessoa.Cpf,
            NormalizedUserName = pessoa.Cpf.Replace(".", "").Replace("-", ""),
            Email = pessoa.Email,
            PhoneNumber = pessoa.Telefone1
        };

        var result = await _userManager.CreateAsync(newUser,pessoa.Cpf);

        if (result.Succeeded)
        {
            return newUser;
        }
        else
        {
            throw new Exception("Falha ao criar o usuário.");
        }
    }

    public async Task CreatePessoaPapelAsync(Pessoa pessoa, uint idEvento, int idPapel)
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

        var novaInscricao = new Inscricaopessoaevento
        {
            IdPessoa = idPessoa,
            IdEvento = idEvento,
            IdPapel = idPapel,
            DataInscricao = DateTime.Now,
            Status = "S"
        };
        _inscricaoService.CreateInscricaoEvento(novaInscricao);

        string role = idPapel switch
        {
            2 => "GESTOR",
            3 => "COLABORADOR",
            4 => "USUARIO",
            _ => throw new ArgumentException("Papel inválido.")
        };

        var isInRole = await _userManager.IsInRoleAsync(existingUser, role);
        if (!isInRole)
        {
            var roleResult = await _userManager.AddToRoleAsync(existingUser, role);
            if (!roleResult.Succeeded)
            {
                throw new Exception("Erro ao associar o papel ao usuário no Identity.");
            }
        }
    }
    
}


