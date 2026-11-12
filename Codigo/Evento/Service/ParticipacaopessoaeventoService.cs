using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Service;
using Util;

namespace Core.Service
{
    public class ParticipacaoPessoaEventoService : IParticipacaoPessoaEventoService
    {
        private const int PapelParticipante = 4;

        private readonly EventoContext _context;
        private readonly IEventoService _eventoService;
        private readonly ISubeventoService _subeventoService;

        public ParticipacaoPessoaEventoService(
            EventoContext context,
            IEventoService? eventoService = null,
            ISubeventoService? subeventoService = null)
        {
            _context = context;
            _eventoService = eventoService ?? new EventoService(context);
            _subeventoService = subeventoService ?? new SubeventoService(context);
        }

        private void RecalcularFrequenciaSeFinalizado(uint idEvento, uint idPessoa)
        {
            var evento = _context.Eventos.Find(idEvento);
            if (evento?.Status == "F")
            {
                _eventoService.AtualizarFrequenciaFinal(idEvento, idPessoa, saveChanges: false);
            }
        }

        public async Task<List<Participacaopessoaevento>> GetAllAsync()
        {
            return await _context.Participacaopessoaeventos
                .Include(p => p.IdPessoaNavigation)
                .ToListAsync();
        }

        public async Task<List<Participacaopessoaevento>> GetByEventoAsync(uint idEvento)
        {
            return await _context.Participacaopessoaeventos
                .Include(p => p.IdPessoaNavigation)
                .Where(p => p.IdEvento == idEvento)
                .OrderByDescending(p => p.Entrada)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Participacaopessoaevento> GetByIdAsync(uint id)
        {
            return await _context.Participacaopessoaeventos.FindAsync(id);
        }

        public async Task<Participacaopessoaevento> AddAsync(Participacaopessoaevento entity)
        {
            var result = (await _context.Participacaopessoaeventos.AddAsync(entity)).Entity;
            RecalcularFrequenciaSeFinalizado(entity.IdEvento, entity.IdPessoa);
            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<bool> UpdateAsync(Participacaopessoaevento entity)
        {
            var existing = await _context.Participacaopessoaeventos.FindAsync(entity.Id);
            if (existing == null)
                return false;

            var eventoAnterior = _context.Entry(existing).Property(p => p.IdEvento).OriginalValue;
            var pessoaAnterior = _context.Entry(existing).Property(p => p.IdPessoa).OriginalValue;
            existing.IdPessoa = entity.IdPessoa;
            existing.IdEvento = entity.IdEvento;
            existing.Entrada = entity.Entrada;
            existing.Saida = entity.Saida;

            _context.Participacaopessoaeventos.Update(existing);
            RecalcularFrequenciaSeFinalizado(eventoAnterior, pessoaAnterior);
            if (eventoAnterior != existing.IdEvento || pessoaAnterior != existing.IdPessoa)
                RecalcularFrequenciaSeFinalizado(existing.IdEvento, existing.IdPessoa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(uint id)
        {
            var existing = await _context.Participacaopessoaeventos.FindAsync(id);
            if (existing == null)
                return false;

            _context.Participacaopessoaeventos.Remove(existing);
            RecalcularFrequenciaSeFinalizado(existing.IdEvento, existing.IdPessoa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ResultadoImportacaoParticipantesDTO> ImportarParticipantesAsync(
            uint idEvento,
            uint? idSubEvento,
            IEnumerable<PessoaInscritaDto> participantes)
        {
            var lista = participantes?.ToList() ?? new List<PessoaInscritaDto>();
            var resultado = new ResultadoImportacaoParticipantesDTO
            {
                TotalLidos = lista.Count
            };

            if (lista.Count == 0)
            {
                return resultado;
            }

            IDbContextTransaction? transaction = null;
            if (_context.Database.IsRelational())
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                var cpfsUnicosNoLote = new HashSet<string>(StringComparer.Ordinal);
                var itensParaProcessar = new List<(string Cpf, PessoaInscritaDto Dto)>();

                foreach (var item in lista)
                {
                    var cpfNormalizado = CpfValidator.Normalizar(item.Cpf);
                    if (string.IsNullOrWhiteSpace(cpfNormalizado))
                    {
                        resultado.TotalFalhas++;
                        resultado.Inconsistencias.Add("CPF ausente ou em formato inválido.");
                        continue;
                    }

                    if (!cpfsUnicosNoLote.Add(cpfNormalizado))
                    {
                        resultado.TotalJaRegistrados++;
                        continue;
                    }

                    itensParaProcessar.Add((cpfNormalizado, item));
                }

                var cpfsBusca = itensParaProcessar.Select(x => x.Cpf).ToList();
                var pessoasExistentes = await _context.Pessoas
                    .Where(p => cpfsBusca.Contains(p.Cpf))
                    .ToDictionaryAsync(p => p.Cpf, StringComparer.Ordinal);

                bool novasPessoasCriadas = false;
                foreach (var (cpf, dto) in itensParaProcessar)
                {
                    if (!pessoasExistentes.ContainsKey(cpf))
                    {
                        var nome = string.IsNullOrWhiteSpace(dto.Nome) ? "Participante" : dto.Nome.Trim();
                        if (nome.Length > 50)
                            nome = nome.Substring(0, 50);

                        var nomeCracha = nome.Length > 20 ? nome.Substring(0, 20) : nome;
                        var email = string.IsNullOrWhiteSpace(dto.Email) ? $"{cpf}@evento.com" : dto.Email.Trim();
                        if (email.Length > 50)
                            email = email.Substring(0, 50);

                        var novaPessoa = new Pessoa
                        {
                            Cpf = cpf,
                            Nome = nome,
                            NomeCracha = nomeCracha,
                            Email = email,
                            Sexo = "N"
                        };

                        _context.Pessoas.Add(novaPessoa);
                        pessoasExistentes[cpf] = novaPessoa;
                        novasPessoasCriadas = true;
                    }
                }

                if (novasPessoasCriadas)
                {
                    await _context.SaveChangesAsync();
                }

                var idsPessoas = pessoasExistentes.Values.Select(p => p.Id).ToList();

                var inscricoesEvento = await _context.Inscricaopessoaeventos
                    .Where(i => i.IdEvento == idEvento && idsPessoas.Contains(i.IdPessoa))
                    .ToListAsync();

                var inscricoesSubEvento = idSubEvento.HasValue && idSubEvento.Value > 0
                    ? await _context.Inscricaopessoasubeventos
                        .Where(i => i.IdSubEvento == idSubEvento.Value && idsPessoas.Contains(i.IdPessoa))
                        .ToListAsync()
                    : new List<Inscricaopessoasubevento>();

                var participacoesAbertasEvento = !(idSubEvento.HasValue && idSubEvento.Value > 0)
                    ? (await _context.Participacaopessoaeventos
                        .Where(p => p.IdEvento == idEvento && p.Saida == null && idsPessoas.Contains(p.IdPessoa))
                        .Select(p => p.IdPessoa)
                        .ToListAsync()).ToHashSet()
                    : new HashSet<uint>();

                var participacoesAbertasSubEvento = idSubEvento.HasValue && idSubEvento.Value > 0
                    ? (await _context.Participacaopessoasubeventos
                        .Where(p => p.IdSubEvento == idSubEvento.Value && p.Saida == null && idsPessoas.Contains(p.IdPessoa))
                        .Select(p => p.IdPessoa)
                        .ToListAsync()).ToHashSet()
                    : new HashSet<uint>();

                var agora = DateTime.Now;
                bool houveNovaInscricaoEvento = false;
                bool houveNovaInscricaoSubEvento = false;

                foreach (var (cpf, _) in itensParaProcessar)
                {
                    if (!pessoasExistentes.TryGetValue(cpf, out var pessoa) || pessoa.Id == 0)
                    {
                        resultado.TotalFalhas++;
                        continue;
                    }

                    var inscricaoExistente = inscricoesEvento.FirstOrDefault(i => i.IdPessoa == pessoa.Id);
                    if (inscricaoExistente != null && inscricaoExistente.IdPapel != PapelParticipante)
                    {
                        resultado.TotalFalhas++;
                        resultado.Inconsistencias.Add($"CPF {cpf}: pessoa já vinculada ao evento em outro papel.");
                        continue;
                    }

                    if (inscricaoExistente == null)
                    {
                        var novaInscricao = new Inscricaopessoaevento
                        {
                            IdPessoa = pessoa.Id,
                            IdEvento = idEvento,
                            IdPapel = PapelParticipante,
                            DataInscricao = agora,
                            NomeCracha = !string.IsNullOrWhiteSpace(pessoa.NomeCracha) ? pessoa.NomeCracha : pessoa.Nome,
                            Status = "S"
                        };
                        _context.Inscricaopessoaeventos.Add(novaInscricao);
                        inscricoesEvento.Add(novaInscricao);
                        houveNovaInscricaoEvento = true;
                    }

                    if (idSubEvento.HasValue && idSubEvento.Value > 0)
                    {
                        var inscricaoSubExistente = inscricoesSubEvento.FirstOrDefault(i => i.IdPessoa == pessoa.Id);
                        if (inscricaoSubExistente != null && inscricaoSubExistente.IdPapel != PapelParticipante)
                        {
                            resultado.TotalFalhas++;
                            resultado.Inconsistencias.Add($"CPF {cpf}: pessoa já vinculada ao subevento em outro papel.");
                            continue;
                        }

                        if (inscricaoSubExistente == null)
                        {
                            var novaInscricaoSub = new Inscricaopessoasubevento
                            {
                                IdPessoa = pessoa.Id,
                                IdSubEvento = idSubEvento.Value,
                                IdPapel = PapelParticipante,
                                DataInscricao = agora,
                                Status = "S",
                                FrequenciaFinal = 0m,
                                Valor = 0m
                            };
                            _context.Inscricaopessoasubeventos.Add(novaInscricaoSub);
                            inscricoesSubEvento.Add(novaInscricaoSub);
                            houveNovaInscricaoSubEvento = true;
                        }

                        if (participacoesAbertasSubEvento.Contains(pessoa.Id))
                        {
                            resultado.TotalJaRegistrados++;
                            continue;
                        }

                        var novaParticipacaoSub = new Participacaopessoasubevento
                        {
                            IdPessoa = pessoa.Id,
                            IdSubEvento = idSubEvento.Value,
                            Entrada = agora
                        };
                        _context.Participacaopessoasubeventos.Add(novaParticipacaoSub);
                        participacoesAbertasSubEvento.Add(pessoa.Id);
                        resultado.TotalImportados++;
                    }
                    else
                    {
                        if (participacoesAbertasEvento.Contains(pessoa.Id))
                        {
                            resultado.TotalJaRegistrados++;
                            continue;
                        }

                        var novaParticipacao = new Participacaopessoaevento
                        {
                            IdPessoa = pessoa.Id,
                            IdEvento = idEvento,
                            Entrada = agora
                        };
                        _context.Participacaopessoaeventos.Add(novaParticipacao);
                        participacoesAbertasEvento.Add(pessoa.Id);
                        resultado.TotalImportados++;
                    }
                }

                if (houveNovaInscricaoEvento)
                {
                    _eventoService.AtualizarVagasDisponiveis(idEvento, saveChanges: false);
                }

                if (houveNovaInscricaoSubEvento && idSubEvento.HasValue && idSubEvento.Value > 0)
                {
                    _subeventoService.AtualizarVagasDisponiveis(idSubEvento.Value, saveChanges: false);
                }

                var evento = await _context.Eventos.FindAsync(idEvento);
                if (evento?.Status == "F" && resultado.TotalImportados > 0)
                {
                    _eventoService.AtualizarFrequenciaFinal(idEvento, saveChanges: false);
                }

                if (idSubEvento.HasValue && idSubEvento.Value > 0 && resultado.TotalImportados > 0)
                {
                    var subevento = await _context.Subeventos.FindAsync(idSubEvento.Value);
                    if (subevento != null && (subevento.Status == "F" || evento?.Status == "F"))
                    {
                        _subeventoService.AtualizarFrequenciaFinal(idSubEvento.Value, saveChanges: false);
                    }
                }

                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return resultado;
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }
    }
}
