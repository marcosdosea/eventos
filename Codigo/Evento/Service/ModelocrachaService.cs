using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public int Create(Modelocracha modelocracha)
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
        public void Delete(int id)
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
        public Modelocracha Get(int id)
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
    }
}
