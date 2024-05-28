using Core.DTO;

namespace Core.Service
{
    public interface ISubeventoService
    {
        uint Create(Subevento subevento);
        void Edit(Subevento subevento);
        void Delete(uint id);
        Subevento Get(uint id);
        IEnumerable<Subevento> GetAll();
        IEnumerable<SubeventoDTO> GetByNome(string nome);
    }
}





