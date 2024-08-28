using Core;
using Core.DTO;
using Core.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Service;

public class AdministradorService : IAdministradorService
{
    private readonly EventoContext _context;
    private readonly IdentityContext _identityContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly IPessoaService _pessoaService;
    
    public AdministradorService(EventoContext context,IdentityContext identityContext, RoleManager<IdentityRole> roleManager, UserManager<UsuarioIdentity> userManager, IPessoaService pessoaService)
    {
        
        _context = context;
        _identityContext = identityContext;
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
        
        var existingUser = await GetbyCpfAsync(pessoa.Cpf);
        UsuarioIdentity usuario;

        if (existingUser == null)
        {
            var newUser = new UsuarioIdentity
            {
                UserName = pessoa.Nome,
                Email = pessoa.Cpf,
                PhoneNumber = pessoa.Telefone1,
                NormalizedEmail = pessoa.Email.ToUpper() 
            };

            var result = await _userManager.CreateAsync(newUser);

            if (result.Succeeded)
            {
                usuario = newUser;
            }
            else
            {
                throw new Exception("Falha ao criar o usuário.");
            }
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

    
    public async Task<IEnumerable<PessoaGestorDTO>> GetAdministradoresAsync()
    {
        var role = await _roleManager.FindByNameAsync("ADMINISTRADOR");
        if (role == null)
        {
            return Enumerable.Empty<PessoaGestorDTO>();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync("ADMINISTRADOR");

        var administradores = usersInRole.Select(user => new PessoaGestorDTO
        {
            cpf = user.Email,
            Nome = user.UserName,
            Telefone1 = user.PhoneNumber,
            Email = user.NormalizedEmail,
        });

        return administradores;
    }

    public async Task<UsuarioIdentity> GetbyCpfAsync(string cpf)
    {
        var user = await _userManager.FindByEmailAsync(cpf);
        return user;
    }


}