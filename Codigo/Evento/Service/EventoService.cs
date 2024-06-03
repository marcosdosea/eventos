using Core;
using Core.Service;
using Core.DTO;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class EventoService : IEventoService
    {
        private readonly EventoContext _context;
        
        public EventoService(EventoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria um novo evento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public uint Create(Evento evento)
        {
            _context.Add(evento);
            _context.SaveChanges();
            return (uint)evento.Id;
        }


        /// <summary>
        /// Deleta um evento do evento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Delete(uint Id)
        {
            var evento = _context.Eventos.Find(Id);
            if (evento != null)
            {
                _context.Eventos.Remove(evento);
                _context.SaveChanges();
            }
        }


        /// <summary>
        /// Edita um evento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Edit(Evento evento)
        {
            _context.Update(evento);
            _context.SaveChanges();
        }


        /// <summary>
        /// Busca um evento
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Evento Get(uint id)
        {
            return _context.Eventos.Find(id);
        }


        /// <summary>
        /// Busca todos os eventos
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Evento> GetAll()
        {
            return _context.Eventos.AsNoTracking();
        }

        /// <summary>
        /// Obter eventos que iniciam com o nome
        /// </summary>
        /// <param name="nome">nome do evento</param>
        /// <returns>lista dos eventos</returns>
        public IEnumerable<EventoDTO> GetByNome(string Nome)
        {
            var query = from evento in _context.Eventos
                        where evento.Nome.StartsWith(Nome)
                        orderby evento.Nome
                        select new Core.DTO.EventoDTO
                        {
                            Id = evento.Id,
                            Nome = evento.Nome
                        };
            return query.AsNoTracking();
        }
    }
}
