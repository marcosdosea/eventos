using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service
{
    public class SubeventoService : ISubeventoService
    {
        private const int PapelParticipante = 4;

        private readonly EventoContext _context;

        public SubeventoService(EventoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria um novo subevento para o evento
        /// </summary>
        /// <param name="subevento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public uint Create(Subevento subevento)
        {
            _context.Add(subevento);
            _context.SaveChanges();
            return subevento.Id;
        }

        /// <summary>
        /// Deleta um subevento do evento
        /// </summary>
        /// <param name="subevento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Delete(uint id)
        {
            var subevento = _context.Subeventos
                .Include(s => s.Inscricaopessoasubeventos)
                .Include(s => s.Participacaopessoasubeventos)
                .Include(s => s.IdTipoInscricaos) // Se for muitos-para-muitos
                .FirstOrDefault(s => s.Id == id);

            if (subevento != null)
            {
                try
                {
                    if (subevento.Inscricaopessoasubeventos != null && subevento.Inscricaopessoasubeventos.Any())
                    {
                        _context.Inscricaopessoasubeventos.RemoveRange(subevento.Inscricaopessoasubeventos);
                    }

                    if (subevento.Participacaopessoasubeventos != null && subevento.Participacaopessoasubeventos.Any())
                    {
                        _context.Participacaopessoasubeventos.RemoveRange(subevento.Participacaopessoasubeventos);
                    }

                    subevento.IdTipoInscricaos?.Clear();
                    _context.Subeventos.Remove(subevento);
                    _context.SaveChanges();
                }
                catch (ServiceException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ServiceException("Não foi possível excluir o subevento. Verifique se existem dependências ativas.", ex);
                }
            }
        }

        /// <summary>
        /// Edita um subevento
        /// </summary>
        /// <param name="subevento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void Edit(Subevento subevento)
        {
            _context.Update(subevento);
            var evento = _context.Eventos.Find(subevento.IdEvento);
            if (subevento.Status == "F" || evento?.Status == "F")
                AtualizarFrequenciaFinal(subevento.Id, saveChanges: false);
            _context.SaveChanges();
        }

        /// <summary>
        /// Busca um subevento
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Subevento Get(uint id)
        {
            return _context.Subeventos.Find(id);
        }

        /// <summary>
        /// Busca todos os subeventos
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Subevento> GetAll()
        {
            return _context.Subeventos.AsNoTracking();
        }

        /// <summary>
        /// Obter eventos que iniciam com o nome
        /// </summary>
        /// <param name="nome">nome do subevento</param>
        /// <returns>lista de subeventos</returns>
        public IEnumerable<SubeventoDTO> GetByNome(string nome)
        {
            var query = from subevento in _context.Subeventos
                        where subevento.Nome.StartsWith(nome)
                        orderby subevento.Nome
                        select new Core.DTO.SubeventoDTO
                        {
                            Id = subevento.Id,
                            Nome = subevento.Nome
                        };
            return query;
        }
        /// <summary>
        /// Obter eventos que iniciam com o nome
        /// </summary>
        /// <param name="nome">nome do subevento</param>
        /// <returns>lista de subeventos</returns>
        public IEnumerable<SubeventoEventoDTO> GetByIdEvento(uint idEvento)
        {
            var query = from subevento in _context.Subeventos
                where subevento.IdEvento == idEvento
                orderby subevento.Nome
                select new SubeventoEventoDTO
                {
                    Id = subevento.Id,
                    Nome = subevento.Nome,
                    VagasOfertadas = subevento.VagasOfertadas,
                    VagasDisponiveis = subevento.VagasDisponiveis,
                    NomeTipoEvento = subevento.IdTipoEventoNavigation.Nome,
                    InscricaoGratuita = subevento.InscricaoGratuita,
                    ValorInscricao = subevento.ValorInscricao,
                    Status = subevento.Status,
                    DataInicioInscricao = subevento.DataInicioInscricao,
                    DataFimInscricao = subevento.DataFimInscricao,
                    DataInicio = subevento.DataInicio,
                    DataFim = subevento.DataFim,
                    CargaHoraria = subevento.CargaHoraria,
                    Descricao = subevento.Descricao
                };

            return query.ToList();
        }

        public void AtualizarVagasDisponiveis(uint idSubevento, bool saveChanges = true)
        {
            var subevento = _context.Subeventos.FirstOrDefault(s => s.Id == idSubevento);

            if (subevento != null)
            {
                // Mesma regra do evento: vagas disponíveis = vagas ofertadas - participantes.
                // A contagem soma o que já está gravado com o que está pendente no
                // ChangeTracker, para funcionar tanto na inscrição (lote) quanto no
                // cancelamento, que rodam dentro da mesma transação.
                var participantesNoBanco = _context.Inscricaopessoasubeventos
                    .Count(i => i.IdSubEvento == idSubevento && i.IdPapel == PapelParticipante);

                var inscricoesPendentes = _context.ChangeTracker
                    .Entries<Inscricaopessoasubevento>()
                    .Count(e => e.State == EntityState.Added
                             && e.Entity.IdSubEvento == idSubevento
                             && e.Entity.IdPapel == PapelParticipante);

                var inscricoesRemovidas = _context.ChangeTracker
                    .Entries<Inscricaopessoasubevento>()
                    .Count(e => e.State == EntityState.Deleted
                             && e.Entity.IdSubEvento == idSubevento
                             && e.Entity.IdPapel == PapelParticipante);

                var quantidadeParticipantes = participantesNoBanco + inscricoesPendentes - inscricoesRemovidas;

                int vagasRestantes = (int)subevento.VagasOfertadas - quantidadeParticipantes;
                subevento.VagasDisponiveis = (uint)(vagasRestantes < 0 ? 0 : vagasRestantes);

                _context.Update(subevento);
                if (saveChanges)
                {
                    _context.SaveChanges();
                }
            }
        }

        public void AtualizarFrequenciaFinal(uint idSubevento, uint? idPessoa = null, bool saveChanges = true)
        {
            var subevento = _context.Subeventos.FirstOrDefault(s => s.Id == idSubevento);
            if (subevento == null)
                return;

            var inscricoes = _context.Inscricaopessoasubeventos
                .Where(i => i.IdSubEvento == idSubevento && i.IdPapel == PapelParticipante && i.Status != "C" && (!idPessoa.HasValue || i.IdPessoa == idPessoa.Value))
                .ToList();

            var rastreadas = _context.ChangeTracker.Entries<Participacaopessoasubevento>().ToList();
            var idsRastreados = rastreadas
                .Where(e => e.State != EntityState.Added)
                .Select(e => e.Entity.Id)
                .ToHashSet();

            var presencasBanco = _context.Participacaopessoasubeventos
                .AsNoTracking()
                .Where(p => p.IdSubEvento == idSubevento && (!idPessoa.HasValue || p.IdPessoa == idPessoa.Value) && !idsRastreados.Contains(p.Id))
                .ToList();

            var presencasMemoria = rastreadas
                .Where(e => e.State != EntityState.Deleted && e.Entity.IdSubEvento == idSubevento && (!idPessoa.HasValue || e.Entity.IdPessoa == idPessoa.Value))
                .Select(e => e.Entity);

            var presencasPorPessoa = presencasBanco
                .Concat(presencasMemoria)
                .GroupBy(p => p.IdPessoa)
                .ToDictionary(g => g.Key, g => g.Select(p => (p.Entrada, p.Saida)).ToList());

            foreach (var inscricao in inscricoes)
            {
                presencasPorPessoa.TryGetValue(inscricao.IdPessoa, out var periodos);
                inscricao.FrequenciaFinal = EventoService.CalcularFrequencia(subevento.DataInicio, subevento.DataFim, subevento.CargaHoraria, periodos ?? new());
            }

            if (saveChanges)
            {
                _context.SaveChanges();
            }
        }
    }
}

