using Core;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class TipoInscricaoService : ITipoInscricaoService
    {
        private readonly EventoContext context;

        public TipoInscricaoService(EventoContext eventoContext)
        {
            context = eventoContext;
        }

        /// <summary>
        /// Cria um novo tipo de inscrição para o evento
        /// </summary>
        /// <param name="tipoInscricao"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public int Create(Tipoinscricao tipoInscricao)
        {
            context.Add(tipoInscricao);
            context.SaveChanges();
            return tipoInscricao.Id;
        }

        public void Delete(int idTipoInscricao)
        {
            throw new NotImplementedException();
        }

        public void Edit(Tipoinscricao tipoInscricao)
        {
            throw new NotImplementedException();
        }

        public Tipoinscricao Get(int idTipoInscricao)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tipoinscricao> GetAll()
        {
            return context.Tipoinscricaos.AsNoTracking();
        }

        public IEnumerable<Tipoinscricao> GetByEvento(int idEvento)
        {
            throw new NotImplementedException();
        }
    }
}
