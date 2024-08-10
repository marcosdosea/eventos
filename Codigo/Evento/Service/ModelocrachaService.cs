using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class ModelocrachaService : IModelocrachaService
    {
        private readonly EventoContext _context;

        public ModelocrachaService(EventoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria um novo modelo crachá
        /// </summary>
        /// <param name="modelocracha"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public uint Create(Modelocracha modelocracha)
        {
            _context.Add(modelocracha);
            _context.SaveChanges();
            return modelocracha.Id;
        }

        /// <summary>
        /// Deleta modelo crachá
        /// </summary>
        /// <param name="modelocracha"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Delete(uint id)
        {
            var modelo = _context.Modelocrachas.Find(id);
            _context.Remove(modelo);
            _context.SaveChanges();
        }

        /// <summary>
        /// Edita um modelo crachá
        /// </summary>
        /// <param name="modelocracha"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Edit(Modelocracha modelocracha)
        {
            _context.Update(modelocracha);
            _context.SaveChanges();
        }

        /// <summary>
        /// Busca modelo crachá
        /// </summary>
        /// <param name="modelocracha"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Modelocracha Get(uint id)
        {
            return _context.Modelocrachas.Find(id);
        }

        /// <summary>
        /// Busca todos os modelo crachá
        /// </summary>
        /// <param name="modelocracha"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<Modelocracha> GetAll()
        {
            return _context.Modelocrachas.AsNoTracking();
        }

        public IEnumerable<Modelocracha> GetByEvento(uint id)
        {
            var evento = _context.Eventos.Include(e => e.Modelocrachas)
                .FirstOrDefault(e => e.Id == id);

            if (evento != null)
            {
                return evento.Modelocrachas;
            }

            return Enumerable.Empty<Modelocracha>();
        }
    }
}
