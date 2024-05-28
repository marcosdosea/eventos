using Core.DTO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public interface IAreaInteresseService 
    {
        uint Create(Areainteresse areainteresse);
        void Edit(Areainteresse areainteresse);
        void Delete(uint id);
        Areainteresse Get(uint id);
        IEnumerable<Areainteresse> GetAll();
        IEnumerable<AreainteresseDTO> GetByNome(string nome);
    }
}
