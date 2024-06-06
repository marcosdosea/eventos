using Core;
using Core.DTO;

namespace Core.Service
{
    public interface IEventoService
    {
        uint Create(Evento evento);
        void Edit(Evento evento);
        void Delete(uint Id);

        Evento Get(uint Id);

        IEnumerable<Evento> GetAll();

        IEnumerable<EventoDTO> GetByNome(string Nome);
        void CreateGestorModel(Pessoa gestorEvento, uint idEvento);
    }
}
