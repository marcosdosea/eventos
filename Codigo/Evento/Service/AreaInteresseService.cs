using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service;

public class AreaInteresseService : IAreaInteresseService
{
    /// <summary>
    /// Manter dados de áreas de interesse no banco de dados
    /// </summary>
    private readonly EventoContext context;

    public AreaInteresseService(EventoContext context)
    {
        this.context = context;
    }
    /// <summary>
    /// Insere uma nova área de interesse na base de dados
    /// </summary>
    /// <param name="areainteresse">dados da area de interesse</param>
    /// <returns></returns>
    public uint Create(Areainteresse areainteresse)
    {
        context.Add(areainteresse);
        context.SaveChanges();
        return areainteresse.Id;
    }
    
    /// <summary>
    /// Edita uma área de interesse na base de dados
    /// </summary>
    /// <param name="areainteresse">dados da area de interesse</param>
    /// <returns></returns>
    public void Edit(Areainteresse areainteresse)
    {
        context.Update(areainteresse);
        context.SaveChanges();
    }
    /// <summary>
    /// Exclui uma área de interesse na base de dados
    /// </summary>
    /// <param name="id">dados da area de interesse</param>
    /// <returns></returns>
    public void Delete(uint id)
    {
        var areainteresse = context.Areainteresses.Find(id);
        context.Remove(areainteresse);
        context.SaveChanges();
    }
    /// <summary>
    /// Obtém uma área de interesse específica por id
    /// </summary>
    /// <param name="id">dados da area de interesse</param>
    /// <returns></returns>
    public Areainteresse Get(uint id)
    {
        return context.Areainteresses.Find(id);
    }
    /// <summary>
    /// Obtém todas áreas de interesse
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Areainteresse> GetAll()
    {
        return context.Areainteresses.AsNoTracking();
    }
    
    /// <summary>
    /// Obtém uma área de interesse específica por nome
    /// </summary>
    /// <param name="nome">dados da area de interesse</param>
    /// <returns></returns>
    public IEnumerable<Areainteresse> GetByNome(string nome)
    {
        IQueryable<Areainteresse> tbAreaInteresse = context.Areainteresses;
        var query = from areainteresse in tbAreaInteresse
            where areainteresse.Nome.StartsWith(nome)
            orderby areainteresse.Nome descending
            select areainteresse;
        return query;
    }
}
