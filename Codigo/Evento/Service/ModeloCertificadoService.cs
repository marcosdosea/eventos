using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class ModeloCertificadoService : IModeloCertificadoService
    {
        private readonly EventoContext _context;

        public ModeloCertificadoService(EventoContext context) {
            _context = context;
        }

        public uint Create(Modelocertificado modelocertificado)
        {
            _context.Add(modelocertificado);
            _context.SaveChanges();
            return modelocertificado.Id; 
        }

        public void Edit(Modelocertificado modelocertificado)
        {
            _context.Update(modelocertificado);
            _context.SaveChanges();
        }

        public void Delete(uint id)
        {
            var certificado = _context.Modelocertificados.Find(id);
            _context.Remove(certificado);
        }

        public Modelocertificado Get(uint id)
        {
            return _context.Modelocertificados.Find(id);
        }

        public IEnumerable<Modelocertificado> GetAll()
        {
            return _context.Modelocertificados.AsNoTracking();
        }
        
        public IEnumerable<Modelocertificado> GetByEvento(uint id)
        {
            var certificado = _context.Eventos.Include(e => e.Modelocertificados)
                .FirstOrDefault(e => e.Id == id);

            if(certificado != null)
            {
                return certificado.Modelocertificados;
            }

            return Enumerable.Empty<Modelocertificado>();
        }
    }
}
