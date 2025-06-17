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
    [Route("modelocertificados")]
    public class ModelocertificadoController : Controller
    {
        private readonly IModelocertificadoService _service;
        private readonly IEventoService _eventoService;
        private readonly IMapper _mapper;

        public ModelocertificadoController(
            IModelocertificadoService service,
            IEventoService eventoService,
            IMapper mapper)
        {
            _service = service;
            _eventoService = eventoService;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var items = _service.GetAll();
            var model = items.Select(x => _mapper.Map<ModelocertificadoModel>(x)).ToList();
            return View(model);
        }

        [HttpGet("{id:int}")]
        public IActionResult Details(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            return View(model);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            var model = new ModelocertificadoModel
            {
                DataEmissao = System.DateTime.Now,
                Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome")
            };
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ModelocertificadoModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome", model.IdEvento);
                return View(model);
            }

            var entity = _mapper.Map<Modelocertificado>(model);
            _service.Create(entity);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public IActionResult Edit(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            model.Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome", model.IdEvento);
            return View(model);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ModelocertificadoModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome", model.IdEvento);
                return View(model);
            }

            var entity = _mapper.Map<Modelocertificado>(model);
            _service.Update(entity);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("delete/{id:int}")]
        public IActionResult Delete(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            return View(model);
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _service.Delete((uint)id);
            return RedirectToAction(nameof(Index));
        }
    }
}
