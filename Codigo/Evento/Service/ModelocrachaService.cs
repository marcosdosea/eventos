using Core;
using Core.Service;
using Microsoft.AspNetCore.Http;
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
            var modelocracha = _context.Modelocrachas.Find(id);
            _context.Remove(modelocracha);
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

        public bool IsImage(IFormFile file)
        {
            if (file == null)
                return false;

            // Verifica o tipo MIME do arquivo
            if (file.ContentType.ToLower() != "image/jpeg" &&
                file.ContentType.ToLower() != "image/jpg" &&
                file.ContentType.ToLower() != "image/png" &&
                file.ContentType.ToLower() != "image/gif")
            {
                return false;
            }

            // Verifica a extensão do arquivo (opcional)
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif")
            {
                return false;
            }

            return true;
        }
    }
}
