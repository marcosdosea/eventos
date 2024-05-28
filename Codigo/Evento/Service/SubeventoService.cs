using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class SubeventoService : ISubeventoService
    {
        private readonly EventoContext _context;

        public SubeventoService(EventoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria um novo subevento para o evento
        /// </summary>
        /// <param name="subevento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public uint Create(Subevento subevento)
        {
            _context.Add(subevento);
            _context.SaveChanges();
            return subevento.Id;
        }

        /// <summary>
        /// Deleta um subevento do evento
        /// </summary>
        /// <param name="subevento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Delete(uint id)
        {
            var subevento = _context.Subeventos.Find(id);
            _context.Remove(subevento);
            _context.SaveChanges();

        }

        /// <summary>
        /// Edita um subevento
        /// </summary>
        /// <param name="subevento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Edit(Subevento subevento)
        {
            _context.Update(subevento);
            _context.SaveChanges();
        }

        /// <summary>
        /// Busca um subevento
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Subevento Get(uint id)
        {
            return _context.Subeventos.Find(id);
        }

        /// <summary>
        /// Busca todos os subeventos
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Subevento> GetAll()
        {
            return _context.Subeventos.AsNoTracking();
        }

        /// <summary>
        /// Obter eventos que iniciam com o nome
        /// </summary>
        /// <param name="nome">nome da editora</param>
        /// <returns>lista de eventos</returns>
        public IEnumerable<SubeventoDTO> GetByNome(string nome)
        {
            var query = from subevento in _context.Subeventos
                        where subevento.Nome.StartsWith(nome)
                        orderby subevento.Nome
                        select new Core.DTO.SubeventoDTO
                        {
                            Id = subevento.Id,
                            Nome = subevento.Nome
                        };
            return query;
        }
    }
}

