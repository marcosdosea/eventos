using Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public interface ITipoeventoService
    {
        int Create(Tipoevento tipoevento);
        void Edit(Tipoevento tipoevento);
        void Delete(int idTipoEvento);
        Tipoevento Get(int idTipoEvento);
		IEnumerable<Tipoevento> GetAll();
		string GetNomeById(int idTipoEvento);
	}
}
