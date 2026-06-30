using Core;
using Core.Service;
using Core.DTO;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class EventoService : IEventoService
    {
        private readonly EventoContext _context;

        public EventoService(EventoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria um novo evento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public uint Create(Evento evento)
        {
            _context.Add(evento);
            _context.SaveChanges();
            return (uint)evento.Id;
        }

        /// <summary>
        /// Deleta um evento do evento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Delete(uint Id)
        {
            var evento = _context.Eventos
                .Include(e => e.IdAreaInteresses)         
                .Include(e => e.Inscricaopessoaeventos)   
                .Include(e => e.Subeventos)
                .Include(e => e.Modelocertificados)
                .Include(e => e.Modelocrachas)
                .Include(e => e.Participacaopessoaeventos)
                .Include(e => e.Tipoinscricaos)
                .FirstOrDefault(e => e.Id == Id);

            if (evento != null)
            {
                try
                {
                    evento.IdAreaInteresses.Clear();

                    _context.Subeventos.RemoveRange(evento.Subeventos);
                    _context.Inscricaopessoaeventos.RemoveRange(evento.Inscricaopessoaeventos);
                    _context.Modelocertificados.RemoveRange(evento.Modelocertificados);
                    _context.Modelocrachas.RemoveRange(evento.Modelocrachas);
                    _context.Participacaopessoaeventos.RemoveRange(evento.Participacaopessoaeventos);
                    _context.Tipoinscricaos.RemoveRange(evento.Tipoinscricaos);

                    _context.Eventos.Remove(evento);

                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    throw new Exception("Não foi possível excluir o evento. Verifique se existem dependências ativas.", ex);
                }
            }
        }

        /// <summary>
        /// Edita um evento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Edit(Evento evento, List<uint> novosIdsAreaInteresse)
        {
            try
            {
                var local = _context.Eventos.Local.FirstOrDefault(e => e.Id == evento.Id);
                if (local != null)
                {
                    _context.Entry(local).State = EntityState.Detached;
                }

                var eventoExistente = _context.Eventos
                    .Include(e => e.IdAreaInteresses)
                    .FirstOrDefault(e => e.Id == evento.Id);

                if (eventoExistente == null) throw new ServiceException();

                var idsAtuaisNoBanco = eventoExistente.IdAreaInteresses.Select(ai => ai.Id).ToList();
                var idsDesejados = novosIdsAreaInteresse ?? new List<uint>();

                _context.Entry(eventoExistente).CurrentValues.SetValues(evento);

                if (evento.ImagemPortal != null && evento.ImagemPortal.Length > 0)
                {
                    eventoExistente.ImagemPortal = evento.ImagemPortal;
                }

                var paraRemover = eventoExistente.IdAreaInteresses
                    .Where(ai => !idsDesejados.Contains(ai.Id))
                    .ToList();

                foreach (var area in paraRemover)
                {
                    eventoExistente.IdAreaInteresses.Remove(area);
                }

                var idsParaAdicionar = idsDesejados
                    .Where(id => !idsAtuaisNoBanco.Contains(id))
                    .ToList();

                if (idsParaAdicionar.Any())
                {
                    var novasAreas = _context.Areainteresses
                        .Where(ai => idsParaAdicionar.Contains(ai.Id))
                        .ToList();

                    foreach (var area in novasAreas)
                    {
                        eventoExistente.IdAreaInteresses.Add(area);
                    }
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Erro ao atualizar o evento: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca um evento
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Evento Get(uint id)
        {
            return _context.Eventos.FirstOrDefault(e => e.Id == id);
        }
        public EventoSimpleDTO GetEventoSimpleDto(uint id)
        {
            var evento = Get(id);
            if (evento != null)
            {
                var eventoSimpleDto = new EventoSimpleDTO
                {
                    Id = evento.Id,
                    Nome = evento.Nome
                };

                return eventoSimpleDto;
            }
            else
            {
                return null;
            }
        }
    
        /// <summary>
        /// Busca todos os eventos
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Evento> GetAll()
        {
            return _context.Eventos.AsNoTracking();
        }

        public IEnumerable<Evento> GetEventByCpf(string userCpf, uint idPapel)
        {
            var pessoa = _context.Pessoas.FirstOrDefault(p => p.Cpf == userCpf);
            if (pessoa == null)
            {
                throw new Exception("Pessoa não encontrada para o CPF fornecido.");
            }
            var eventos = from evento in _context.Eventos
                join inscricao in _context.Inscricaopessoaeventos
                    on evento.Id equals inscricao.IdEvento
                where inscricao.IdPessoa == pessoa.Id && inscricao.IdPapel == idPapel
                select evento;

            return eventos.ToList();
        }

        /// <summary>
        /// Obter eventos que iniciam com o nome
        /// </summary>
        /// <param name="nome">nome do evento</param>
        /// <returns>lista dos eventos</returns>
        public IEnumerable<EventoDTO> GetByNome(string Nome)
        {
            var query = from evento in _context.Eventos
                        where evento.Nome.StartsWith(Nome)
                        orderby evento.Nome
                        select new Core.DTO.EventoDTO
                        {
                            Id = evento.Id,
                            Nome = evento.Nome
                        };
            return query.AsNoTracking();
        }
        public string GetNomeById(uint id)
        {
            return _context.Eventos
                   .Where(t => t.Id == id)
                   .Select(t => t.Nome)
                   .FirstOrDefault();
        }
		// No EventoService
		public IEnumerable<Areainteresse> GetAreasInteresseByEventoId(uint eventoId)
		{
			var evento = _context.Eventos
								 .Include(e => e.IdAreaInteresses) // Carrega as áreas de interesse associadas
								 .FirstOrDefault(e => e.Id == eventoId);

			return evento?.IdAreaInteresses ?? Enumerable.Empty<Areainteresse>();
		}
		public void AtualizarVagasDisponiveis(uint idEvento)
        {
            
            var evento = _context.Eventos
                .Include(e => e.Inscricaopessoaeventos)
                .FirstOrDefault(e => e.Id == idEvento);

            if (evento != null)
            {
                
                var quantidadeParticipantes = evento.Inscricaopessoaeventos
                    .Count(i => i.IdPapel == 4); 

                evento.VagasDisponiveis = evento.VagasOfertadas - quantidadeParticipantes;

                _context.SaveChanges();
            }
        }

    }
}
