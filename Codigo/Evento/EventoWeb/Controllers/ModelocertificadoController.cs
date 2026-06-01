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
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            var items = _service.GetAll();
            var model = items.Select(x => _mapper.Map<ModelocertificadoModel>(x)).ToList();
            return View(model);
        }

        [HttpGet]
        [Route("Details/{id:int}")]
        public IActionResult Details(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
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
                Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome")
            };
            return View(model);
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ModelocertificadoModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome", model.IdEvento);
                return View(model);
            }


            byte[] logotipoSource = null;
            byte[] assinatura1Source = null;
            byte[] assinatura2Source = null;
            const long MaxSize = 65535; 

            if (model.LogotipoSuperior != null && model.LogotipoSuperior.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    model.LogotipoSuperior.CopyTo(ms);
                    if (ms.Length <= MaxSize) logotipoSource = ms.ToArray();
                    else
                    {
                        ModelState.AddModelError("LogotipoSuperior", "O logotipo superior deve ser menor ou igual a 64KB.");
                        return View(model);
                    }
                }
            }

            if (model.Assinatura1 != null && model.Assinatura1.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    model.Assinatura1.CopyTo(ms);
                    if (ms.Length <= MaxSize) assinatura1Source = ms.ToArray();
                    else
                    {
                        ModelState.AddModelError("Assinatura1", "A assinatura 1 deve ser menor ou igual a 64KB.");
                        return View(model);
                    }
                }
            }

            if (model.Assinatura2 != null && model.Assinatura2.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    model.Assinatura2.CopyTo(ms);
                    if (ms.Length <= MaxSize) assinatura2Source = ms.ToArray();
                    else
                    {
                        ModelState.AddModelError("Assinatura2", "A assinatura 2 deve ser menor ou igual a 64KB.");
                        return View(model);
                    }
                }
            }
            model.Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome", model.IdEvento);
            var modeloCertificado = _mapper.Map<Modelocertificado>(model);
            modeloCertificado.LogotipoSuperior = logotipoSource;
            modeloCertificado.Assinatura1 = assinatura1Source;
            modeloCertificado.Assinatura2 = assinatura2Source;

            try
            {
                _service.Create(modeloCertificado);
                return RedirectToAction(nameof(Index), new { idEvento = model.IdEvento });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Ocorreu um erro ao criar o modelo de certificado: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        [Route("Edit/{id:int}")]
        public IActionResult Edit(int id)
        {
            var entity = _service.Get((uint)id);
            if (entity == null) return NotFound();
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            model.Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome", model.IdEvento);
            return View(model);
        }

        [HttpPost]
        [Route("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ModelocertificadoModel model)
        {
            if (!ModelState.IsValid || model.IdEvento == 0)
            {
                model.Eventos = new SelectList(_eventoService.GetAll(), "Id", "Nome", model.IdEvento);
                if (model.IdEvento == 0) ModelState.AddModelError("IdEvento", "Selecione o Evento");
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
            var model = _mapper.Map<ModelocertificadoModel>(entity);
            return View(model);
        }

        [HttpPost]
        [Route("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _service.Delete((uint)id);
            return RedirectToAction(nameof(Index));
        }
    }
}