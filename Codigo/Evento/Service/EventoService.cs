
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
    public class EventoService : IEventoService
    {
        private readonly EventoContext _context;
        
        public EventoService(EventoContext context)
        {
            _context = context;
        }

        public uint Inserir(Evento evento)
        {
            _context.Add(evento);
            _context.SaveChanges();
            return (uint)evento.Id;
        }

        public void Remover(uint Id)
        {
            var evento = _context.Eventos.Find(Id);
            if (evento != null)
            {
                _context.Eventos.Remove(evento);
                _context.SaveChanges();
            }
        }

        public void Atualizar(Evento evento)
        {
            _context.Update(evento);
            _context.SaveChanges();
        }

        public Evento Obter(uint Id)
        {
            return _context.Eventos.Find(Id);
        }

        public IEnumerable<Evento> GetAll()
        {
            return _context.Eventos.AsNoTracking();
        }

        public IEnumerable<Evento> GetByNome(string Nome)
        {
            var query = from evento in _context.Eventos
                        where evento.Nome.StartsWith(Nome)
                        orderby evento.Nome
                        select evento;
            return query.AsNoTracking();
        }
    }
}
