using System.Linq;
using AutoMapper;
using Core.Service;
using Core;
using EventoWeb.Helpers;
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
            return AutorizacaoEventoHelper.IsAutorizado(User, _inscricaoService, idEvento);
        }

        private SelectList EventosSelectList(uint? selecionado = null)
        {
            if (User.IsInRole("ADMINISTRADOR"))
                return new SelectList(_eventoService.GetAllSimpleDto(), "Id", "Nome", selecionado);
            var username = User.Identity?.Name;
            var meusEventos = string.IsNullOrEmpty(username)
                ? Enumerable.Empty<Core.DTO.EventoSimpleDTO>()
                : _eventoService.GetEventByCpf(username, 2)?.Select(e => new Core.DTO.EventoSimpleDTO { Id = e.Id, Nome = e.Nome }) ?? Enumerable.Empty<Core.DTO.EventoSimpleDTO>();
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
        public IActionResult Create(uint? idEvento = null)
        {
            if (idEvento.HasValue && !IsAuthorized(idEvento.Value))
                return Forbid();

            ViewData["EventoId"] = idEvento;
            var model = new ModelocertificadoModel
            {
                IdEvento = idEvento ?? 0,
                Eventos = EventosSelectList(idEvento)
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
            entity.LogotipoSuperior = Array.Empty<byte>();
            entity.Assinatura1 = Array.Empty<byte>();
            entity.Assinatura1Texto = string.Empty;
            entity.TextoAntesCargaHoraria = "com carga horária de";
            try
            {
                _service.Create(entity);
                return RedirectToAction(nameof(Index), new { idEvento = entity.IdEvento });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Eventos = EventosSelectList(model.IdEvento);
                return View(model);
            }
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

            _mapper.Map(model, existente);
            try
            {
                _service.Update(existente);
                return RedirectToAction(nameof(Index), new { idEvento = existente.IdEvento });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Eventos = EventosSelectList(model.IdEvento);
                return View(model);
            }
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
            return RedirectToAction(nameof(Index), new { idEvento = existente.IdEvento });
        }
    }
}
