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
            var evento = _context.Eventos.Find(Id);
            if (evento != null)
            {
                _context.Eventos.Remove(evento);
                _context.SaveChanges();
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
            var eventoExistente = _context.Eventos
                .Include(e => e.IdAreaInteresses)
                .FirstOrDefault(e => e.Id == evento.Id);
            if (eventoExistente == null)
            {
                throw new Exception("Evento não encontrado.");
            }
            var areasParaRemover = eventoExistente.IdAreaInteresses
                .Where(ai => !novosIdsAreaInteresse.Contains(ai.Id))
                .ToList();

            foreach (var area in areasParaRemover)
            {
                eventoExistente.IdAreaInteresses.Remove(area);
            }
            var areasParaAdicionar = novosIdsAreaInteresse
                .Where(id => !eventoExistente.IdAreaInteresses.Any(ai => ai.Id == id))
                .Select(id => _context.Areainteresses.Find(id))
                .ToList();
            foreach (var area in areasParaAdicionar)
            {
                eventoExistente.IdAreaInteresses.Add(area);
            }
            eventoExistente.ImagemPortal = evento.ImagemPortal;
            _context.Update(eventoExistente);
            _context.SaveChanges();
        }

        /// <summary>
        /// Busca um evento
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Evento Get(uint id)
        {
            return _context.Eventos.Find(id);
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
