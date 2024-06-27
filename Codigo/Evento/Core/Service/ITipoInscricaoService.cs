namespace Core
{
    public interface ITipoInscricaoService
    {
        uint Create(Tipoinscricao tipoInscricao);
        void Edit(Tipoinscricao tipoInscricao);
        void Delete(int idTipoInscricao);
        Tipoinscricao Get(int idTipoInscricao);
        IEnumerable<Tipoinscricao> GetAll();
        IEnumerable<Tipoinscricao> GetByEvento(int idEvento);

    }
}
