using Core.DTO;

namespace Core.Service
{
    public interface IEventoService
    {
        uint Create(Evento evento);
        void Edit(Evento evento, List<uint> idsAreaInteresse);
        void Delete(uint Id);

        Evento Get(uint Id);

        EventoSimpleDTO GetEventoSimpleDto(uint id);

        IEnumerable<Evento> GetAll();

        IEnumerable<EventoDTO> GetByNome(string Nome);

		public IEnumerable<Areainteresse> GetAreasInteresseByEventoId(uint Id);

		string GetNomeById(uint id);

        void AtualizarVagasDisponiveis(uint idEvento);
    }
}
