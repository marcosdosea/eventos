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

    public async Task UpdateAsync(Pessoa pessoa)
    {
        var existingPessoa = _pessoaService.GetByCpf(pessoa.Cpf);
        if (existingPessoa == null)
        {
            throw new Exception("Colaborador não encontrado.");
        }

        pessoa.Id = existingPessoa.Id; // Garante que o ID seja mantido
        _pessoaService.Edit(pessoa);

        var usuario = await _userManager.FindByNameAsync(pessoa.Cpf);
        if (usuario != null)
        {
            usuario.Email = pessoa.Email;
            usuario.NormalizedEmail = pessoa.Email.ToUpper();
            usuario.PhoneNumber = pessoa.Telefone1;
            var result = await _userManager.UpdateAsync(usuario);
            if (!result.Succeeded)
            {
                throw new Exception($"Erro ao atualizar usuário: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    public async Task DeleteAsync(string cpf)
    {
        var pessoa = _pessoaService.GetByCpf(cpf);
        if (pessoa == null)
        {
            throw new Exception("Colaborador não encontrado.");
        }

        try
        {
            var usuario = await _userManager.FindByNameAsync(cpf);
            if (usuario != null)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                if (roles.Contains("COLABORADOR"))
                {
                    await _userManager.RemoveFromRoleAsync(usuario, "COLABORADOR");
                }
                var userResult = await _userManager.DeleteAsync(usuario);
                if (!userResult.Succeeded)
                {
                    throw new Exception($"Erro ao excluir usuário: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
                }
            }

            _pessoaService.Delete(pessoa.Id);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao excluir colaborador: {ex.Message}. Verifique se o colaborador possui inscrições associadas.");
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
            var pessoa = _pessoaService.GetByCpf(user.UserName);
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
}