using AutoMapper;
using Core.Service;
using Core;
using Core.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class InscricaoController : Controller
    {
        public const uint TIPO_MEIA_ENTRADA = uint.MaxValue;

        private readonly IEventoService _eventoService;
        private readonly IPessoaService _pessoaService;
        private readonly IInscricaoService _inscricaoService;
        private readonly ISubeventoService _subeventoService;
        private readonly UserManager<UsuarioIdentity> _userManager;
        private readonly ITipoInscricaoService _tipoinscricaoService;
        private readonly IMapper _mapper;

        public InscricaoController(UserManager<UsuarioIdentity> userManager, ITipoInscricaoService tipoinscricaoService, IEventoService eventoService, IMapper mapper, IInscricaoService inscricaoService, IPessoaService pessoaService, ISubeventoService subeventoService)
        {
            _tipoinscricaoService = tipoinscricaoService;
            _eventoService = eventoService;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
            _pessoaService = pessoaService;
            _subeventoService = subeventoService;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("RealizarInscricao/{idEvento}/{idSubevento?}")]
        public IActionResult realizarInscricao(uint idEvento, uint? idSubevento)
        {
            Evento evento = _eventoService.Get(idEvento);
            if (evento == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (evento.Status != "A")
            {
                TempData["ParticipanteMessage"] = "Este evento não está ativo para inscrições.";
                return RedirectToAction("Index", "Home", new { vitrine = "true" });
            }

            if (evento.DataInicioInscricao.HasValue && evento.DataInicioInscricao.Value > DateTime.Now)
            {
                TempData["ParticipanteMessage"] = "O período de inscrições para este evento ainda não começou.";
                return RedirectToAction("Index", "Home", new { vitrine = "true" });
            }

            if (evento.DataFimInscricao.HasValue && evento.DataFimInscricao.Value < DateTime.Now)
            {
                TempData["ParticipanteMessage"] = "O período de inscrições para este evento já foi encerrado.";
                return RedirectToAction("Index", "Home", new { vitrine = "true" });
            }

            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && !referer.Contains("Account/Login", StringComparison.OrdinalIgnoreCase) && !referer.Contains("RealizarInscricao", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.UrlVoltar = referer;
                TempData["UrlVoltar"] = referer;
            }
            else if (TempData.ContainsKey("UrlVoltar"))
            {
                ViewBag.UrlVoltar = TempData["UrlVoltar"];
                TempData.Keep("UrlVoltar");
            }
            else
            {
                ViewBag.UrlVoltar = Url.Action("Index", "Home", new { vitrine = "true" });
            }

            if (User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name)
                && _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento) != null)
            {
                return RedirectToAction("GerenciarEvento", "Evento", new { idEvento = idEvento });
            }

            EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
            var tipoInscricaoModel = _tipoinscricaoService.GetByEvento(idEvento).ToList();
            var subeventos = _subeventoService.GetByIdEvento(idEvento).ToList();
            var tiposPorSubevento = _tipoinscricaoService.GetTiposAgrupadosPorSubevento(idEvento) 
                ?? new Dictionary<uint, List<TipoInscricaoDTO>>();
            var subeventosOpcoes = new List<SubeventoOpcao>();
            foreach(var sub in subeventos)
            {
                tiposPorSubevento.TryGetValue(sub.Id, out var tipos);
                subeventosOpcoes.Add(new SubeventoOpcao { Subevento = sub, TiposInscricao = tipos ?? Enumerable.Empty<TipoInscricaoDTO>() });
            }

            var model = new InscricaoEventoViewModel(){
                tipoInscricao = tipoInscricaoModel,
                eventoNavigation = eventoModel,
                SubeventosOpcoes = subeventosOpcoes
            };

            if (User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name))
            {
                var pessoa = _pessoaService.GetByCpf(User.Identity.Name);
                if (pessoa != null)
                {
                    var inscricaoExistente = _inscricaoService.GetByEvento(idEvento)
                        .FirstOrDefault(i => i.IdPessoa == pessoa.Id && i.IdPapel == 4);

                    var subeventosInscritos = (evento.PossuiCertificado != 0)
                        ? _inscricaoService.GetSubByEvento(idEvento)
                            .Where(s => s.IdPessoa == pessoa.Id && s.Status != "C")
                            .Select(s => s.IdSubEvento)
                            .ToHashSet()
                        : new HashSet<uint>();

                    ViewBag.SubeventosInscritos = subeventosInscritos;

                    bool estaInscritoEventoPrincipal = evento.PossuiCertificado != 0 && (inscricaoExistente != null || _inscricaoService.IsInscrito(pessoa.Id, idEvento));
                    ViewBag.JaInscritoEventoPrincipal = estaInscritoEventoPrincipal;

                    bool todosSubeventosInscritos = subeventos.Any() && subeventos.All(s => subeventosInscritos.Contains(s.Id));

                    if (estaInscritoEventoPrincipal && (!subeventos.Any() || todosSubeventosInscritos))
                    {
                        ViewBag.JaInscrito = true;
                    }

                    var nomeSugestao = !string.IsNullOrWhiteSpace(inscricaoExistente?.NomeCracha)
                        ? inscricaoExistente.NomeCracha
                        : (!string.IsNullOrWhiteSpace(pessoa.NomeCracha) ? pessoa.NomeCracha : pessoa.Nome);

                    if (!string.IsNullOrEmpty(nomeSugestao) && nomeSugestao.Length > 20)
                    {
                        nomeSugestao = nomeSugestao.Substring(0, 20);
                    }

                    model.inscricaoNavigation = new InscricaoEventoModel
                    {
                        NomeCracha = nomeSugestao
                    };
                }
            }
            
            if (evento.PossuiCertificado == 0)
            {
                return View("RealizarInscricaoLote", model);
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [Route("RealizarInscricao/{idEvento}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> realizarInscricao(uint idEvento, InscricaoEventoModel inscricaoEvento)
        {
            var pessoa = _pessoaService.GetByCpf(User.Identity.Name);
            if (pessoa == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var evento = _eventoService.Get(idEvento);
            if (evento == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (evento.Status != "A")
            {
                TempData["ParticipanteMessage"] = "Este evento não está ativo para inscrições.";
                return RedirectToAction("Index", "Home");
            }

            if (evento.DataInicioInscricao.HasValue && evento.DataInicioInscricao.Value > DateTime.Now)
            {
                TempData["ParticipanteMessage"] = "O período de inscrições para este evento ainda não começou.";
                return RedirectToAction("Index", "Home");
            }

            if (evento.DataFimInscricao.HasValue && evento.DataFimInscricao.Value < DateTime.Now)
            {
                TempData["ParticipanteMessage"] = "O período de inscrições para este evento já foi encerrado.";
                return RedirectToAction("Index", "Home");
            }

            bool jaInscritoEventoPrincipal = evento.PossuiCertificado != 0 && _inscricaoService.IsInscrito(pessoa.Id, idEvento);
            var subeventosJaInscritos = (evento.PossuiCertificado != 0)
                ? _inscricaoService.GetSubByEvento(idEvento)
                    .Where(s => s.IdPessoa == pessoa.Id && s.Status != "C")
                    .Select(s => s.IdSubEvento)
                    .ToHashSet()
                : new HashSet<uint>();

            if (jaInscritoEventoPrincipal)
            {
                var subeventosNovos = inscricaoEvento.SelectedSubeventos?
                    .Where(idSub => !subeventosJaInscritos.Contains(idSub))
                    .ToList();

                if (subeventosNovos == null || !subeventosNovos.Any())
                {
                    TempData["ParticipanteMessage"] = "Você já está inscrito neste evento!";
                    return RedirectToAction("minhasInscricoes", new { idEvento = idEvento });
                }
            }

            var mainEventQuantities = new Dictionary<uint, int>();
            int totalEventTickets = 0;

            if (!jaInscritoEventoPrincipal)
            {
                foreach (var key in Request.Form.Keys)
                {
                    if (key.StartsWith("QuantidadeTipoInscricao_"))
                    {
                        if (uint.TryParse(key.Replace("QuantidadeTipoInscricao_", ""), out uint idTipo))
                        {
                            if (int.TryParse(Request.Form[key], out int qtd) && qtd > 0)
                            {
                                mainEventQuantities[idTipo] = qtd;
                                totalEventTickets += qtd;
                            }
                        }
                    }
                }

                if (evento.PossuiCertificado != 0 || totalEventTickets < 1)
                {
                    totalEventTickets = 1;
                    if (mainEventQuantities.Count > 0)
                    {
                        var firstKey = mainEventQuantities.Keys.First();
                        mainEventQuantities.Clear();
                        mainEventQuantities[firstKey] = 1;
                    }
                    else
                    {
                        mainEventQuantities[inscricaoEvento.IdTipoInscricao ?? 0] = 1;
                    }
                }
                else if (totalEventTickets > 8)
                {
                    TempData["ParticipanteMessage"] = "Limite máximo de ingressos excedido.";
                    return RedirectToAction("Index", "Home"); 
                }

                if (evento.VagasDisponiveis <= 0)
                {
                    TempData["ParticipanteMessage"] = "Lotação esgotada.";
                    return RedirectToAction("Index", "Home"); 
                }

                if (evento.VagasDisponiveis < totalEventTickets)
                {
                    TempData["ParticipanteMessage"] = "A quantidade de ingressos solicitada excede o número de vagas disponíveis do evento.";
                    return RedirectToAction("Index", "Home"); 
                }
            }

            if (inscricaoEvento.SelectedSubeventos != null && inscricaoEvento.SelectedSubeventos.Any())
            {
                foreach (var idSubevento in inscricaoEvento.SelectedSubeventos)
                {
                    var subevento = _subeventoService.Get(idSubevento);
                    if (subevento == null || subevento.Status == "C" || subevento.Status == "F" || subevento.DataFimInscricao < DateTime.Now)
                    {
                        continue;
                    }

                    if (subevento.DataInicioInscricao > DateTime.Now)
                    {
                        TempData["ParticipanteMessage"] = "Um dos subeventos selecionados ainda não abriu inscrições.";
                        return RedirectToAction("realizarInscricao", new { idEvento = idEvento });
                    }

                    int totalSubTicketsCheck = 0;
                    string prefix = $"QuantidadeTipoInscricaoSubevento_{idSubevento}_";
                    foreach (var key in Request.Form.Keys)
                    {
                        if (key.StartsWith(prefix))
                        {
                            if (int.TryParse(Request.Form[key], out int qtd) && qtd > 0)
                            {
                                totalSubTicketsCheck += qtd;
                            }
                        }
                    }

                    if (totalSubTicketsCheck > 8)
                    {
                        TempData["ParticipanteMessage"] = "Limite máximo para subeventos excedido.";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            string? nomeCracha = !string.IsNullOrWhiteSpace(inscricaoEvento?.NomeCracha)
                ? inscricaoEvento.NomeCracha.Trim()
                : (!string.IsNullOrWhiteSpace(pessoa.NomeCracha) ? pessoa.NomeCracha.Trim() : pessoa.Nome?.Trim());

            if (string.IsNullOrWhiteSpace(nomeCracha))
            {
                nomeCracha = "Participante";
            }
            else if (nomeCracha.Length > 20)
            {
                nomeCracha = nomeCracha.Substring(0, 20);
            }

            if (pessoa.NomeCracha != nomeCracha)
            {
                pessoa.NomeCracha = nomeCracha;
                await _pessoaService.Edit(pessoa);
            }

            var eventosParaSalvar = new List<Inscricaopessoaevento>();
            var subeventosParaSalvar = new List<Inscricaopessoasubevento>();
            var subeventosAtualizarVagas = new HashSet<uint>();
            foreach (var kvp in mainEventQuantities)
            {
                uint idTipo = kvp.Key;
                int quantidade = kvp.Value;

                decimal valorMain = 0m;

                var tipoObjMain = (idTipo != 0 && idTipo != TIPO_MEIA_ENTRADA)
                    ? _tipoinscricaoService.Get(idTipo)
                    : null;
                bool tipoRealMain = tipoObjMain != null;

                uint? idTipoToSave = tipoRealMain ? (uint?)idTipo : null;

                if (tipoRealMain)
                {
                    valorMain = tipoObjMain.Valor;
                }
                else if (idTipo == TIPO_MEIA_ENTRADA)
                {
                    valorMain = evento != null ? (evento.ValorInscricao / 2m) : 0m;
                }
                else
                {
                    valorMain = evento != null ? evento.ValorInscricao : 0m;
                }

                for (int i = 0; i < quantidade; i++)
                {
                    var novaInscricao = new InscricaoEventoModel()
                    {
                        IdPessoa = pessoa.Id,
                        IdEvento = idEvento,
                        IdPapel = 4,
                        DataInscricao = DateTime.Now,
                        NomeCracha = nomeCracha,
                        Status = "S",
                        IdTipoInscricao = idTipoToSave,
                        FrequenciaFinal = 0m,
                        ValorTotal = valorMain 
                    };

                    var inscricao = _mapper.Map<Inscricaopessoaevento>(novaInscricao);
                    eventosParaSalvar.Add(inscricao);
                }
            }

            if (inscricaoEvento.SelectedSubeventos != null && inscricaoEvento.SelectedSubeventos.Any())
            {
                foreach (var idSubevento in inscricaoEvento.SelectedSubeventos)
                {
                    if (evento.PossuiCertificado != 0 && subeventosJaInscritos.Contains(idSubevento))
                    {
                        continue;
                    }

                    var subevento = _subeventoService.Get(idSubevento);
                    bool subeventoPassou = subevento != null && (subevento.DataFim < DateTime.Now || (subevento.DataInicio < DateTime.Now && subevento.DataFim <= subevento.DataInicio));
                    if (subevento == null || subevento.Status != "A" || subevento.DataInicioInscricao > DateTime.Now || subevento.DataFimInscricao < DateTime.Now || subeventoPassou || subevento.IdEvento != idEvento)
                    {
                        continue;
                    }
                    var subEventQuantities = new Dictionary<uint, int>();
                    int totalSubTickets = 0;

                    foreach (var key in Request.Form.Keys)
                    {
                        string prefix = $"QuantidadeTipoInscricaoSubevento_{idSubevento}_";
                        if (key.StartsWith(prefix))
                        {
                            if (uint.TryParse(key.Replace(prefix, ""), out uint idTipoSub))
                            {
                                if (int.TryParse(Request.Form[key], out int qtd) && qtd > 0)
                                {
                                    subEventQuantities[idTipoSub] = qtd;
                                    totalSubTickets += qtd;
                                }
                            }
                        }
                    }
                    
                    if (totalSubTickets > 8)
                    {
                        TempData["ParticipanteMessage"] = "Limite máximo para subeventos excedido.";
                        return RedirectToAction("Index", "Home"); 
                    }

                    if (subEventQuantities.Count == 0)
                    {
                        var tipoVal = Request.Form[$"TipoInscricaoSubevento_{idSubevento}"];
                        uint parsedTipo = 0;
                        if (!string.IsNullOrEmpty(tipoVal) && uint.TryParse(tipoVal, out uint t))
                        {
                            parsedTipo = t;
                        }
                        subEventQuantities[parsedTipo] = 1;
                    }

                    if (subevento.VagasDisponiveis <= 0)
                    {
                        TempData["ParticipanteMessage"] = $"As vagas para o subevento {subevento.Nome} estão esgotadas.";
                        return RedirectToAction("realizarInscricao", new { idEvento = idEvento }); 
                    }

                    if (subevento.VagasDisponiveis < totalSubTickets)
                    {
                        TempData["ParticipanteMessage"] = $"A quantidade de ingressos solicitada excede o número de vagas disponíveis para o subevento {subevento.Nome}.";
                        return RedirectToAction("minhasInscricoes", new { idEvento = idEvento }); 
                    }

                    foreach (var kvpSub in subEventQuantities)
                    {
                        uint idTipoSub = kvpSub.Key;
                        int quantidadeSub = kvpSub.Value;

                        if (idTipoSub != 0 && idTipoSub != TIPO_MEIA_ENTRADA)
                        {
                            var tipoCheckSub = _tipoinscricaoService.Get(idTipoSub);
                            if (tipoCheckSub == null || tipoCheckSub.IdEvento != idEvento)
                            {
                                continue;
                            }
                        }

                        decimal valorSub = 0m;

                        var tipoObjSub = (idTipoSub != 0 && idTipoSub != TIPO_MEIA_ENTRADA)
                            ? _tipoinscricaoService.Get(idTipoSub)
                            : null;
                        bool tipoRealSub = tipoObjSub != null;

                        uint? idTipoSubParaSalvar = tipoRealSub ? (uint?)idTipoSub : null;

                        if (tipoRealSub)
                        {
                            valorSub = tipoObjSub.Valor;
                        }
                        else if (idTipoSub == TIPO_MEIA_ENTRADA)
                        {
                            valorSub = subevento != null ? (subevento.ValorInscricao / 2m) : 0m;
                        }
                        else
                        {
                            valorSub = subevento != null ? subevento.ValorInscricao : 0m;
                        }

                        for (int j = 0; j < quantidadeSub; j++)
                        {
                            var novaInscricaoSub = new Inscricaopessoasubevento()
                            {
                                IdPessoa = pessoa.Id,
                                IdSubEvento = idSubevento,
                                IdPapel = 4,
                                DataInscricao = DateTime.Now,
                                Status = "S",
                                IdTipoInscricao = idTipoSubParaSalvar,
                                FrequenciaFinal = 0m,
                                Valor = valorSub,
                            };
                            subeventosParaSalvar.Add(novaInscricaoSub);
                        }
                    }
                    if (subEventQuantities.Any())
                    {
                        subeventosAtualizarVagas.Add(idSubevento);
                    }
                }
            }

            if (eventosParaSalvar.Any() || subeventosParaSalvar.Any())
            {
                _inscricaoService.CreateInscricoesEmLote(eventosParaSalvar, subeventosParaSalvar, saveChanges: false);

                if (eventosParaSalvar.Any())
                {
                    _eventoService.AtualizarVagasDisponiveis(idEvento, saveChanges: false);
                }

                foreach (var idSub in subeventosAtualizarVagas)
                {
                    _subeventoService.AtualizarVagasDisponiveis(idSub, saveChanges: false);
                }

                _inscricaoService.SaveChanges();
            }

            if (jaInscritoEventoPrincipal)
            {
                TempData["ParticipanteSuccessMessage"] = "Inscrição no(s) subevento(s) realizada com sucesso!";
            }
            else
            {
                TempData["ParticipanteSuccessMessage"] = "Inscrição realizada com sucesso!";
            }
            return RedirectToAction("minhasInscricoes", new { idEvento = idEvento });
        }

        [Authorize]
        [HttpGet]
        [Route("MinhasInscricoes")]
        public async Task<IActionResult> minhasInscricoes(uint? idEvento)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Home");
            }

            var inscricaoUser = _inscricaoService.GetAllEventsByUserId(username);
            var listarEventosModel = inscricaoUser.Select(i =>
            {
                var model = _mapper.Map<InscricaoEventoModel>(i);
                if (i.Inscricaopessoasubeventos != null && i.Inscricaopessoasubeventos.Any())
                {
                    model.Inscricaopessoasubeventos = i.Inscricaopessoasubeventos.ToList();
                }
                return model;
            }).ToList();

            ViewBag.EventoId = idEvento ?? listarEventosModel.FirstOrDefault()?.IdEvento;
            return View(listarEventosModel);
        }

        [Authorize]
        [HttpGet]
        [Route("DetalhesInscricao/{id?}")]
        public async Task<IActionResult> DetalhesInscricao(uint? id, uint? idEvento)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Home");
            }

            var inscricoesUser = _inscricaoService.GetAllEventsByUserId(username);
            Inscricaopessoaevento? inscricao = null;

            if (id.HasValue && id.Value > 0)
            {
                inscricao = inscricoesUser.FirstOrDefault(i => i.Id == id.Value);
            }

            if (inscricao == null && idEvento.HasValue && idEvento.Value > 0)
            {
                inscricao = inscricoesUser.FirstOrDefault(i => i.IdEvento == idEvento.Value);
            }

            if (inscricao == null)
            {
                TempData["ParticipanteMessage"] = "Inscrição não encontrada.";
                return RedirectToAction(nameof(minhasInscricoes));
            }

            var model = _mapper.Map<InscricaoEventoModel>(inscricao);
            if (inscricao.Inscricaopessoasubeventos != null && inscricao.Inscricaopessoasubeventos.Any())
            {
                model.Inscricaopessoasubeventos = inscricao.Inscricaopessoasubeventos.ToList();
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [Route("CancelarInscricao")]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarInscricao(uint idEvento)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Home");
            }

            // Só o dono cancela as próprias inscrições de participante:
            // a pessoa é sempre resolvida a partir do usuário logado.
            var pessoa = _pessoaService.GetByCpf(username);
            if (pessoa == null)
            {
                TempData["ParticipanteMessage"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(minhasInscricoes));
            }

            var evento = _eventoService.Get(idEvento);
            if (evento == null)
            {
                TempData["ParticipanteMessage"] = "Evento não encontrado.";
                return RedirectToAction(nameof(minhasInscricoes));
            }

            if (evento.Status != "A")
            {
                TempData["ParticipanteMessage"] = "Este evento não está ativo para desistência.";
                return RedirectToAction(nameof(minhasInscricoes));
            }

            // A desistência vale até o início do evento, não até o fim do prazo
            // de inscrição: após DataFimInscricao o participante ainda pode
            // liberar a vaga, desde que o evento não tenha começado.
            if (evento.DataInicio.HasValue && evento.DataInicio.Value <= DateTime.Now)
            {
                TempData["ParticipanteMessage"] = "Este evento já foi iniciado e não permite mais desistência.";
                return RedirectToAction(nameof(minhasInscricoes));
            }

            if (evento.DataFim.HasValue && evento.DataFim.Value < DateTime.Now)
            {
                TempData["ParticipanteMessage"] = "Este evento já foi encerrado e não permite mais desistência.";
                return RedirectToAction(nameof(minhasInscricoes));
            }

            try
            {
                var (removidas, subeventosAfetados) = _inscricaoService.CancelarInscricaoParticipante(pessoa.Id, idEvento);
                if (removidas == 0)
                {
                    TempData["ParticipanteMessage"] = "Nenhuma inscrição de participante encontrada para este evento.";
                    return RedirectToAction(nameof(minhasInscricoes));
                }

                _eventoService.AtualizarVagasDisponiveis(idEvento);
                foreach (var idSub in subeventosAfetados)
                {
                    _subeventoService.AtualizarVagasDisponiveis(idSub);
                }
            }
            catch (DbUpdateException)
            {
                TempData["ParticipanteMessage"] = "Não foi possível concluir a desistência. Tente novamente.";
                return RedirectToAction(nameof(minhasInscricoes));
            }
            catch (Exception)
            {
                TempData["ParticipanteMessage"] = "Não foi possível concluir a desistência. Tente novamente.";
                return RedirectToAction(nameof(minhasInscricoes));
            }

            TempData["ParticipanteSuccessMessage"] = "Desistência realizada com sucesso! Sua vaga foi liberada.";
            return RedirectToAction(nameof(minhasInscricoes));
        }

    }
}
