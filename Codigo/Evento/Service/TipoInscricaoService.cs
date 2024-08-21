using Core;
using Core.DTO;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class TipoInscricaoService : ITipoInscricaoService
    {
        private readonly EventoContext _context;
        private ITipoInscricaoService _tipoInscricaoServiceImplementation;

        public TipoInscricaoService(EventoContext eventoContext)
        {
            _context = eventoContext;
        }

        /// <summary>
        /// Cria um novo tipo de inscrição
        /// </summary>
        /// <param name="tipoInscricao"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public uint Create(Tipoinscricao tipoInscricao)
        {
            _context.Add(tipoInscricao);
            _context.SaveChanges();
            return tipoInscricao.Id;
        }


        /// <summary>
        /// Edita um tipo de inscrição na base de dados
        /// </summary>
        /// <param name="tipoInscricao">dados da area de interesse</param>
        /// <returns></returns>
        public void Edit(Tipoinscricao tipoInscricao)
        {
            _context.Update(tipoInscricao);
            _context.SaveChanges();
        }

        /// <summary>
        /// Exclui um tipo de inscrição na base de dados
        /// </summary>
        /// <param name="id">dados da area de interesse</param>
        /// <returns></returns>
        public void Delete(uint id)
        {
            var tipoinscricao = _context.Tipoinscricaos.Find(id);
            _context.Remove(tipoinscricao);
            _context.SaveChanges();
        }

        /// <summary>
        /// Obtém um tipo de inscrição específica por id
        /// </summary>
        /// <param name="id">dados da area de interesse</param>
        /// <returns></returns>
        public Tipoinscricao Get(uint id)
        {
            return _context.Tipoinscricaos.Find(id);
        }

        /// <summary>
        /// Obtém todos tipo de inscrição
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tipoinscricao> GetAll()
        {
            return _context.Tipoinscricaos.AsNoTracking();
        }

        public IEnumerable<Tipoinscricao> GetByEvento(uint id)
        {
            var evento = _context.Eventos.Include(e => e.Tipoinscricaos)
                .FirstOrDefault(e => e.Id == id);

            if (evento != null)
            {
                return evento.Tipoinscricaos;
            }

            return Enumerable.Empty<Tipoinscricao>();
        }


        /// <summary>
        /// Obtém um tipo de inscrição específica por id
        /// </summary>
        /// <param name="nome">dados da area de interesse</param>
        /// <returns></returns>
        public IEnumerable<TipoInscricaoDTO> GetByEventoUsadaSubevento(uint id)
        {
            var evento = _context.Eventos
                .Include(e => e.Tipoinscricaos)
                .FirstOrDefault(e => e.Id == id);

            if (evento != null)
            {
                return evento.Tipoinscricaos
                    .Where(ti => ti.UsadaSubevento == 1)
                    .Select(tipoInscricao => new TipoInscricaoDTO
                    {
                        Id = tipoInscricao.Id,
                        Nome = tipoInscricao.Nome,
                        IdEvento = tipoInscricao.IdEvento,
                        Descricao = tipoInscricao.Descricao,
                        Valor = tipoInscricao.Valor,
                        DataInicio = tipoInscricao.DataInicio,
                        Datafim = tipoInscricao.Datafim
                    })
                    .ToList();
            }

            return Enumerable.Empty<TipoInscricaoDTO>();
        }

        public IEnumerable<TipoInscricaoDTO> GetTiposInscricaosSubevento(uint idSubevento)
        {
            return _context.Tipoinscricaos
                .Where(ti => ti.IdSubEventos.Any(se => se.Id == idSubevento))
                .Select(ti => new TipoInscricaoDTO
                {
                    Id = ti.Id,
                    Nome = ti.Nome,
                    IdEvento = ti.IdEvento,
                    Descricao = ti.Descricao,
                    Valor = ti.Valor,
                    DataInicio = ti.DataInicio,
                    Datafim = ti.Datafim
                })
                .ToList();
        }

        public void AssociacaoTipoInscricaoSubevento(uint Idsubevento, uint IdtipoInscricao)
        {
            var subevento = _context.Subeventos
                .Include(s => s.IdTipoInscricaos)
                .FirstOrDefault(s => s.Id == Idsubevento);

            var tipoInscricao = _context.Tipoinscricaos
                .FirstOrDefault(ti => ti.Id == IdtipoInscricao);

            if (subevento == null || tipoInscricao == null)
            {
                throw new ArgumentException("Subevento or Tipoinscricao not found");
            }

            if (!subevento.IdTipoInscricaos.Contains(tipoInscricao))
            {
                subevento.IdTipoInscricaos.Add(tipoInscricao);
                tipoInscricao.IdSubEventos.Add(subevento);
                _context.SaveChanges();
            }
        }
        public void DeleteTipoInscricaoSubevento(uint Idsubevento, uint IdtipoInscricao)
        {
            var subevento = _context.Subeventos
                .Include(s => s.IdTipoInscricaos)
                .FirstOrDefault(s => s.Id == Idsubevento);

            if (subevento == null)
            {
                throw new ArgumentException("Subevento não encontrado");
            }
            var tipoInscricao = subevento.IdTipoInscricaos
                .FirstOrDefault(ti => ti.Id == IdtipoInscricao);

            if (tipoInscricao == null)
            {
                throw new ArgumentException("Tipo de Inscrição não encontrado para o subevento");
            }

            tipoInscricao.IdSubEventos.Remove(subevento);
            subevento.IdTipoInscricaos.Remove(tipoInscricao);

            _context.SaveChanges();
        }
    }
}
