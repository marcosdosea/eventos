using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
    public class TipoInscricaoController : Controller
    {
        private readonly ITipoInscricaoService _tipoInscricaoService;
        private readonly IEventoService _eventoService;
        private readonly ISubeventoService _subeventoService;
        private readonly IInscricaoService _inscricaoService;
        private readonly IMapper _mapper;

        public TipoInscricaoController(ITipoInscricaoService tipoInscricaoService, IMapper mapper, IEventoService eventoService, ISubeventoService subeventoService, IInscricaoService inscricaoService)
        {
            this._tipoInscricaoService = tipoInscricaoService;
            this._eventoService = eventoService;
            this._subeventoService = subeventoService;
            this._inscricaoService = inscricaoService;
            this._mapper = mapper;
        }

        private bool IsAuthorized(uint idEvento)
        {
            if (User.IsInRole("ADMINISTRADOR"))
                return true;
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return false;
            return _inscricaoService.GetGestorInEvent(username, idEvento) != null;
        }

        // GET: /TipoInscricaocontroller
        [HttpGet]
        public ActionResult Index(uint? idEvento)
        {
            if (idEvento.HasValue)
            {
                if (!IsAuthorized(idEvento.Value))
                    return Forbid();
                var listaTipoInscricao = _tipoInscricaoService.GetByEvento(idEvento.Value).ToList();
                var listaTipoInscricaoModel = listaTipoInscricao.Select(e => new TipoInscricaoModel
                {
                    Id = e.Id,
                    Nome = e.Nome,
                    IdEvento = e.IdEvento,
                    NomeEvento = _eventoService.GetNomeById(e.IdEvento),
                    Descricao = e.Descricao,
                    Valor = e.Valor,
                    DataInicio = e.DataInicio,
                    Datafim = e.Datafim,
                    UsadaEvento = e.UsadaEvento,
                    UsadaSubevento = e.UsadaSubevento
                }).ToList();

                ViewData["EventoId"] = idEvento.Value;
                ViewData["EventoNome"] = _eventoService.GetNomeById(idEvento.Value);

                return View(listaTipoInscricaoModel);
            }
            else
            {
                return RedirectToAction("Index", "Evento");
            }
        }

        // GET: /TipoInscricao/Details/5
        [HttpGet("Details/{id}")]
        public ActionResult Details(uint id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
            if (tipoinscricao == null) return NotFound();

            if (!IsAuthorized(tipoinscricao.IdEvento))
                return Forbid();

            TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            return View(tipoInscricaoModel);
        }

        // GET: /TipoInscricao/Create?idEvento=X
        [HttpGet("Create")]
        public ActionResult Create(uint idEvento)
        {
            if (!IsAuthorized(idEvento))
                return Forbid();
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new TipoInscricaoModel();
            viewModel.IdEvento = idEvento;
            viewModel.Evento = new SelectList(eventos, "Id", "Nome");
            viewModel.DataInicio = DateTime.Now;
            viewModel.Datafim = DateTime.Now;

            return View(viewModel);
        }

        // POST: /TipoInscricao/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TipoInscricaoModel tipoInscricaoModel)
        {
            if (!IsAuthorized(tipoInscricaoModel.IdEvento))
                return Forbid();
            ModelState.Remove("Evento");
            if (ModelState.IsValid)
            {
                var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel);
                _tipoInscricaoService.Create(tipoinscricao);
                return RedirectToAction(nameof(Index), new { idEvento = tipoInscricaoModel.IdEvento });
            }

            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            tipoInscricaoModel.Evento = new SelectList(eventos, "Id", "Nome");
            return View(tipoInscricaoModel);
        }

        // GET: /TipoInscricao/Edit/5
        [HttpGet("Edit/{id}")]
        public ActionResult Edit(uint id)
        {
            var tipoinscricao = _tipoInscricaoService.Get(id);
            if (tipoinscricao == null)
            {
                return NotFound();
            }

            if (!IsAuthorized(tipoinscricao.IdEvento))
                return Forbid();

            var tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = tipoInscricaoModel;
            viewModel.Evento = new SelectList(eventos, "Id", "Nome");

            return View(viewModel);
        }

        // POST: /TipoInscricao/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, TipoInscricaoModel tipoInscricaoModel)
        {
            var existente = _tipoInscricaoService.Get(id);
            if (existente == null)
                return NotFound();
            if (!IsAuthorized(existente.IdEvento) || !IsAuthorized(tipoInscricaoModel.IdEvento))
                return Forbid();
            ModelState.Remove("Evento");
            if (ModelState.IsValid)
            {
                var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel);
                tipoinscricao.Id = id;
                _tipoInscricaoService.Edit(tipoinscricao);

                return RedirectToAction(nameof(Index), new { idEvento = tipoInscricaoModel.IdEvento });
            }

            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            tipoInscricaoModel.Evento = new SelectList(eventos, "Id", "Nome");
            return View(tipoInscricaoModel);
        }

        // GET: /TipoInscricao/Delete/5
        [HttpGet("Delete/{id}")]
        public ActionResult Delete(uint id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
            if (tipoinscricao == null) return NotFound();

            if (!IsAuthorized(tipoinscricao.IdEvento))
                return Forbid();

            TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);

            string nomeEvento = _eventoService.GetNomeById(tipoinscricao.IdEvento);
            tipoInscricaoModel.NomeEvento = nomeEvento;

            return View(tipoInscricaoModel);
        }

        // POST: /TipoInscricao/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, uint idEvento)
        {
            var existente = _tipoInscricaoService.Get(id);
            if (existente == null)
                return NotFound();
            if (!IsAuthorized(existente.IdEvento) || !IsAuthorized(idEvento))
                return Forbid();
            _tipoInscricaoService.Delete(id);
            return RedirectToAction(nameof(Index), new { idEvento = idEvento });
        }

        // GET: /TipoInscricao/CreateTipoInscricaoSubevento?idSubevento=X
        [HttpGet("CreateTipoInscricaoSubevento")]
        public ActionResult CreateTipoInscricaoSubevento(uint idSubevento)
        {
            var subevento = _subeventoService.Get(idSubevento);
            if (subevento == null) return NotFound();

            if (!IsAuthorized(subevento.IdEvento))
                return Forbid();

            var subeventoModel = _mapper.Map<SubeventoModel>(subevento);
            var tiposInscricaos = _tipoInscricaoService.GetByEventoUsadaSubevento(subevento.IdEvento);
            var tiposInscricaosSubevento = _tipoInscricaoService.GetTiposInscricaosSubevento(idSubevento);

            ViewData["EventoId"] = subevento.IdEvento;

            var view = new TipoInscricaoSubeventoModel()
            {
                NomeSubevento = subevento.Nome,
                IdSubevento = idSubevento,
                TiposInscricaos = new SelectList(tiposInscricaos, "Id", "Nome"),
                TiposInscricaosSubevento = tiposInscricaosSubevento
            };
            return View(view);
        }

        // POST: /TipoInscricao/CreateTipoInscricaoSubevento
        [HttpPost("CreateTipoInscricaoSubevento")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTipoInscricaoSubevento(TipoInscricaoSubeventoModel model)
        {
            var subeventoM = _subeventoService.Get(model.IdSubevento);
            if (subeventoM == null) return NotFound();
            if (!IsAuthorized(subeventoM.IdEvento))
                return Forbid();
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Por favor, preencha todos os campos obrigatórios.");

                var tiposInscricaos = _tipoInscricaoService.GetByEventoUsadaSubevento(subeventoM.IdEvento);
                var tiposInscricaosSubevento = _tipoInscricaoService.GetTiposInscricaosSubevento(model.IdSubevento);

                ViewData["EventoId"] = subeventoM.IdEvento;

                var view = new TipoInscricaoSubeventoModel()
                {
                    NomeSubevento = subeventoM.Nome,
                    IdSubevento = subeventoM.Id,
                    TiposInscricaos = new SelectList(tiposInscricaos, "Id", "Nome"),
                    TiposInscricaosSubevento = tiposInscricaosSubevento
                };
                return View(view);
            }

            try
            {
                _tipoInscricaoService.AssociacaoTipoInscricaoSubevento(model.IdSubevento, model.IdTipoInscricao);
                return RedirectToAction("CreateTipoInscricaoSubevento", new { model.IdSubevento });
            }
            catch (ServiceException ex)
            {
                var subevento = _subeventoService.Get(model.IdSubevento);

                ModelState.AddModelError("", ex.Message);
                var tiposInscricaos = _tipoInscricaoService.GetByEventoUsadaSubevento(subevento.IdEvento);
                var tiposInscricaosSubevento = _tipoInscricaoService.GetTiposInscricaosSubevento(model.IdSubevento);
                ViewData["EventoId"] = subevento.IdEvento;
                var view = new TipoInscricaoSubeventoModel()
                {
                    NomeSubevento = subevento.Nome,
                    IdSubevento = subevento.Id,
                    TiposInscricaos = new SelectList(tiposInscricaos, "Id", "Nome"),
                    TiposInscricaosSubevento = tiposInscricaosSubevento
                };
                return View(view);

            }

        }

        // POST: /TipoInscricao/DeleteTipoInscricaoSubevento
        [HttpPost("DeleteTipoInscricaoSubevento")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteTipoInscricaoSubevento(uint idSubevento, uint IdTipoInscricao)
        {
            var subevento = _subeventoService.Get(idSubevento);
            if (subevento == null) return NotFound();
            if (!IsAuthorized(subevento.IdEvento))
                return Forbid();
            _tipoInscricaoService.DeleteTipoInscricaoSubevento(idSubevento, IdTipoInscricao);
            return RedirectToAction("CreateTipoInscricaoSubevento", new { idSubevento });
        }
    }
}
