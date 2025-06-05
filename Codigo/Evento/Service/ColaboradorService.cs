using Core;
using Core.DTO;
using Core.Service;
using Microsoft.AspNetCore.Identity;

namespace Service;

public class ColaboradorService : IColaboradorService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly IPessoaService _pessoaService;

    public ColaboradorService(RoleManager<IdentityRole> roleManager, UserManager<UsuarioIdentity> userManager, IPessoaService pessoaService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _pessoaService = pessoaService;
    }

    /// <summary>
    /// Insere um novo colaborador na base de dados
    /// </summary>
    /// <param name="pessoa">dados de colaborador</param>
    /// <returns></returns>
    public async Task CreateAsync(Pessoa pessoa)
    {
        if (_pessoaService.GetByCpf(pessoa.Cpf) == null)
        {
            _pessoaService.Create(pessoa);
        }

        var existingUser = await _userManager.FindByNameAsync(pessoa.Cpf);
        UsuarioIdentity usuario;

        if (existingUser == null)
        {
            usuario = await _pessoaService.CreateAsync(pessoa);
        }
        else
        {
            usuario = existingUser;
        }

        var isInRole = await _userManager.IsInRoleAsync(usuario, "COLABORADOR");
        if (!isInRole)
        {
            await _userManager.AddToRoleAsync(usuario, "COLABORADOR");
        }
    }


    public async Task<IEnumerable<PessoaSimpleDTO>> GetColaboradoresAsync()
    {
        var role = await _roleManager.FindByNameAsync("COLABORADOR");
        if (role == null)
        {
            return Enumerable.Empty<PessoaSimpleDTO>();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync("COLABORADOR");
        var colaboradores = new List<PessoaSimpleDTO>();

        foreach (var user in usersInRole)
        {
            var pessoa = _pessoaService.GetByCpf(user.NormalizedUserName);

            if (pessoa != null)
            {
                colaboradores.Add(new PessoaSimpleDTO
                {
                    Cpf = pessoa.Cpf,
                    Nome = pessoa.Nome,
                    Telefone1 = pessoa.Telefone1,
                    Email = pessoa.Email
                });
            }
        }

        return colaboradores;
    }


    public async Task DeleteAsync(string cpf)
    {
        // Colaboradores não têm permissão para excluir usuários
        throw new UnauthorizedAccessException("Colaboradores não têm permissão para excluir usuários.");
    }
}