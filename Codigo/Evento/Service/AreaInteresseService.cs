using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.Data;

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
        Salvar(areainteresse, nova: true);
        return areainteresse.Id;
    }
    
    /// <summary>
    /// Edita uma área de interesse na base de dados
    /// </summary>
    /// <param name="areainteresse">dados da area de interesse</param>
    /// <returns></returns>
    public void Edit(Areainteresse areainteresse)
    {
        Salvar(areainteresse, nova: false);
    }

    private void Salvar(Areainteresse area, bool nova)
    {
        using var transacao = context.Database.IsRelational() && context.Database.CurrentTransaction == null
            ? context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;

        area.Nome = area.Nome.Trim();
        var nomeNormalizado = area.Nome.ToLowerInvariant();
        if (context.Areainteresses.Any(a => (nova || a.Id != area.Id) && a.Nome.Trim().ToLower() == nomeNormalizado))
            throw new ServiceException("Já existe uma área de interesse cadastrada com este nome.");

        if (nova)
            context.Add(area);
        else
            context.Update(area);

        try
        {
            context.SaveChanges();
            transacao?.Commit();
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1205 or 1213 })
        {
            throw new ServiceException("Outra alteração ocorreu ao salvar a área de interesse. Tente novamente.", ex);
        }
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
        return context.Areainteresses.AsNoTracking().ToList();
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
