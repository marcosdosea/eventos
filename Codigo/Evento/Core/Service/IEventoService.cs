using Core;



namespace Core.Service
{
    public interface IEventoService
    {
        uint Inserir(Evento evento);
        void Atualizar(Evento evento);
        void Remover(uint Id);

        Evento Obter(uint Id);

        IEnumerable<Evento> GetAll();

        IEnumerable<Evento> GetByNome(string Nome);
    }
}
