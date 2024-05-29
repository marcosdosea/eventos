using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public interface IModelocrachaService
    {
        int Create(Modelocracha modelocracha);
        void Edit(Modelocracha modelocracha);
        void Delete(int id);
        Modelocracha Get(int id);
        IEnumerable<Modelocracha> GetAll();
    }
}
