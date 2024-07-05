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
        uint Create(Tipoevento tipoevento);
        void Edit(Tipoevento tipoevento);
        void Delete(uint idTipoEvento);
        Tipoevento Get(uint idTipoEvento);
		IEnumerable<Tipoevento> GetAll();
		string GetNomeById(uint idTipoEvento);
		string GetNomeById(uint? idTipoEvento);
	}
}
