using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class TipoeventoService : ITipoeventoService
    {
        private readonly EventoContext context;

        public TipoeventoService(EventoContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Insere um tipo de evento
        /// </summary>
        /// <param name="tipoevento">dados da area de interesse</param>
        /// <returns></returns>
        public uint Create(Tipoevento tipoevento)
        {
            context.Add(tipoevento);
            context.SaveChanges();
            return tipoevento.Id;
        }

        /// <summary>
        /// Exclui um tipo de evento
        /// </summary>
        /// <param name="idTipoEvento">dados da area de interesse</param>
        /// <returns></returns>
        public void Delete(uint idTipoEvento)
        {
            var tipoevento = context.Areainteresses.Find(idTipoEvento);
            context.Remove(tipoevento);
            context.SaveChanges();
        }

        /// <summary>
        /// Edita um tipo de evento
        /// </summary>
        /// <param name="areainteresse">dados da area de interesse</param>
        /// <returns></returns>
        public void Edit(Tipoevento tipoevento)
        {
            context.Update(tipoevento);
            context.SaveChanges();
        }

        /// <summary>
        /// Obtém um tipo de evento
        /// </summary>
        /// <param name="idTipoEvento">dados da area de interesse</param>
        /// <returns></returns>
        public Tipoevento Get(uint idTipoEvento)
        {
            return context.Tipoeventos.Find(idTipoEvento);
        }

        /// <summary>
        /// Obtém todos tipo de evento
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tipoevento> GetAll()
        {
            return context.Tipoeventos.AsNoTracking();
        }

		public string GetNomeById(uint idTipoEvento)
		{
			return context.Tipoeventos
				   .Where(t => t.Id == idTipoEvento)
				   .Select(t => t.Nome)
				   .FirstOrDefault();
		}
	}
}