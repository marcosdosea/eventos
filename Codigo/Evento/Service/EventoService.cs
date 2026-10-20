using Core;
using Core.Service;
using Core.DTO;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
        public uint Create(Evento evento, List<uint> idsAreaInteresse = null)
        {
            if (idsAreaInteresse != null && idsAreaInteresse.Any())
            {
                var areas = _context.Areainteresses.Where(a => idsAreaInteresse.Contains(a.Id)).ToList();
                foreach (var area in areas)
                {
                    evento.IdAreaInteresses.Add(area);
                }
            }
            
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
                    .ThenInclude(i => i.Pagamentos)
                .Include(e => e.Subeventos)
                    .ThenInclude(s => s.Inscricaopessoasubeventos)
                .Include(e => e.Subeventos)
                    .ThenInclude(s => s.Participacaopessoasubeventos)
                .Include(e => e.Subeventos)
                    .ThenInclude(s => s.IdTipoInscricaos)
                .Include(e => e.Modelocertificados)
                .Include(e => e.Modelocrachas)
                .Include(e => e.Participacaopessoaeventos)
                .Include(e => e.Tipoinscricaos)
                    .ThenInclude(t => t.IdSubEventos)
                .FirstOrDefault(e => e.Id == Id);

            if (evento != null)
            {
                try
                {
                    evento.IdAreaInteresses.Clear();

                    if (evento.Subeventos != null)
                    {
                        foreach (var sub in evento.Subeventos)
                        {
                            if (sub.Inscricaopessoasubeventos != null && sub.Inscricaopessoasubeventos.Any())
                            {
                                _context.Inscricaopessoasubeventos.RemoveRange(sub.Inscricaopessoasubeventos);
                            }

                            if (sub.Participacaopessoasubeventos != null && sub.Participacaopessoasubeventos.Any())
                            {
                                _context.Participacaopessoasubeventos.RemoveRange(sub.Participacaopessoasubeventos);
                            }

                            sub.IdTipoInscricaos?.Clear();
                        }
                    }

                    if (evento.Tipoinscricaos != null)
                    {
                        foreach (var tipo in evento.Tipoinscricaos)
                        {
                            tipo.IdSubEventos?.Clear();
                        }
                    }

                    if (evento.Inscricaopessoaeventos != null)
                    {
                        foreach (var inscricao in evento.Inscricaopessoaeventos)
                        {
                            if (inscricao.Pagamentos != null && inscricao.Pagamentos.Any())
                            {
                                _context.Pagamentos.RemoveRange(inscricao.Pagamentos);
                            }
                        }
                        _context.Inscricaopessoaeventos.RemoveRange(evento.Inscricaopessoaeventos);
                    }

                    if (evento.Subeventos != null && evento.Subeventos.Any())
                    {
                        _context.Subeventos.RemoveRange(evento.Subeventos);
                    }

                    if (evento.Tipoinscricaos != null && evento.Tipoinscricaos.Any())
                    {
                        _context.Tipoinscricaos.RemoveRange(evento.Tipoinscricaos);
                    }

                    if (evento.Modelocertificados != null && evento.Modelocertificados.Any())
                    {
                        _context.Modelocertificados.RemoveRange(evento.Modelocertificados);
                    }

                    if (evento.Modelocrachas != null && evento.Modelocrachas.Any())
                    {
                        _context.Modelocrachas.RemoveRange(evento.Modelocrachas);
                    }

                    if (evento.Participacaopessoaeventos != null && evento.Participacaopessoaeventos.Any())
                    {
                        _context.Participacaopessoaeventos.RemoveRange(evento.Participacaopessoaeventos);
                    }

                    _context.Eventos.Remove(evento);

                    _context.SaveChanges();
                }
                catch (ServiceException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ServiceException("Não foi possível excluir o evento. Verifique se existem dependências ativas.", ex);
                }
            }
        }

        /// <summary>
        /// Edita um evento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool Edit(Evento evento, List<uint> novosIdsAreaInteresse)
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

                evento.FrequenciaMinimaCertificado = Math.Min(evento.FrequenciaMinimaCertificado, 100);
                evento.ValorInscricao = Math.Min(evento.ValorInscricao, 99999999);

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
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Erro ao atualizar o evento: {ex.Message}", ex);
                return false;
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
            return _context.Eventos
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new EventoSimpleDTO
                {
                    Id = e.Id,
                    Nome = e.Nome
                })
                .FirstOrDefault();
        }

        public IEnumerable<EventoSimpleDTO> GetAllSimpleDto()
        {
            return _context.Eventos
                .AsNoTracking()
                .OrderBy(e => e.Nome)
                .Select(e => new EventoSimpleDTO
                {
                    Id = e.Id,
                    Nome = e.Nome
                })
                .ToList();
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
            var pessoa = _context.Pessoas.AsNoTracking().FirstOrDefault(p => p.Cpf == userCpf);
            if (pessoa == null)
            {
                throw new ServiceException("Pessoa não encontrada para o CPF fornecido.");
            }
            var eventos = from evento in _context.Eventos.AsNoTracking()
                join inscricao in _context.Inscricaopessoaeventos.AsNoTracking()
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
                   .AsNoTracking()
                   .Where(t => t.Id == id)
                   .Select(t => t.Nome)
                   .FirstOrDefault();
        }
		public IEnumerable<Areainteresse> GetAreasInteresseByEventoId(uint eventoId)
		{
			var evento = _context.Eventos
								 .AsNoTracking()
								 .Include(e => e.IdAreaInteresses)
								 .FirstOrDefault(e => e.Id == eventoId);

			return evento?.IdAreaInteresses ?? Enumerable.Empty<Areainteresse>();
		}
		public void AtualizarVagasDisponiveis(uint idEvento, bool saveChanges = true)
        {
            
            var evento = _context.Eventos
                .Include(e => e.Inscricaopessoaeventos)
                .FirstOrDefault(e => e.Id == idEvento);

            if (evento != null)
            {
                
                var quantidadeParticipantes = evento.Inscricaopessoaeventos
                    .Count(i => i.IdPapel == 4); 

                evento.VagasDisponiveis = evento.VagasOfertadas - quantidadeParticipantes;

                if (saveChanges)
                {
                    _context.SaveChanges();
                }
            }
        }

        public IEnumerable<Evento> Search(EventoFilterDTO filter, int pagina, int tamanhoPagina, out int totalRegistros)
        {
            var query = _context.Eventos
                .AsNoTracking()
                .Include(e => e.IdTipoEventoNavigation)
                .Include(e => e.IdAreaInteresses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.TermoBusca))
            {
                var termos = filter.TermoBusca.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var termo in termos)
                {
                    var termoLike = $"%{termo}%";
                    query = query.Where(e => 
                        EF.Functions.Like(e.Nome, termoLike) || 
                        EF.Functions.Like(e.Descricao, termoLike) ||
                        (e.IdTipoEventoNavigation != null && EF.Functions.Like(e.IdTipoEventoNavigation.Nome, termoLike)) ||
                        e.IdAreaInteresses.Any(ai => EF.Functions.Like(ai.Nome, termoLike))
                    );
                }
            }

            if (filter.IdAreaInteresse.HasValue)
            {
                query = query.Where(e => e.IdAreaInteresses.Select(ai => ai.Id).Contains(filter.IdAreaInteresse.Value));
            }

            if (filter.IdTipoEvento.HasValue)
            {
                query = query.Where(e => e.IdTipoEvento == filter.IdTipoEvento.Value);
            }

            if (filter.Data.HasValue)
            {
                query = query.Where(e => e.DataInicio.HasValue && e.DataInicio.Value.Date >= filter.Data.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(filter.Estado))
            {
                var estadoLimpo = filter.Estado.Trim();
                query = query.Where(e => e.Estado != null && e.Estado.Trim() == estadoLimpo);
            }

            if (!string.IsNullOrWhiteSpace(filter.Cidade))
            {
                query = query.Where(e => e.Cidade == filter.Cidade);
            }

            if (filter.PossuiSubeventos.HasValue && filter.PossuiSubeventos.Value)
            {
                query = query.Where(e => e.Subeventos.Any());
            }

            query = query.Where(e => e.Status == "A" && e.DataFimInscricao >= DateTime.Now);

            totalRegistros = query.Count();

            // Paginacao (ordenando para garantir resultados consistentes)
            return query.OrderBy(e => e.Id)
                        .Skip((pagina - 1) * tamanhoPagina)
                        .Take(tamanhoPagina)
                        .AsNoTracking()
                        .ToList();
        }

        public uint VerificarPerfilAtual(IList<string> roles, String perfilAtivo, uint idPapel)
        {
            if (!string.IsNullOrEmpty(perfilAtivo))
            {

                if (perfilAtivo == "ADMINISTRADOR")
                {
                    idPapel = 1;
                }
                else if (perfilAtivo == "GESTOR")
                {
                    idPapel = 2;
                }
                else if (perfilAtivo == "COLABORADOR")
                {
                    idPapel = 3;


                }
            }
            else
            {
                if (roles.Contains("ADMINISTRADOR"))
                {
                    idPapel = 1;
                }
                else if (roles.Contains("GESTOR"))
                {
                    idPapel = 2;
                }
                else if (roles.Contains("COLABORADOR"))
                {
                    idPapel = 3;
                }   
            }
            
            return idPapel;
        }

        private IQueryable<Inscricaopessoaevento> QueryParticipantes(uint idEvento, DashboardFiltrosDTO filtros)
        {
            var query = _context.Inscricaopessoaeventos
                .AsNoTracking()
                .Where(i => i.IdEvento == idEvento && i.IdPapel == 4);

            if (filtros.IdTipoInscricao.HasValue)
                query = query.Where(i => i.IdTipoInscricao == filtros.IdTipoInscricao.Value);

            if (filtros.DataInicio.HasValue)
                query = query.Where(i => i.DataInscricao >= filtros.DataInicio.Value);

            if (filtros.DataFim.HasValue)
            {
                var fim = filtros.DataFim.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.DataInscricao <= fim);
            }

            if (filtros.IdSubevento.HasValue)
            {
                var idSub = filtros.IdSubevento.Value;
                var pessoasSub = _context.Inscricaopessoasubeventos
                    .AsNoTracking()
                    .Where(s => s.IdSubEvento == idSub && s.IdPapel == 4)
                    .Select(s => s.IdPessoa)
                    .Distinct();
                query = query.Where(i => pessoasSub.Contains(i.IdPessoa));
            }

            return query;
        }

        /// <summary>
        /// Aba 1 do Dashboard (Visão Geral &amp; Finanças) - Issue #727.
        /// Agrega inscrições de participantes (IdPapel=4) + pagamentos.
        /// Status inscrição: A=Ativa, S=Solicitada, C=Cancelada.
        /// Status pagamento: C=Confirmado, S=Solicitado. Forma: P/C/B/D.
        /// </summary>
        public DashboardFinanceiroDTO GetDashboardFinanceiro(uint idEvento, DashboardFiltrosDTO filtros)
        {
            filtros ??= new DashboardFiltrosDTO();
            var resultado = new DashboardFinanceiroDTO();

            var evento = _context.Eventos.AsNoTracking().FirstOrDefault(e => e.Id == idEvento);
            if (evento == null)
                throw new ServiceException("Evento não encontrado.");

            resultado.VagasOfertadas = evento.VagasOfertadas;

            var inscricoes = QueryParticipantes(idEvento, filtros).ToList();
            var total = inscricoes.Count;
            resultado.TotalInscritos = total;
            resultado.PercentualOcupacao = evento.VagasOfertadas > 0
                ? Math.Round((decimal)total * 100 / evento.VagasOfertadas, 2)
                : 0;

            // Funil A/C/S
            var ativas = inscricoes.Count(i => i.Status == "A");
            var solicitadas = inscricoes.Count(i => i.Status == "S");
            var canceladas = inscricoes.Count(i => i.Status == "C");
            resultado.Funil = new FunilInscricoesDTO
            {
                Ativas = ativas,
                AtivasPercentual = total > 0 ? Math.Round((decimal)ativas * 100 / total, 1) : 0,
                Solicitadas = solicitadas,
                SolicitadasPercentual = total > 0 ? Math.Round((decimal)solicitadas * 100 / total, 1) : 0,
                Canceladas = canceladas,
                CanceladasPercentual = total > 0 ? Math.Round((decimal)canceladas * 100 / total, 1) : 0
            };

            // Por categoria (TipoInscricao)
            var tipos = _context.Tipoinscricaos
                .AsNoTracking()
                .Where(t => t.IdEvento == idEvento)
                .ToDictionary(t => t.Id, t => t.Nome);
            resultado.PorCategoria = inscricoes
                .GroupBy(i => i.IdTipoInscricao)
                .Select(g => new CategoriaInscricaoDTO
                {
                    IdTipoInscricao = g.Key,
                    Nome = g.Key.HasValue && tipos.ContainsKey(g.Key.Value)
                        ? tipos[g.Key.Value]
                        : "Sem categoria",
                    Quantidade = g.Count(),
                    Percentual = total > 0 ? Math.Round((decimal)g.Count() * 100 / total, 1) : 0,
                    ValorTotal = g.Sum(x => x.ValorTotal)
                })
                .OrderByDescending(c => c.Quantidade)
                .ToList();

            // Pagamentos das inscrições filtradas
            var idsInscricoes = inscricoes.Select(i => i.Id).ToList();
            var pagamentos = idsInscricoes.Count == 0
                ? new List<Pagamento>()
                : _context.Pagamentos
                    .AsNoTracking()
                    .Where(p => idsInscricoes.Contains(p.IdInscricaoPessoaEvento))
                    .ToList();

            var confirmados = pagamentos.Where(p => p.Status == "C").ToList();
            var emAberto = pagamentos.Where(p => p.Status == "S").ToList();

            resultado.ReceitaConfirmada = confirmados.Sum(p => p.Valor);
            resultado.ValorEmAberto = emAberto.Sum(p => p.Valor);
            resultado.PagamentosEmAberto = emAberto.Count;
            resultado.TicketMedio = total > 0
                ? Math.Round(resultado.ReceitaConfirmada / total, 2)
                : 0;

            static string NomeForma(string forma) => forma switch
            {
                "P" => "PIX Instantâneo",
                "C" => "Cartão de Crédito",
                "B" => "Boleto Bancário",
                "D" => "Dinheiro",
                _ => forma
            };

            resultado.PorForma = confirmados
                .GroupBy(p => p.Forma)
                .Select(g => new FormaPagamentoDTO
                {
                    Forma = g.Key,
                    NomeExibicao = NomeForma(g.Key),
                    Quantidade = g.Count(),
                    ValorTotal = g.Sum(p => p.Valor),
                    Percentual = resultado.ReceitaConfirmada > 0
                        ? Math.Round(g.Sum(p => p.Valor) * 100 / resultado.ReceitaConfirmada, 1)
                        : 0
                })
                .OrderByDescending(f => f.ValorTotal)
                .ToList();

            return resultado;
        }

        /// <summary>
        /// Aba 2 do Dashboard (Operação &amp; Presença) - Issue #727.
        /// Presentes via Participacao (evento ou subevento filtrado),
        /// aptos via FrequenciaFinal &gt;= FrequenciaMinimaCertificado,
        /// demografia via Pessoa.Sexo/Estado.
        /// </summary>
        public DashboardOperacaoDTO GetDashboardOperacao(uint idEvento, DashboardFiltrosDTO filtros)
        {
            filtros ??= new DashboardFiltrosDTO();
            var resultado = new DashboardOperacaoDTO();

            var evento = _context.Eventos.AsNoTracking().FirstOrDefault(e => e.Id == idEvento);
            if (evento == null)
                throw new ServiceException("Evento não encontrado.");

            var inscricoes = QueryParticipantes(idEvento, filtros).ToList();
            var total = inscricoes.Count;
            var idsPessoas = inscricoes.Select(i => i.IdPessoa).Distinct().ToList();

            // Presentes: check-in registrado. Com filtro de subevento, usa participação do subevento.
            List<uint> presentesIds;
            if (filtros.IdSubevento.HasValue)
            {
                var idSub = filtros.IdSubevento.Value;
                presentesIds = _context.Participacaopessoasubeventos
                    .AsNoTracking()
                    .Where(p => p.IdSubEvento == idSub && idsPessoas.Contains(p.IdPessoa))
                    .Select(p => p.IdPessoa)
                    .Distinct()
                    .ToList();
            }
            else
            {
                presentesIds = _context.Participacaopessoaeventos
                    .AsNoTracking()
                    .Where(p => p.IdEvento == idEvento && idsPessoas.Contains(p.IdPessoa))
                    .Select(p => p.IdPessoa)
                    .Distinct()
                    .ToList();
            }
            resultado.Presentes = presentesIds.Count;
            resultado.Ausentes = Math.Max(0, total - resultado.Presentes);

            // Aptos a certificado + frequência média (base: inscrições ativas)
            var ativas = inscricoes.Where(i => i.Status == "A").ToList();
            var baseCert = ativas.Count > 0 ? ativas : inscricoes;
            resultado.AptosCertificado = baseCert.Count(i => i.FrequenciaFinal >= evento.FrequenciaMinimaCertificado);
            resultado.FrequenciaMedia = baseCert.Count > 0
                ? Math.Round(baseCert.Average(i => i.FrequenciaFinal), 2)
                : 0;

            // Ocupação por subevento (exclui canceladas)
            var subeventos = _context.Subeventos
                .AsNoTracking()
                .Where(s => s.IdEvento == idEvento)
                .OrderBy(s => s.Nome)
                .ToList();
            if (filtros.IdSubevento.HasValue)
                subeventos = subeventos.Where(s => s.Id == filtros.IdSubevento.Value).ToList();

            var ocupacao = new List<OcupacaoSubeventoDTO>();
            foreach (var sub in subeventos)
            {
                var q = _context.Inscricaopessoasubeventos
                    .AsNoTracking()
                    .Where(s => s.IdSubEvento == sub.Id && s.IdPapel == 4 && s.Status != "C");
                if (filtros.IdTipoInscricao.HasValue)
                    q = q.Where(s => s.IdTipoInscricao == filtros.IdTipoInscricao.Value);
                if (filtros.DataInicio.HasValue)
                    q = q.Where(s => s.DataInscricao >= filtros.DataInicio.Value);
                if (filtros.DataFim.HasValue)
                {
                    var fim = filtros.DataFim.Value.Date.AddDays(1).AddTicks(-1);
                    q = q.Where(s => s.DataInscricao <= fim);
                }
                var ocupados = q.Select(s => s.IdPessoa).Distinct().Count();
                var vagas = (int)sub.VagasOfertadas;
                ocupacao.Add(new OcupacaoSubeventoDTO
                {
                    IdSubevento = sub.Id,
                    Nome = sub.Nome,
                    Ocupados = ocupados,
                    VagasOfertadas = vagas,
                    Percentual = vagas > 0 ? Math.Round((decimal)ocupados * 100 / vagas, 1) : 0
                });
            }
            resultado.OcupacaoSubeventos = ocupacao;

            // Demografia: sexo + origem (top 3 UFs + Outros)
            if (idsPessoas.Count == 0)
                return resultado;

            var pessoas = _context.Pessoas
                .AsNoTracking()
                .Where(p => idsPessoas.Contains(p.Id))
                .Select(p => new { p.Sexo, p.Estado })
                .ToList();

            var denominador = pessoas.Count;
            var fem = pessoas.Count(p => p.Sexo == "F");
            var masc = pessoas.Count(p => p.Sexo == "M");
            resultado.PorSexo = new DistribuicaoSexoDTO
            {
                Feminino = fem,
                FemininoPercentual = denominador > 0 ? Math.Round((decimal)fem * 100 / denominador, 1) : 0,
                Masculino = masc,
                MasculinoPercentual = denominador > 0 ? Math.Round((decimal)masc * 100 / denominador, 1) : 0,
                NaoInformado = Math.Max(0, denominador - fem - masc)
            };

            var porUf = pessoas
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Estado) ? "NI" : p.Estado!.Trim().ToUpper())
                .Select(g => new { Estado = g.Key, Qtd = g.Count() })
                .OrderByDescending(g => g.Qtd)
                .ToList();
            var top = porUf.Take(3).ToList();
            var resto = porUf.Skip(3).Sum(g => g.Qtd);
            var origem = top.Select(g => new OrigemGeograficaDTO
            {
                Estado = g.Estado,
                Quantidade = g.Qtd,
                Percentual = denominador > 0 ? Math.Round((decimal)g.Qtd * 100 / denominador, 1) : 0
            }).ToList();
            if (resto > 0)
                origem.Add(new OrigemGeograficaDTO
                {
                    Estado = "Outros",
                    Quantidade = resto,
                    Percentual = denominador > 0 ? Math.Round((decimal)resto * 100 / denominador, 1) : 0
                });
            resultado.PorOrigem = origem;

            return resultado;
        }
    }
}
