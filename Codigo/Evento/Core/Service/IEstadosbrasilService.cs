using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
	public interface IEstadosbrasilService
	{
		string Create(Estadosbrasil estadosbrasil);
		void Edit(Estadosbrasil estadosbrasil);
		void Delete(string Estado);
		Estadosbrasil Get(string Estado);
		IEnumerable<Estadosbrasil> GetAll();
	}
}
