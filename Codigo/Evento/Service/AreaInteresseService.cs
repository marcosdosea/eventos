using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service;

public class AreaInteresseService : IAreaInteresseService
{
    /// <summary>
    /// Manter dados de áreas de interesse no banco de dados
    /// </summary>
    private readonly EventoContext _context;

    public AreaInteresseService(EventoContext context)
    {
        this._context = context;
    }
    /// <summary>
    /// Insere uma nova área de interesse na base de dados
    /// </summary>
    /// <param name="areainteresse">dados da area de interesse</param>
    /// <returns></returns>
    public uint Create(Areainteresse areainteresse)
    {
        _context.Add(areainteresse);
        _context.SaveChanges();
        return (uint)areainteresse.Id;
    }
    
    /// <summary>
    /// Edita uma área de interesse na base de dados
    /// </summary>
    /// <param name="areainteresse">dados da area de interesse</param>
    /// <returns></returns>
    public void Edit(Areainteresse areainteresse)
    {
        _context.Update(areainteresse);
        _context.SaveChanges();
    }
    /// <summary>
    /// Exclui uma área de interesse na base de dados
    /// </summary>
    /// <param name="id">dados da area de interesse</param>
    /// <returns></returns>
    public void Delete(uint id)
    {
        var areainteresse = _context.Areainteresses.Find(id);
        _context.Remove(areainteresse);
        _context.SaveChanges();
    }
    /// <summary>
    /// Obtém uma área de interesse específica por id
    /// </summary>
    /// <param name="id">dados da area de interesse</param>
    /// <returns></returns>
    public Areainteresse Get(uint id)
    {
        return _context.Areainteresses.Find(id);
    }
    /// <summary>
    /// Obtém todas áreas de interesse
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Areainteresse> GetAll()
    {
        return _context.Areainteresses.AsNoTracking();
    }
    
    /// <summary>
    /// Obtém uma área de interesse específica por nome
    /// </summary>
    /// <param name="nome">dados da area de interesse</param>
    /// <returns></returns>
    public IEnumerable<Areainteresse> GetByNome(string nome)
    {
        IQueryable<Areainteresse> tbAreaInteresse = _context.Areainteresses;
        var query = from areainteresse in tbAreaInteresse
            where areainteresse.Nome.StartsWith(nome)
            orderby areainteresse.Nome descending
            select areainteresse;
        return query;
    }
}
