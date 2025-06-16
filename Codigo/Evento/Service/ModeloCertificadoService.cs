using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service
{
  
    public class ModelocertificadoService : IModelocertificadoService
    {
        private readonly EventoContext _context;

        public ModelocertificadoService(EventoContext context)
        {
            _context = context;
        }

        public uint Create(Modelocertificado modelocertificado)
        {
            if (_context.Modelocertificados.Any(m => m.IdEvento == modelocertificado.IdEvento))
                throw new Exception("Já existe um modelo de certificado para este evento");
                
            _context.Modelocertificados.Add(modelocertificado);
            _context.SaveChanges();
            return modelocertificado.Id;
        }

        public void Update(Modelocertificado modelocertificado)
        {
            var existingModel = _context.Modelocertificados.Find(modelocertificado.Id);
            if (existingModel == null)
                throw new Exception("Modelo de certificado não encontrado");
                
            _context.Entry(existingModel).CurrentValues.SetValues(modelocertificado);
            _context.SaveChanges();
        }

        public void Delete(uint id)
        {
            var entity = _context.Modelocertificados.Find(id);
            if (entity != null)
            {
                _context.Modelocertificados.Remove(entity);
                _context.SaveChanges();
            }
        }

        public Modelocertificado Get(uint id)
        {
            return _context.Modelocertificados
                .Include(m => m.IdEventoNavigation)
                .AsNoTracking()
                .FirstOrDefault(m => m.Id == id);
        }

        public IEnumerable<Modelocertificado> GetAll()
        {
            return _context.Modelocertificados
                .Include(m => m.IdEventoNavigation)
                .AsNoTracking()
                .ToList();
        }

        public IEnumerable<Modelocertificado> GetByEvento(uint idEvento)
        {
            return _context.Modelocertificados
                .Include(m => m.IdEventoNavigation)
                .Where(m => m.IdEvento == idEvento)
                .AsNoTracking()
                .ToList();
        }
    }
}
