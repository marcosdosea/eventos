using System.Text.RegularExpressions;
using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using EventoWeb.Areas.Identity.Data;


namespace Service;

public class PessoaService : IPessoaService
{
    /// <summary>
    /// Manter dados de pessoa no banco de dados
    /// </summary>
    
    private readonly EventoContext _context;
    private readonly IdentityContext _identityContext;
    private readonly IInscricaoService _inscricaoService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<UsuarioIdentity> _userManager;

    public PessoaService(EventoContext context, IInscricaoService inscricaoService, IdentityContext identityContext, RoleManager<IdentityRole> roleManager, UserManager<UsuarioIdentity> userManager)
    {
        _context = context;
        _inscricaoService = inscricaoService;
        _identityContext = identityContext;
        _roleManager = roleManager;
        _userManager = userManager;
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

        return query.SingleOrDefault(); // Returns a single matching Pessoa or null if none found
    }
    
    public void CreatePessoaPapel(Pessoa pessoa, uint idEvento, int idPapel)
    {   
        var existingPessoa = GetByCpf(pessoa.Cpf);

        uint idPessoa;
    
        if(existingPessoa != null)
        {
            idPessoa = existingPessoa.Id;
        }
        else
        {
            idPessoa = Create(pessoa);
        }
    
        var novaInscricao = new Inscricaopessoaevento
        {
            IdPessoa = idPessoa,
            IdEvento = idEvento,
            IdPapel = idPapel,
            DataInscricao = DateTime.Now, 
            Status = "S", //(Solicitada)
        };
        _inscricaoService.CreateInscricaoEvento(novaInscricao);
            
    }
    
    public bool CPFIsValid(string cpf)
    {
        cpf = Regex.Replace(cpf, @"[^\d]", "");

        if (cpf.Length != 11)
            return false;

        if (Regex.IsMatch(cpf, @"^(\d)\1{10}$"))
            return false;
        
        int[] multipliers1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * multipliers1[i];
        
        int remainder = sum % 11;
        int digit1 = remainder < 2 ? 0 : 11 - remainder;

        int[] multipliers2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * multipliers2[i];
        
        remainder = sum % 11;
        int digit2 = remainder < 2 ? 0 : 11 - remainder;
        
        return cpf.EndsWith(digit1.ToString() + digit2.ToString());
    }
    public string FormataCPF(string cpf)
    {
        return Regex.Replace(cpf, @"\D", "");
    }
    public string FormataCep(string cep)
    {
        return Regex.Replace(cep, @"[^\d]", "");
    }
}


