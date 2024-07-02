namespace Core
{
    public interface ITipoInscricaoService
    {
        uint Create(Tipoinscricao tipoInscricao);
        void Edit(Tipoinscricao tipoInscricao);
        void Delete(uint idTipoInscricao);
        Tipoinscricao Get(uint idTipoInscricao);
        IEnumerable<Tipoinscricao> GetAll();
        IEnumerable<Tipoinscricao> GetByEvento(int idEvento);

    }
}
