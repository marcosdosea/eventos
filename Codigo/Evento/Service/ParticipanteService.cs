using Core;
using Core.DTO;
using Core.Service;
using Microsoft.AspNetCore.Identity;

namespace Service;

public class ParticipanteService : IParticipanteService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly IPessoaService _pessoaService;

    public ParticipanteService(RoleManager<IdentityRole> roleManager, UserManager<UsuarioIdentity> userManager, IPessoaService pessoaService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _pessoaService = pessoaService;
    }

    /// <summary>
    /// Insere uma novo participante na base de dados
    /// </summary>
    /// <param name="participante">dados de participante</param>
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

        var isInRole = await _userManager.IsInRoleAsync(usuario, "PARTICIPANTE");
        if (!isInRole)
        {
            await _userManager.AddToRoleAsync(usuario, "PARTICIPANTE");
        }
    }


    public async Task<IEnumerable<PessoaSimpleDTO>> GetParticipantesAsync()
    {
        var role = await _roleManager.FindByNameAsync("PARTICIPANTE");
        if (role == null)
        {
            return Enumerable.Empty<PessoaSimpleDTO>();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync("PARTICIPANTE");
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
                    NomeCracha = pessoa.NomeCracha,
                    Telefone1 = pessoa.Telefone1,
                    Email = pessoa.Email
                });
            }
        }

        return administradores;
    }


    public async Task DeleteAsync(string cpf)
    {
        var usuario = await _userManager.FindByNameAsync(cpf); ;
        if (usuario == null)
        {
            throw new Exception("Usuário não encontrado.");
        }

        var isInRole = await _userManager.IsInRoleAsync(usuario, "PARTICIPANTE");
        if (isInRole)
        {
            var removeRoleResult = await _userManager.RemoveFromRoleAsync(usuario, "PARTICIPANTE");
            if (!removeRoleResult.Succeeded)
            {
                throw new Exception("Falha ao remover o usuário da função de participante.");
            }
        }
        else
        {
            throw new Exception("O usuário não é um participante.");
        }
    }

    public async Task<Pessoa?> GetParticipanteByCpfAsync(string cpf)
    {
        // Busca a pessoa completa pelo CPF usando o serviço de pessoas
        // Pode ser síncrono, mas mantemos async para compatibilidade com a interface
        return await Task.FromResult(_pessoaService.GetByCpf(cpf));
    }
}