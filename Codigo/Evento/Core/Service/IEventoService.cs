using Core.DTO;

namespace Core.Service
{
    public interface IEventoService
    {
        uint Create(Evento evento, List<uint> idsAreaInteresse = null);
        bool Edit(Evento evento, List<uint> idsAreaInteresse);
        void Delete(uint Id);

        Evento Get(uint Id);

        EventoSimpleDTO GetEventoSimpleDto(uint id);

        IEnumerable<Evento> GetAll();
        
        IEnumerable<Evento> GetEventByCpf(string userCpf, uint idPapel);

        IEnumerable<EventoDTO> GetByNome(string Nome);

		public IEnumerable<Areainteresse> GetAreasInteresseByEventoId(uint Id);

		string GetNomeById(uint id);

        void AtualizarVagasDisponiveis(uint idEvento);

        IEnumerable<Evento> Search(EventoFilterDTO filter, int pagina, int tamanhoPagina, out int totalRegistros);
        
        public uint VerificarPerfilAtual(IList<string> roles, String perfilAtivo, uint idPapel);
    }
}
