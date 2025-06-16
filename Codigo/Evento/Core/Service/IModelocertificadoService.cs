using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;


namespace Core.Service
{
   
    public interface IModelocertificadoService
    {
      
        uint Create(Modelocertificado modelocertificado);

        void Update(Modelocertificado modelocertificado);

        void Delete(uint id);

        Modelocertificado Get(uint id);

        IEnumerable<Modelocertificado> GetAll();

        IEnumerable<Modelocertificado> GetByEvento(uint idEvento);
    }
}