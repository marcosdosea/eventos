using Core.DTO;

namespace Core
{
    public interface ITipoInscricaoService
    {
        uint Create(Tipoinscricao tipoInscricao);
        void Edit(Tipoinscricao tipoInscricao);
        void Delete(uint idTipoInscricao);
        Tipoinscricao Get(uint idTipoInscricao);
        IEnumerable<Tipoinscricao> GetAll();
        IEnumerable<Tipoinscricao> GetByEvento(uint idEvento);
        
        IEnumerable<TipoInscricaoDTO> GetByEventoUsadaSubevento(uint idEvento);

        IEnumerable<TipoInscricaoDTO> GetTiposInscricaosSubevento(uint idSubevento);

        void AssociacaoTipoInscricaoSubevento(uint Idsubevento, uint IdtipoInscricao);
        void DeleteTipoInscricaoSubevento(uint Idsubevento, uint IdtipoInscricao);

    }
}
