using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class AreainteresseService : IAreaInteresseService
    {
      private readonly EventoContext _context;

        public AreainteresseService(EventoContext context) {  
            _context = context; }

        public uint Create(Areainteresse areainteresse)
        {
            _context.Add(areainteresse);
            _context.SaveChanges();
            return areainteresse.Id;
        }
        public void Edit(Areainteresse areainteresse)
        {
            _context.Update(areainteresse);
            _context.SaveChanges();
        }
        public void Delete(uint id)
        {
            var areainteresse = _context.Areainteresses.Find(id);
            _context.Remove(id);
            _context.SaveChanges();
        }
        public Areainteresse Get(uint id)
        {
            return _context.Areainteresses.Find(id);
            
        }
        public IEnumerable<Areainteresse> GetAll()
        {
            return _context.Areainteresses.AsNoTracking();
        }
        public IEnumerable<AreainteresseDTO> GetByNome(string nome)
        {
            var query = from areainteresse in _context.Areainteresses
                        where areainteresse.Nome.StartsWith(nome)
                        orderby areainteresse.Nome
                        select new Core.DTO.AreainteresseDTO
                        {
                            Id = areainteresse.Id,
                            Nome = areainteresse.Nome
                        };
            return query;
        }
    }
}
