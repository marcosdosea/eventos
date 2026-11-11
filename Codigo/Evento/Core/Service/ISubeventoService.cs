using Core.DTO;

namespace Core.Service
{
    public interface ISubeventoService
    {
        Task<List<BipouSubeventoDTO>> GetSubeventosBipouAsync(uint idEvento, CancellationToken cancellationToken = default);
        uint Create(Subevento subevento);
        void Edit(Subevento subevento);
        void Delete(uint id);
        Subevento Get(uint id);
        IEnumerable<Subevento> GetAll();
        IEnumerable<SubeventoDTO> GetByNome(string nome);
        IEnumerable<SubeventoEventoDTO> GetByIdEvento(uint idEvento);
        void AtualizarVagasDisponiveis(uint idSubevento, bool saveChanges = true);
        void AtualizarFrequenciaFinal(uint idSubevento, uint? idPessoa = null, bool saveChanges = true);
    }
}





