using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Helpers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;


namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
    public class SubeventoController : Controller
    {
        private readonly ISubeventoService _subeventoService;
        private readonly IEventoService _eventoService;
        private readonly ITipoeventoService _tipoEventoService;
        private readonly ITipoInscricaoService _tipoInscricaoService;
        private readonly IInscricaoService _inscricaoService;
        private readonly IMapper _mapper;
        public SubeventoController(ISubeventoService subeventoService, IMapper mapper, IEventoService eventoService, ITipoeventoService tipoeventoService, ITipoInscricaoService tipoInscricaoService, IInscricaoService inscricaoService)
        {
            _subeventoService = subeventoService;
            _eventoService = eventoService;
            _tipoEventoService = tipoeventoService;
            _mapper = mapper;
            _tipoInscricaoService = tipoInscricaoService;
            _inscricaoService = inscricaoService;
        }

        private bool IsAuthorized(uint idEvento)
        {
            return AutorizacaoEventoHelper.IsAutorizado(User, _inscricaoService, idEvento);
        }

        // GET: SubeventoController
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return View(new List<SubeventoModel>());
            }

            string userCpf = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(userCpf))
            {
                return View(new List<SubeventoModel>());
            }

            List<Subevento> listaSubeventos = new List<Subevento>();

            var todosSubeventos = _subeventoService.GetAll();
            if (todosSubeventos != null)
            {
                if (User.IsInRole("ADMINISTRADOR"))
                {
                    listaSubeventos = todosSubeventos.ToList();
                }
                else
                {
                    var eventosDoUsuario = _eventoService.GetEventByCpf(userCpf, 2);

                    var idsEventosDoUsuario = eventosDoUsuario != null
                        ? eventosDoUsuario.Select(ev => ev.Id).ToHashSet()
                        : new HashSet<uint>();

                    listaSubeventos = todosSubeventos
                        .Where(s => idsEventosDoUsuario.Contains(s.IdEvento))
                        .ToList();
                }
            }

            var todosEventos = _eventoService.GetAll();
                var dicionarioEventos = todosEventos != null
                    ? todosEventos.ToDictionary(e => e.Id, e => e.Nome)
                    : new Dictionary<uint, string>();

                var todosTipos = _tipoEventoService.GetAll();
                var dicionarioTipos = todosTipos != null
                    ? todosTipos.ToDictionary(t => t.Id, t => t.Nome)
                    : new Dictionary<uint, string>();

                var listaSubeventosModel = listaSubeventos.Select(e =>
                {
                    string nomeEvento = dicionarioEventos.TryGetValue(e.IdEvento, out var nomeE) ? nomeE : "Evento não encontrado";

                    string nomeTipoEvento = dicionarioTipos.TryGetValue(e.IdTipoEvento, out var nomeT) ? nomeT : "Tipo não encontrado";

                    return new SubeventoModel
                    {
                        Id = e.Id,
                        Nome = e.Nome,
                        IdEvento = e.IdEvento,
                        NomeEvento = nomeEvento,
                        DataInicio = e.DataInicio,
                        Status = e.Status,
                        IdTipoEvento = e.IdTipoEvento,
                        NomeTipoEvento = nomeTipoEvento
                    };
                }).ToList();

                return View(listaSubeventosModel);
            }

            // GET: SubeventoController/Details/5
            [HttpGet]
            [Route("Details/{id}")]
            public ActionResult Details(uint id)
            {
                Subevento subevento = _subeventoService.Get(id);
                if (subevento == null) return NotFound();
                if (!IsAuthorized(subevento.IdEvento))
                    return Forbid();
                SubeventoModel subeventoModel = _mapper.Map<SubeventoModel>(subevento);
                return View(subeventoModel);
            }

            // GET: SubeventoController/CreateOrEdit/{idEvento}/{idSubevento?}
            [HttpGet]
            [Route("CreateOrEdit/{idEvento}/{idSubevento?}")]
            public ActionResult CreateOrEdit(uint idEvento, uint? idSubevento)
            {
                if (!IsAuthorized(idEvento))
                    return Forbid();
                SubeventoModel subeventoModel;
                if (idSubevento.HasValue)
                {
                    var subevento = _subeventoService.Get(idSubevento.Value);
                    if (subevento == null)
                    {
                        return NotFound();
                    }
                    if (subevento.IdEvento != idEvento)
                        return Forbid();
                    subeventoModel = _mapper.Map<SubeventoModel>(subevento);
                }
                else
                {
                    subeventoModel = new SubeventoModel();
                }

                var tipoEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
                var evento = _eventoService.GetEventoSimpleDto(idEvento);

                var viewModel = subeventoModel;

                viewModel.IdEvento = idEvento;
                viewModel.Evento = evento;
                viewModel.TiposEventos = new SelectList(tipoEventos, "Id", "Nome");

                return View(viewModel);
            }

            // POST: SubeventoController/CreateOrEdit/{idEvento}/{idSubevento?}
            [HttpPost]
            [Route("CreateOrEdit/{idEvento}/{idSubevento?}")]
            [ValidateAntiForgeryToken]
            public ActionResult CreateOrEdit(uint idEvento, [Bind("Id,IdEvento,Nome,Descricao,DataInicio,DataFim,InscricaoGratuita,Status,DataInicioInscricao,DataFimInscricao,ValorInscricao,PossuiCertificado,FrequenciaMinimaCertificado,VagasOfertadas,CargaHoraria,IdTipoEvento")] SubeventoModel subeventoModel, uint? idSubevento = null)
            {
                if (!IsAuthorized(idEvento))
                    return Forbid();
                if (idSubevento.HasValue && subeventoModel.Id != 0 && idSubevento.Value != subeventoModel.Id)
                    return BadRequest("Id do subevento divergente.");
                if (subeventoModel.IdEvento != 0 && subeventoModel.IdEvento != idEvento)
                    return BadRequest("Não é permitido mover o subevento para outro evento.");
                Subevento? existente = null;
                // Impede mover subevento de evento alheio para o próprio evento
                if (subeventoModel.Id != 0)
                {
                    existente = _subeventoService.Get(subeventoModel.Id);
                    if (existente == null)
                        return NotFound();
                    if (existente.IdEvento != idEvento)
                        return BadRequest("Não é permitido mover o subevento para outro evento.");
                    if (!IsAuthorized(existente.IdEvento))
                        return Forbid();
                }

                var evento = _eventoService.Get(idEvento);
                if (evento != null)
                {
                    if (evento.DataInicio.HasValue && subeventoModel.DataInicio < evento.DataInicio.Value)
                    {
                        ModelState.AddModelError("DataInicio", "A data de início do subevento não pode ser anterior ao início do evento principal.");
                    }

                    if (evento.DataFim.HasValue && subeventoModel.DataFim > evento.DataFim.Value)
                    {
                        ModelState.AddModelError("DataFim", "A data de término do subevento não pode ser posterior ao término do evento principal.");
                    }
                }

                if (subeventoModel.DataInicioInscricao > subeventoModel.DataFimInscricao)
                {
                    ModelState.AddModelError("DataInicioInscricao", "A data inicial de inscrição não pode ser posterior à data final de inscrição.");
                }

                if (subeventoModel.DataFimInscricao > subeventoModel.DataInicio)
                {
                    ModelState.AddModelError("DataFimInscricao", "O período de inscrições deve encerrar antes ou no início do subevento.");
                }

                if (subeventoModel.InscricaoGratuita == 1 && subeventoModel.ValorInscricao > 0)
                {
                    ModelState.AddModelError("ValorInscricao", "Para subeventos gratuitos, o valor de inscrição deve ser 0,00.");
                }

                if (ModelState.IsValid)
                {
                    if (existente != null)
                    {
                        existente.Nome = subeventoModel.Nome;
                        existente.Descricao = subeventoModel.Descricao;
                        existente.DataInicio = subeventoModel.DataInicio;
                        existente.DataFim = subeventoModel.DataFim;
                        existente.InscricaoGratuita = subeventoModel.InscricaoGratuita;
                        existente.Status = subeventoModel.Status;
                        existente.DataInicioInscricao = subeventoModel.DataInicioInscricao;
                        existente.DataFimInscricao = subeventoModel.DataFimInscricao;
                        existente.ValorInscricao = subeventoModel.ValorInscricao;
                        existente.PossuiCertificado = subeventoModel.PossuiCertificado;
                        existente.FrequenciaMinimaCertificado = subeventoModel.FrequenciaMinimaCertificado;
                        existente.VagasOfertadas = (uint)subeventoModel.VagasOfertadas;
                        existente.CargaHoraria = (uint)subeventoModel.CargaHoraria;
                        existente.IdTipoEvento = subeventoModel.IdTipoEvento;
                        existente.IdEvento = idEvento;
                        _subeventoService.Edit(existente);
                    }
                    else
                    {
                        var subevento = new Subevento
                        {
                            IdEvento = idEvento,
                            Nome = subeventoModel.Nome,
                            Descricao = subeventoModel.Descricao,
                            DataInicio = subeventoModel.DataInicio,
                            DataFim = subeventoModel.DataFim,
                            InscricaoGratuita = subeventoModel.InscricaoGratuita,
                            Status = subeventoModel.Status,
                            DataInicioInscricao = subeventoModel.DataInicioInscricao,
                            DataFimInscricao = subeventoModel.DataFimInscricao,
                            ValorInscricao = subeventoModel.ValorInscricao,
                            PossuiCertificado = subeventoModel.PossuiCertificado,
                            FrequenciaMinimaCertificado = subeventoModel.FrequenciaMinimaCertificado,
                            VagasOfertadas = (uint)subeventoModel.VagasOfertadas,
                            VagasReservadas = 0,
                            VagasDisponiveis = (uint)Math.Max(0, subeventoModel.VagasOfertadas),
                            CargaHoraria = (uint)subeventoModel.CargaHoraria,
                            IdTipoEvento = subeventoModel.IdTipoEvento
                        };
                        _subeventoService.Create(subevento);
                    }

                    return RedirectToAction("GerenciarEvento", "Evento", new { idEvento = idEvento });
                }

                var tipoEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
                var eventoDto = _eventoService.GetEventoSimpleDto(idEvento);
                subeventoModel.IdEvento = idEvento;
                subeventoModel.Evento = eventoDto;
                subeventoModel.TiposEventos = new SelectList(tipoEventos, "Id", "Nome");
                return View(subeventoModel);
            }

            // GET: SubeventoController/Delete/5
            [HttpGet]
            [Route("Delete/{id}")]
            public ActionResult Delete(uint id)
            {

                var subevento = _subeventoService.Get(id);
                if (subevento == null) return NotFound();
                if (!IsAuthorized(subevento.IdEvento))
                    return Forbid();
                var subeventoModel = _mapper.Map<SubeventoModel>(subevento);

                string nomeEvento = _eventoService.GetNomeById(subevento.IdEvento);
                subeventoModel.NomeEvento = nomeEvento;

                string nomeTipoEvento = _tipoEventoService.GetNomeById(subevento.IdTipoEvento);
                subeventoModel.NomeTipoEvento = nomeTipoEvento; ;

                return View(subeventoModel);
            }
            // POST: SubeventoController/Delete/5
            [HttpPost]
            [Route("Delete")]
            [ValidateAntiForgeryToken]
            public ActionResult Delete(uint id, SubeventoModel subeventoModel)
            {
                var existente = _subeventoService.Get(id);
                if (existente == null) return NotFound();
                if (!IsAuthorized(existente.IdEvento))
                    return Forbid();
                var tiposInscricao = _tipoInscricaoService.GetTiposInscricaosSubevento(id);

                if (tiposInscricao.Any())
                {
                    foreach (var tipoInscricao in tiposInscricao)
                    {
                        _tipoInscricaoService.DeleteTipoInscricaoSubevento(id, tipoInscricao.Id);
                    }
                }
                _subeventoService.Delete(id);
                return RedirectToAction(nameof(Index));
            }

        }
    }
