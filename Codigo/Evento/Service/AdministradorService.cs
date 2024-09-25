using Core;
using Core.DTO;
using Core.Service;
using Microsoft.AspNetCore.Identity;

namespace Service;

public class AdministradorService : IAdministradorService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly IPessoaService _pessoaService;
    
    public AdministradorService(RoleManager<IdentityRole> roleManager, UserManager<UsuarioIdentity> userManager, IPessoaService pessoaService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _pessoaService = pessoaService;
    }
    
    /// <summary>
    /// Insere uma novo administrador na base de dados
    /// </summary>
    /// <param name="administrador">dados de administrador</param>
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

        var isInRole = await _userManager.IsInRoleAsync(usuario, "ADMINISTRADOR");
        if (!isInRole)
        {
            await _userManager.AddToRoleAsync(usuario, "ADMINISTRADOR");
        }
    }

    
    public async Task<IEnumerable<PessoaSimpleDTO>> GetAdministradoresAsync()
    {
        var role = await _roleManager.FindByNameAsync("ADMINISTRADOR");
        if (role == null)
        {
            return Enumerable.Empty<PessoaSimpleDTO>();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync("ADMINISTRADOR");
        var administradores = new List<PessoaSimpleDTO>();

        foreach (var user in usersInRole)
        {
            var pessoa = _pessoaService.GetByCpf(user.NormalizedUserName);

            if (pessoa != null)
            {
                administradores.Add(new PessoaSimpleDTO
                {
                    Cpf = pessoa.Cpf,
                    Nome = pessoa.Nome,
                    Telefone1 = pessoa.Telefone1,
                    Email = pessoa.Email
                });
            }
        }

        return administradores;
    }
    

    public async Task DeleteAsync(string cpf)
    {
        var usuario = await _userManager.FindByNameAsync(cpf);;
        if (usuario == null)
        {
            throw new Exception("Usuário não encontrado.");
        }
        
        var isInRole = await _userManager.IsInRoleAsync(usuario, "ADMINISTRADOR");
        if (isInRole)
        {
            var removeRoleResult = await _userManager.RemoveFromRoleAsync(usuario, "ADMINISTRADOR");
            if (!removeRoleResult.Succeeded)
            {
                throw new Exception("Falha ao remover o usuário da função de administrador.");
            }
        }
        else
        {
            throw new Exception("O usuário não é um administrador.");
        }
    }


}