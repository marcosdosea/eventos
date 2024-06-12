using Core.DTO;

namespace Core.Service
{
    public interface IEventoService
    {
        uint Create(Evento evento);
        void Edit(Evento evento);
        void Delete(uint Id);

        Evento Get(uint Id);

        EventoSimpleDTO GetEventoSimpleDto(uint id);

        IEnumerable<Evento> GetAll();

        IEnumerable<EventoDTO> GetByNome(string Nome);

        void CreateGestorModel(Pessoa pessoa,uint IdEvento);
    }
}
