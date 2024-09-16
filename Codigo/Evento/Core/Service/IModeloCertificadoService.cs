using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public interface IModeloCertificadoService
    {
        uint Create(Modelocertificado modelocertificado);
        void Edit(Modelocertificado modelocertificado);
        void Delete(uint id);
        Modelocertificado Get(uint id);
        IEnumerable<Modelocertificado> GetByEvento(uint id);
        IEnumerable<Modelocertificado> GetAll();
    }
}
