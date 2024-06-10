using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
	public class EstadosbrasilService : IEstadosbrasilService
	{

		private readonly EventoContext context;

		public EstadosbrasilService(EventoContext context)
		{
			this.context = context;
		}

		/// <summary>
		/// Insere um estado
		/// </summary>
		/// <param name="estadosbrasil">dados da area de interesse</param>
		/// <returns></returns>
		public string Create(Estadosbrasil estadosbrasil)
		{
			context.Add(estadosbrasil);
			context.SaveChanges();
			return estadosbrasil.Estado;
		}

		/// <summary>
		/// Deleta um estado
		/// </summary>
		/// <param name="Estado"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public void Delete(string Estado)
		{
			var estados = context.Estadosbrasils.Find(Estado);
			if (estados != null)
			{
				context.Remove(estados);
				context.SaveChanges();
			}
		}

		/// <summary>
		/// Edita um estado
		/// </summary>
		/// <param name="estadosbrasil"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public void Edit(Estadosbrasil estadosbrasil)
		{
			context.Update(estadosbrasil);
			context.SaveChanges();
		}

		/// <summary>
		/// Busca um estado
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public Estadosbrasil Get(string Estado)
		{
			return context.Estadosbrasils.Find(Estado);
		}

		/// <summary>
		/// Busca todos os eventos
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Estadosbrasil> GetAll()
		{
			return context.Estadosbrasils.AsNoTracking();
		}
	}
}
