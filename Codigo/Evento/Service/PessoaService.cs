using System.Text.RegularExpressions;
using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service;

public class PessoaService : IPessoaService
{
    /// <summary>
    /// Manter dados de pessoa no banco de dados
    /// </summary>
    private readonly EventoContext _context;

    private readonly IInscricaoService _inscricaoService;

    public PessoaService(EventoContext context,IInscricaoService inscricaoService)
    {
        this._context = context;
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
}


