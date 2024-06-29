using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public interface IModelocrachaService
    {
        uint Create(Modelocracha modelocracha);
        void Edit(Modelocracha modelocracha);
        void Delete(uint id);
        Modelocracha Get(uint id);
        IEnumerable<Modelocracha> GetAll();
    }
}
