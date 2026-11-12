using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Util;

namespace Service
{
    public class InscricaoService : IInscricaoService
    {
        private readonly EventoContext _context;
        private readonly UserManager<UsuarioIdentity> _userManager;
        private readonly IEventoService _eventoService;
        private readonly ISubeventoService _subeventoService;

        private const int PapelParticipante = 4;
        public InscricaoService(EventoContext context, UserManager<UsuarioIdentity> userManager, IEventoService eventoService, ISubeventoService subeventoService)
        {
            _userManager = userManager;
            _context = context;
            _eventoService = eventoService;
            _subeventoService = subeventoService;
        }

        public bool IsInscrito(uint idPessoa, uint idEvento)
        {
            return _context.Inscricaopessoaeventos
                .Any(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento);
        }

        public uint CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento)
        {
            if (IsInscrito(inscricaopessoaevento.IdPessoa, inscricaopessoaevento.IdEvento))
            {
                return 0;
            }

            _context.Add(inscricaopessoaevento);
            _context.SaveChanges();
            return inscricaopessoaevento.Id;
        }

        public uint CreateInscricaoEventoLote(Inscricaopessoaevento inscricaopessoaevento)
        {
            _context.Add(inscricaopessoaevento);
            _context.SaveChanges();
            return inscricaopessoaevento.Id;
        }

        /// <summary>
        /// Adiciona as inscrições do lote, atualiza as vagas do evento e dos
        /// subeventos afetados e persiste tudo em um único SaveChanges.
        /// </summary>
        public void CreateInscricoesEmLote(uint idEvento, IEnumerable<Inscricaopessoaevento> eventos, IEnumerable<Inscricaopessoasubevento> subeventos, IEnumerable<uint> idsSubeventosAtualizarVagas)
        {
            var eventosList = eventos?.ToList() ?? new List<Inscricaopessoaevento>();
            var subeventosList = subeventos?.ToList() ?? new List<Inscricaopessoasubevento>();

            if (eventosList.Count > 0)
            {
                _context.AddRange(eventosList);
            }
            if (subeventosList.Count > 0)
            {
                _context.AddRange(subeventosList);
            }

            if (eventosList.Count > 0)
            {
                _eventoService.AtualizarVagasDisponiveis(idEvento, saveChanges: false);
            }

            foreach (var idSubevento in (idsSubeventosAtualizarVagas ?? Enumerable.Empty<uint>()).Distinct())
            {
                _subeventoService.AtualizarVagasDisponiveis(idSubevento, saveChanges: false);
            }

            _context.SaveChanges();
        }

        public async Task DeletePessoaPapelAsync(uint idPessoa, uint idEvento, uint idPapel, string cpf)
        {
            var pessoa = await _context.Pessoas.FirstOrDefaultAsync(p => p.Id == idPessoa && p.Cpf == cpf);
            if (pessoa == null)
            {
                throw new Exception("Pessoa não encontrada com o CPF informado.");
            }

            var inscricao = await _context.Inscricaopessoaeventos
                .FirstOrDefaultAsync(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento && i.IdPapel == idPapel);

            if (inscricao != null)
            {
                _context.Inscricaopessoaeventos.Remove(inscricao);
                await _context.SaveChangesAsync();
            }

            var existePapelUsuario = await _context.Inscricaopessoaeventos
                .AnyAsync(i => i.IdPessoa == idPessoa && i.IdPapel == idPapel);

            if (!existePapelUsuario && idPapel != 4)
            {
                await RemoveUserRole(idPessoa, idPapel, cpf);
            }
        }

        public async Task RemoveUserRole(uint idPessoa, uint idPapel, string cpf)
        {
            string cpfSemFormatacao = Methods.RemoveNaoNumericos(cpf);
            var user = await _userManager.FindByNameAsync(cpfSemFormatacao);

            if (user == null)
            {
                throw new Exception("Usuário não encontrado.");
            }

            string role = idPapel switch
            {
                1 => "GESTOR",
                2 => "GESTOR",
                3 => "COLABORADOR",
                _ => throw new ArgumentException("Papel inválido.")
            };

            if (await _userManager.IsInRoleAsync(user, role))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, role);

                if (!removeResult.Succeeded)
                {
                    throw new Exception("Erro ao remover o papel do usuário.");
                }
            }
        }
        public IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel)
        {
            return _context.Inscricaopessoaeventos
                .Include(i => i.IdPessoaNavigation)
                .Where(i => i.IdEvento == idEvento && i.IdPapel == idPapel)
                .AsNoTracking()
                .ToList();
        }


        public int GetPapelPessoaByEvento(uint idPessoa, uint idEvento)
        {
            return _context.Inscricaopessoaeventos
                .Where(i => i.IdEvento == idEvento && i.IdPessoa == idPessoa)
                .Select(i => i.IdPapel)
                .FirstOrDefault();
        }

        public IEnumerable<Inscricaopessoaevento> GetByEvento(uint idEvento)
        {
            return _context.Inscricaopessoaeventos
                .Include(i => i.IdPessoaNavigation)
                .Where(i => i.IdEvento == idEvento)
                .AsNoTracking()
                .ToList();
        }

        public void CreateInscricaoSubEvento(Inscricaopessoasubevento inscricaopessoasubevento)
        {
            _context.Add(inscricaopessoasubevento);
            _context.SaveChanges();
        }

        /// <summary>
        /// Cancela as inscrições de participante (papel 4) de uma pessoa em um evento,
        /// incluindo as inscrições nos subeventos, e libera as vagas do evento e dos
        /// subeventos afetados em um único SaveChanges.
        /// Retorna a quantidade de inscrições do evento removidas.
        /// </summary>
        public int CancelarInscricaoParticipante(uint idPessoa, uint idEvento)
        {
            var inscricoesEvento = _context.Inscricaopessoaeventos
                .Where(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento && i.IdPapel == PapelParticipante)
                .ToList();

            if (!inscricoesEvento.Any())
            {
                return 0;
            }

            var idsSubeventosDoEvento = _context.Subeventos
                .Where(s => s.IdEvento == idEvento)
                .Select(s => s.Id)
                .ToList();

            var inscricoesSub = _context.Inscricaopessoasubeventos
                .Where(s => s.IdPessoa == idPessoa && s.IdPapel == PapelParticipante && idsSubeventosDoEvento.Contains(s.IdSubEvento))
                .ToList();
            var idsSubAfetados = inscricoesSub.Select(s => s.IdSubEvento).Distinct().ToList();

            // Pagamentos referenciam a inscrição (FK RESTRICT): sem remover,
            // a exclusão da inscrição falha por integridade referencial.
            var idsInscricoes = inscricoesEvento.Select(i => i.Id).ToList();
            var pagamentos = idsInscricoes.Count > 0
                ? _context.Pagamentos.Where(p => idsInscricoes.Contains(p.IdInscricaoPessoaEvento)).ToList()
                : new List<Pagamento>();

            _context.Pagamentos.RemoveRange(pagamentos);
            _context.Inscricaopessoasubeventos.RemoveRange(inscricoesSub);
            _context.Inscricaopessoaeventos.RemoveRange(inscricoesEvento);

            _eventoService.AtualizarVagasDisponiveis(idEvento, saveChanges: false);
            foreach (var idSub in idsSubAfetados)
            {
                _subeventoService.AtualizarVagasDisponiveis(idSub, saveChanges: false);
            }

            _context.SaveChanges();

            return inscricoesEvento.Count;
        }

        public IEnumerable<Inscricaopessoasubevento> GetSubByEvento(uint idEvento)
        {
            return _context.Inscricaopessoasubeventos
                .Include(i => i.IdPessoaNavigation)
                .Include(i => i.IdSubEventoNavigation)
                .Where(i => i.IdSubEventoNavigation.IdEvento == idEvento)
                .AsNoTracking()
                .ToList();
        }

        public IEnumerable<Inscricaopessoasubevento> GetAllSubEventsByUserId(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Enumerable.Empty<Inscricaopessoasubevento>();

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var subeventos = _context.Inscricaopessoasubeventos
                .Include(s => s.IdSubEventoNavigation)
                .Include(s => s.IdTipoInscricaoNavigation)
                .Include(s => s.IdPessoaNavigation)
                .Where(s => s.IdPessoaNavigation != null && (s.IdPessoaNavigation.Cpf == cpfParaBusca || s.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && s.IdPessoaNavigation.Cpf == cpfFormatado)))
                .AsNoTracking()
                .ToList();

            foreach (var sub in subeventos)
            {
                if (sub.IdSubEventoNavigation == null)
                {
                    sub.IdSubEventoNavigation = _context.Subeventos.FirstOrDefault(s => s.Id == sub.IdSubEvento)!;
                }
            }

            return subeventos;
        }

        public IEnumerable<Inscricaopessoaevento> GetAllEventsByUserId(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Enumerable.Empty<Inscricaopessoaevento>();

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var subeventosUsuario = GetAllSubEventsByUserId(username).ToList();

            var query = _context.Inscricaopessoaeventos
                .Include(i => i.IdEventoNavigation)
                .Include(i => i.IdPessoaNavigation)
                .Include(i => i.IdTipoInscricaoNavigation)
                .Where(i => i.IdPessoaNavigation != null && (i.IdPessoaNavigation.Cpf == cpfParaBusca || i.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && i.IdPessoaNavigation.Cpf == cpfFormatado)))
                .ToList();

            foreach (var inscricao in query)
            {
                inscricao.Inscricaopessoasubeventos = subeventosUsuario
                    .Where(s => (s.IdSubEventoNavigation?.IdEvento ?? _context.Subeventos.FirstOrDefault(sub => sub.Id == s.IdSubEvento)?.IdEvento) == inscricao.IdEvento)
                    .ToList();
            }

            // Garante que subeventos cujo evento principal não pertence ao usuário (ex: transferido separadamente) continuem aparecendo no painel
            var idsEventosComInscricao = query.Select(i => i.IdEvento).ToHashSet();
            var subeventosSemEventoPrincipal = subeventosUsuario
                .Where(s => !idsEventosComInscricao.Contains(s.IdSubEventoNavigation?.IdEvento ?? _context.Subeventos.FirstOrDefault(sub => sub.Id == s.IdSubEvento)?.IdEvento ?? 0))
                .GroupBy(s => s.IdSubEventoNavigation?.IdEvento ?? _context.Subeventos.FirstOrDefault(sub => sub.Id == s.IdSubEvento)?.IdEvento ?? 0);

            foreach (var grupo in subeventosSemEventoPrincipal)
            {
                var idEv = grupo.Key;
                if (idEv == 0) continue;

                var evNav = _context.Eventos.FirstOrDefault(e => e.Id == idEv);
                if (evNav == null) continue;

                var primeiroSub = grupo.First();
                query.Add(new Inscricaopessoaevento
                {
                    Id = 0, // Id = 0 indica que não possui ingresso de evento principal ativo (apenas subeventos)
                    IdEvento = idEv,
                    IdEventoNavigation = evNav,
                    IdPessoa = primeiroSub.IdPessoa,
                    IdPessoaNavigation = primeiroSub.IdPessoaNavigation,
                    DataInscricao = primeiroSub.DataInscricao,
                    Status = grupo.Any(s => s.Status == "A") ? "A" : grupo.First().Status,
                    ValorTotal = grupo.Sum(s => s.Valor),
                    Inscricaopessoasubeventos = grupo.ToList()
                });
            }

            return query;
        }

        public Inscricaopessoaevento GetGestorInEvent(string username, uint idEvento)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var query = from i in _context.Inscricaopessoaeventos.Include(i => i.IdPessoaNavigation)
                        where i.IdPessoaNavigation != null
                              && (i.IdPessoaNavigation.Cpf == cpfParaBusca || i.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && i.IdPessoaNavigation.Cpf == cpfFormatado))
                              && i.IdPapel == 2 && i.IdEvento == idEvento
                        select i;
            return query.FirstOrDefault();
        }

        public Inscricaopessoaevento GetColaboradorInEvent(string username, uint idEvento)
        {
            if (string.IsNullOrEmpty(username))
                return null;

            var cpfLimpo = Methods.RemoveNaoNumericos(username);
            var cpfParaBusca = string.IsNullOrEmpty(cpfLimpo) ? username : cpfLimpo;
            var cpfFormatado = cpfLimpo.Length == 11 ? Methods.PatternCpf(cpfLimpo) : null;

            var query = from i in _context.Inscricaopessoaeventos.Include(i => i.IdPessoaNavigation)
                        where i.IdPessoaNavigation != null
                              && (i.IdPessoaNavigation.Cpf == cpfParaBusca || i.IdPessoaNavigation.Cpf == username || (cpfFormatado != null && i.IdPessoaNavigation.Cpf == cpfFormatado))
                              && i.IdPapel == 3 && i.IdEvento == idEvento
                        select i;
            return query.FirstOrDefault();
        }

        public void UpdateNomeCracha(uint idPessoa, uint idEvento, string nomeCracha)
        {
            var inscricoes = _context.Inscricaopessoaeventos
                .Where(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento)
                .ToList();

            foreach (var inscricao in inscricoes)
            {
                inscricao.NomeCracha = nomeCracha;
            }

            if (inscricoes.Any())
            {
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Transfere a titularidade de um ingresso de evento ou subevento para outro participante.
        /// Caso o amigo de destino não possua cadastro, ele é cadastrado silenciosamente.
        /// </summary>
        public bool TransferirIngresso(uint idInscricao, string cpfDestino, string nomeDestino, string emailDestino, string cpfOrigem, uint? idSubevento = null)
        {
            if (string.IsNullOrWhiteSpace(cpfOrigem))
                throw new ArgumentException("Usuário de origem não informado.");

            var cpfOrigemLimpo = Methods.RemoveNaoNumericos(cpfOrigem);
            var cpfOrigemFormatado = cpfOrigemLimpo.Length == 11 ? Methods.PatternCpf(cpfOrigemLimpo) : null;

            var pessoaOrigem = _context.Pessoas.FirstOrDefault(p =>
                p.Cpf == cpfOrigemLimpo || p.Cpf == cpfOrigem || (cpfOrigemFormatado != null && p.Cpf == cpfOrigemFormatado));

            if (pessoaOrigem == null)
                throw new ArgumentException("Usuário de origem não encontrado.");

            if (string.IsNullOrWhiteSpace(nomeDestino))
                throw new ArgumentException("O nome completo do destinatário é obrigatório.");

            if (string.IsNullOrWhiteSpace(cpfDestino))
                throw new ArgumentException("O CPF do destinatário é obrigatório.");

            if (string.IsNullOrWhiteSpace(emailDestino) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(emailDestino))
                throw new ArgumentException("O e-mail do destinatário é inválido.");

            var emailLimpo = emailDestino.Trim().ToLowerInvariant();
            if (emailLimpo.Length > 100)
                emailLimpo = emailLimpo.Substring(0, 100);

            var cpfDestinoLimpo = Methods.RemoveNaoNumericos(cpfDestino);
            if (cpfDestinoLimpo.Length != 11 || !Methods.ValidarCpf(cpfDestinoLimpo))
                throw new ArgumentException("CPF do destinatário é inválido.");

            if (cpfDestinoLimpo == cpfOrigemLimpo || cpfDestino == pessoaOrigem.Cpf)
                throw new ArgumentException("Não é possível transferir um ingresso para você mesmo.");

            var cpfDestinoFormatado = Methods.PatternCpf(cpfDestinoLimpo);
            var amigo = _context.Pessoas.FirstOrDefault(p =>
                p.Cpf == cpfDestinoLimpo || p.Cpf == cpfDestino || (cpfDestinoFormatado != null && p.Cpf == cpfDestinoFormatado));

            if (amigo == null)
            {
                // Verifica se o e-mail informado já está vinculado a outra pessoa no sistema
                var pessoaOutroCpf = _context.Pessoas.FirstOrDefault(p =>
                    p.Email != null && p.Email.ToLower() == emailLimpo &&
                    p.Cpf != cpfDestinoLimpo && (cpfDestinoFormatado == null || p.Cpf != cpfDestinoFormatado));

                if (pessoaOutroCpf != null)
                    throw new ArgumentException("O e-mail informado já está cadastrado para outro CPF no sistema. Verifique os dados do destinatário.");

                if (_userManager != null)
                {
                    var userComEmail = _userManager.FindByEmailAsync(emailLimpo).GetAwaiter().GetResult();
                    if (userComEmail != null && userComEmail.UserName != cpfDestinoLimpo)
                        throw new ArgumentException("O e-mail informado já está vinculado a outro CPF no sistema. Verifique os dados do destinatário.");
                }

                var nomeTrim = nomeDestino.Trim();
                if (nomeTrim.Length > 50)
                    nomeTrim = nomeTrim.Substring(0, 50);

                var nomeCracha = nomeTrim.Length > 20 ? nomeTrim.Substring(0, 20) : nomeTrim;
                if (string.IsNullOrWhiteSpace(nomeCracha))
                    nomeCracha = "Participante";

                amigo = new Pessoa
                {
                    Nome = nomeTrim,
                    NomeCracha = nomeCracha,
                    Cpf = cpfDestinoLimpo,
                    Sexo = "N",
                    Email = emailLimpo
                };

                _context.Pessoas.Add(amigo);
                _context.SaveChanges();
            }
            else
            {
                // Amigo já existe no banco: valida se o e-mail digitado confere com o cadastro existente
                if (!string.IsNullOrWhiteSpace(amigo.Email))
                {
                    var emailCadastrado = amigo.Email.Trim().ToLowerInvariant();
                    if (!emailCadastrado.EndsWith("@borala.com.br") && emailCadastrado != emailLimpo)
                    {
                        throw new ArgumentException("O e-mail informado não confere com o cadastro existente deste CPF. Verifique os dados do destinatário.");
                    }
                }

                if (_userManager != null)
                {
                    var userIdentity = _userManager.FindByNameAsync(cpfDestinoLimpo).GetAwaiter().GetResult();
                    if (userIdentity != null && !string.IsNullOrWhiteSpace(userIdentity.Email) && !userIdentity.Email.EndsWith("@borala.com.br"))
                    {
                        if (userIdentity.Email.Trim().ToLowerInvariant() != emailLimpo)
                        {
                            throw new ArgumentException("O e-mail informado não confere com o cadastro existente deste CPF. Verifique os dados do destinatário.");
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(amigo.Nome))
                {
                    var primeiroNomeBanco = amigo.Nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    var primeiroNomeDigitado = nomeDestino.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(primeiroNomeBanco) && !string.IsNullOrWhiteSpace(primeiroNomeDigitado) &&
                        !string.Equals(primeiroNomeBanco, primeiroNomeDigitado, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("O nome informado não confere com o cadastro existente deste CPF. Verifique os dados do destinatário.");
                    }
                }
            }

            if (_userManager != null)
            {
                var existingIdentity = _userManager.FindByNameAsync(cpfDestinoLimpo).GetAwaiter().GetResult();
                if (existingIdentity == null)
                {
                    var novoUsuario = new UsuarioIdentity
                    {
                        UserName = cpfDestinoLimpo,
                        NormalizedUserName = cpfDestinoLimpo.ToUpper(),
                        Email = emailLimpo,
                        EmailConfirmed = true
                    };
                    _userManager.CreateAsync(novoUsuario, "Temp@1234!").GetAwaiter().GetResult();
                    _userManager.AddToRoleAsync(novoUsuario, "USUARIO").GetAwaiter().GetResult();
                }
                else if (string.IsNullOrWhiteSpace(existingIdentity.Email) || existingIdentity.Email.EndsWith("@borala.com.br"))
                {
                    existingIdentity.Email = emailLimpo;
                    _userManager.UpdateAsync(existingIdentity).GetAwaiter().GetResult();
                }
            }

            if (amigo.Id == pessoaOrigem.Id)
                throw new ArgumentException("Não é possível transferir um ingresso para você mesmo.");

            // Caso 1: Transferência EXCLUSIVA de Subevento (se idSubevento informado)
            if (idSubevento.HasValue && idSubevento.Value > 0)
            {
                var subInscricao = _context.Inscricaopessoasubeventos
                    .Include(s => s.IdSubEventoNavigation)
                    .FirstOrDefault(s => s.IdPessoa == pessoaOrigem.Id && s.IdSubEvento == idSubevento.Value);

                if (subInscricao == null)
                    throw new ArgumentException("Inscrição no subevento não encontrada ou não pertence ao usuário.");

                if (subInscricao.Status == "C")
                    throw new InvalidOperationException("Não é possível transferir um subevento cancelado.");

                var sub = subInscricao.IdSubEventoNavigation;
                bool subeventoJaOcorreu = (sub != null) && (
                    (sub.DataFim != DateTime.MinValue && sub.DataFim < DateTime.Now) ||
                    (sub.DataFim == DateTime.MinValue && sub.DataInicio != DateTime.MinValue && sub.DataInicio < DateTime.Now) ||
                    sub.Status == "F" || sub.Status == "I"
                );

                if (subeventoJaOcorreu)
                    throw new InvalidOperationException("Não é possível transferir ingresso de um subevento que já aconteceu ou foi encerrado.");

                var amigoJaInscrito = _context.Inscricaopessoasubeventos
                    .Any(s => s.IdPessoa == amigo.Id && s.IdSubEvento == idSubevento.Value && s.Status != "C");
                if (amigoJaInscrito)
                    throw new InvalidOperationException("O destinatário já possui uma inscrição ativa neste subevento.");

                // Transfere EXCLUSIVAMENTE o subevento selecionado. Não transfere o evento principal nem outros subeventos.
                TransferirRegistroSubEvento(subInscricao, amigo.Id);
                _context.SaveChanges();
                return true;
            }
            // Caso 2: Transferência EXCLUSIVA de Inscrição em Evento Principal (sem transferir os subeventos)
            else if (idInscricao > 0)
            {
                var inscricao = _context.Inscricaopessoaeventos
                    .Include(i => i.IdEventoNavigation)
                    .FirstOrDefault(i => i.Id == idInscricao && i.IdPessoa == pessoaOrigem.Id);

                if (inscricao == null)
                    throw new ArgumentException("Inscrição não encontrada ou não pertence ao usuário.");

                if (inscricao.Status == "C")
                    throw new InvalidOperationException("Não é possível transferir um ingresso cancelado.");

                var ev = inscricao.IdEventoNavigation;
                bool eventoJaOcorreu = (ev != null) && (
                    (ev.DataFim.HasValue && ev.DataFim.Value < DateTime.Now) ||
                    (!ev.DataFim.HasValue && ev.DataInicio.HasValue && ev.DataInicio.Value < DateTime.Now) ||
                    ev.Status == "F" || ev.Status == "C"
                );

                if (eventoJaOcorreu)
                    throw new InvalidOperationException("Não é possível transferir ingresso de um evento que já aconteceu ou foi encerrado.");

                var amigoJaTemEvento = _context.Inscricaopessoaeventos
                    .Any(i => i.IdPessoa == amigo.Id && i.IdEvento == inscricao.IdEvento && i.Status != "C");
                if (amigoJaTemEvento)
                    throw new InvalidOperationException("O destinatário já possui uma inscrição ativa neste evento.");

                // Transfere EXCLUSIVAMENTE o evento principal, NUNCA transferindo os subeventos (filhos)
                inscricao.IdPessoa = amigo.Id;
                inscricao.NomeCracha = amigo.NomeCracha;

                _context.SaveChanges();
                return true;
            }
            else
            {
                throw new ArgumentException("Nenhum ingresso selecionado para transferência.");
            }
        }

        private void TransferirRegistroSubEvento(Inscricaopessoasubevento subInscricao, uint novoIdPessoa)
        {
            subInscricao.IdPessoa = novoIdPessoa;
        }
    }
}
