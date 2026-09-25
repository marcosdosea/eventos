using System.Linq;
using AutoMapper;
using Core.Service;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
    [Route("[controller]")]
    public class ModelocertificadoController : Controller
    {
        private readonly IModelocertificadoService _service;
        private readonly IEventoService _eventoService;
        private readonly IInscricaoService _inscricaoService;
        private readonly IMapper _mapper;

        public ModelocertificadoController(
            IModelocertificadoService service,
            IEventoService eventoService,
            IInscricaoService inscricaoService,
            IMapper mapper)
        {
            _service = service;
            _eventoService = eventoService;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
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

        private SelectList EventosSelectList(uint? selecionado = null)
        {
            if (User.IsInRole("ADMINISTRADOR"))
                return new SelectList(_eventoService.GetAll(), "Id", "Nome", selecionado);
            var username = User.Identity?.Name;
            var meusEventos = string.IsNullOrEmpty(username)
                ? Enumerable.Empty<Evento>()
                : _eventoService.GetEventByCpf(username, 2) ?? Enumerable.Empty<Evento>();
            return new SelectList(meusEventos, "Id", "Nome", selecionado);
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index(uint? idEvento)
        {
            var items = _service.GetAll();
            if (idEvento.HasValue)
            {
                if (!IsAuthorized(idEvento.Value))
                    return Forbid();
                items = items.Where(x => x.IdEvento == idEvento.Value);
                ViewData["EventoId"] = idEvento.Value;
            }
            else if (!User.IsInRole("ADMINISTRADOR"))
            {
                items = items.Where(x => IsAuthorized(x.IdEvento));
            }
            var model = items.Select(x => _mapper.Map<ModelocertificadoModel>(x)).ToList();
            return View(model);
        }

        [HttpGet]
        [Route("Details/{id:int}")]
        public IActionResult Details(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
            if (!IsAuthorized(entity.IdEvento))
                return Forbid();
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            return View(model);
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            var model = new ModelocertificadoModel
            {
                DataEmissao = System.DateTime.Now,
                Eventos = EventosSelectList()
            };
            return View(model);
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ModelocertificadoModel model)
        {
            if (!IsAuthorized(model.IdEvento))
                return Forbid();
            if (!ModelState.IsValid)
            {
                model.Eventos = EventosSelectList(model.IdEvento);
                return View(model);
            }

            var entity = _mapper.Map<Modelocertificado>(model);
            _service.Create(entity);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("Edit/{id:int}")]
        public IActionResult Edit(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
            if (!IsAuthorized(entity.IdEvento))
                return Forbid();
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            model.Eventos = EventosSelectList(model.IdEvento);
            return View(model);
        }

        [HttpPost]
        [Route("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ModelocertificadoModel model)
        {
            if ((uint)id != model.Id) return BadRequest();
            var existente = _service.Get((uint)id);
            if (existente == null) return NotFound();
            if (!IsAuthorized(existente.IdEvento) || !IsAuthorized(model.IdEvento))
                return Forbid();
            if (!ModelState.IsValid)
            {
                model.Eventos = EventosSelectList(model.IdEvento);
                return View(model);
            }

            var entity = _mapper.Map<Modelocertificado>(model);
            _service.Update(entity);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("Delete/{id:int}")]
        public IActionResult Delete(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
            if (!IsAuthorized(entity.IdEvento))
                return Forbid();
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            return View(model);
        }

        [HttpPost]
        [Route("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var existente = _service.Get((uint)id);
            if (existente == null) return NotFound();
            if (!IsAuthorized(existente.IdEvento))
                return Forbid();
            _service.Delete((uint)id);
            return RedirectToAction(nameof(Index));
        }
    }
}
