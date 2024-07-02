using Core;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class TipoInscricaoService : ITipoInscricaoService
    {
        private readonly EventoContext _context;

        public TipoInscricaoService(EventoContext eventoContext)
        {
            _context = eventoContext;
        }

        /// <summary>
        /// Cria um novo tipo de inscrição
        /// </summary>
        /// <param name="tipoInscricao"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public uint Create(Tipoinscricao tipoInscricao)
        {
            _context.Add(tipoInscricao);
            _context.SaveChanges();
            return tipoInscricao.Id;
        }


        /// <summary>
        /// Edita um tipo de inscrição na base de dados
        /// </summary>
        /// <param name="tipoInscricao">dados da area de interesse</param>
        /// <returns></returns>
        public void Edit(Tipoinscricao tipoInscricao)
        {
            _context.Update(tipoInscricao);
            _context.SaveChanges();
        }

        /// <summary>
        /// Exclui um tipo de inscrição na base de dados
        /// </summary>
        /// <param name="id">dados da area de interesse</param>
        /// <returns></returns>
        public void Delete(uint id)
        {
            var tipoinscricao = _context.Tipoinscricaos.Find(id);
            _context.Remove(tipoinscricao);
            _context.SaveChanges();
        }

        /// <summary>
        /// Obtém um tipo de inscrição específica por id
        /// </summary>
        /// <param name="id">dados da area de interesse</param>
        /// <returns></returns>
        public Tipoinscricao Get(uint id)
        {
            return _context.Tipoinscricaos.Find(id);
        }

        /// <summary>
        /// Obtém todos tipo de inscrição
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tipoinscricao> GetAll()
        {
            return _context.Tipoinscricaos.AsNoTracking();
        }

        
        /// <summary>
        /// Obtém um tipo de inscrição específica por id
        /// </summary>
        /// <param name="nome">dados da area de interesse</param>
        /// <returns></returns>
        public IEnumerable<Tipoinscricao> GetByEvento(int id)
        {
            var evento = _context.Eventos.Include(e => e.Tipoinscricaos)
                .FirstOrDefault(e => e.Id == id);

            if (evento != null)
            {
                return evento.Tipoinscricaos;
            }

            return Enumerable.Empty<Tipoinscricao>();
        }
    }
}
