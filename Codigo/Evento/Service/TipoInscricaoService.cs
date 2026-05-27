using Core;
using Core.DTO;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class TipoInscricaoService : ITipoInscricaoService
    {
        private readonly EventoContext _context;

        public TipoInscricaoService(EventoContext eventoContext)
        {
            _context = eventoContext;
        }

        /// <summary>
        /// Cria um novo tipo de inscrição
        /// </summary>
        public uint Create(Tipoinscricao tipoInscricao)
        {
            _context.Add(tipoInscricao);
            _context.SaveChanges();
            return tipoInscricao.Id;
        }

        /// <summary>
        /// Edita um tipo de inscrição na base de dados
        /// </summary>
        public void Edit(Tipoinscricao tipoInscricao)
        {
            _context.Update(tipoInscricao);
            _context.SaveChanges();
        }

        /// <summary>
        /// Exclui um tipo de inscrição na base de dados
        /// </summary>
        public void Delete(uint id)
        {
            var tipoinscricao = _context.Tipoinscricaos.Find(id);
            if (tipoinscricao != null)
            {
                _context.Remove(tipoinscricao);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Obtém um tipo de inscrição específica por id
        /// </summary>
        public Tipoinscricao Get(uint id)
        {
            return _context.Tipoinscricaos.Find(id);
        }

        /// <summary>
        /// Obtém todos tipo de inscrição
        /// </summary>
        public IEnumerable<Tipoinscricao> GetAll()
        {
            return _context.Tipoinscricaos.AsNoTracking().ToList();
        }

        /// <summary>
        /// Obtém os tipos de inscrição de um evento de forma otimizada
        /// </summary>
        public IEnumerable<Tipoinscricao> GetByEvento(uint id)
        {
            // 🚀 Otimização: Busca direto na tabela de inscrições sem carregar o objeto Evento inteiro na memória
            return _context.Tipoinscricaos
                .Where(ti => ti.IdEvento == id)
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Obtém tipos de inscrição do evento permitidos para subevento
        /// </summary>
        public IEnumerable<TipoInscricaoDTO> GetByEventoUsadaSubevento(uint id)
        {
            // 🚀 Otimização: Query direta e filtrada no banco, evitando Includes desnecessários
            return _context.Tipoinscricaos
                .Where(ti => ti.IdEvento == id && ti.UsadaSubevento == 1)
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
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Obtém os tipos de inscrição associados a um subevento
        /// </summary>
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
                .AsNoTracking()
                .ToList();
        }

        /// <summary>
        /// Associa um tipo de inscrição a um subevento
        /// </summary>
        /// <summary>
        /// Associa um tipo de inscrição a um subevento de forma segura
        /// </summary>
        public void AssociacaoTipoInscricaoSubevento(uint Idsubevento, uint IdtipoInscricao)
        {
            // 1. Busca o Subevento puramente (Garante que ele seja encontrado mesmo sem vínculos existentes)
            var subevento = _context.Subeventos
                .FirstOrDefault(s => s.Id == Idsubevento);

            if (subevento == null)
            {
                throw new ArgumentException($"Erro: Subevento com ID {Idsubevento} não foi encontrado no banco de dados.");
            }

            // 2. Carrega a coleção de inscrições separadamente de forma explícita e segura
            _context.Entry(subevento).Collection(s => s.IdTipoInscricaos).Load();

            var tipoInscricao = _context.Tipoinscricaos
                .FirstOrDefault(ti => ti.Id == IdtipoInscricao);

            if (tipoInscricao == null)
            {
                throw new ArgumentException($"Erro: Tipo de Inscrição com ID {IdtipoInscricao} não foi encontrado no banco de dados.");
            }

            subevento.IdTipoInscricaos ??= new List<Tipoinscricao>();

            if (!subevento.IdTipoInscricaos.Any(ti => ti.Id == IdtipoInscricao))
            {
                subevento.IdTipoInscricaos.Add(tipoInscricao);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Remove a associação de forma segura
        /// </summary>
        public void DeleteTipoInscricaoSubevento(uint Idsubevento, uint IdtipoInscricao)
        {
            var subevento = _context.Subeventos
                .FirstOrDefault(s => s.Id == Idsubevento);

            if (subevento == null)
            {
                throw new ArgumentException("Subevento não encontrado");
            }

            // Carrega a coleção explicitamente antes de remover
            _context.Entry(subevento).Collection(s => s.IdTipoInscricaos).Load();

            var tipoInscricao = subevento.IdTipoInscricaos
                .FirstOrDefault(ti => ti.Id == IdtipoInscricao);

            if (tipoInscricao == null)
            {
                throw new ArgumentException("Tipo de Inscrição não encontrado para o subevento");
            }

            subevento.IdTipoInscricaos.Remove(tipoInscricao);
            _context.SaveChanges();
        }
    }
}