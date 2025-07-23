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

    public PessoaService(UserManager<UsuarioIdentity> userManager, EventoContext context, IInscricaoService inscricaoService)
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
        try
        {
            _context.Add(pessoa);
            _context.SaveChanges();
            return pessoa.Id;
        }
        catch (DbUpdateException ex)
        {
            // Captura exceções relacionadas a problemas de atualização/inserção no banco de dados
            // e lança uma nova exceção com a mensagem interna para melhor diagnóstico.
            throw new Exception($"Erro ao salvar pessoa no banco de dados: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Captura outras exceções gerais
            throw new Exception($"Erro inesperado ao criar pessoa: {ex.Message}", ex);
        }
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

    /// <summary>
    /// Cria um novo usuário Identity para a pessoa.
    /// </summary>
    /// <param name="pessoa">Os dados da pessoa para criar o usuário Identity.</param>
    /// <returns>O UsuarioIdentity criado.</returns>
    /// <exception cref="Exception">Lançada se a criação do usuário Identity falhar.</exception>
    public async Task<UsuarioIdentity> CreateAsync(Pessoa pessoa)
    {
        var newUser = new UsuarioIdentity
        {
            UserName = pessoa.Cpf,
            NormalizedUserName = pessoa.Cpf.Replace(".", "").Replace("-", "").ToUpper(), // Normalizar para garantir unicidade e consistência
            Email = pessoa.Email,
            PhoneNumber = pessoa.Telefone1,
            EmailConfirmed = true // Assumindo que o e-mail é confirmado na criação para colaboradores
        };

        // Gerar uma senha padrão forte. É CRÍTICO que esta senha seja temporária
        // e que o usuário seja forçado a alterá-la no primeiro login.
        // Adapte a senha para atender às políticas de senha do seu Identity.
        // Exemplo: "TempPass!23" ou gerar uma GUID.
        string defaultPassword = "TempPassword!2024"; // <--- ALTERE ESTA SENHA PARA ALGO SEGURO E TEMPORÁRIO!

        var result = await _userManager.CreateAsync(newUser, defaultPassword);

        if (result.Succeeded)
        {
            return newUser;
        }
        else
        {
            // Agrega as mensagens de erro do Identity para um diagnóstico mais preciso
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new Exception($"Falha ao criar o usuário Identity para CPF {pessoa.Cpf}: {errors}");
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
                // Melhorar a mensagem de erro para incluir detalhes do Identity
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new Exception($"Erro ao associar o papel '{role}' ao usuário no Identity: {errors}");
            }
        }
    }
}
