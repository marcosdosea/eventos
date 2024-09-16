using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace EventoWeb.Controllers
{
    public class ModeloCertificadoController : Controller
    {
        private readonly IModeloCertificadoService _modeloCertificadoService;
        private readonly IMapper _mapper;
        private readonly IEventoService _eventoService;
        private readonly ISubeventoService _subeventoService;

        public ModeloCertificadoController(IEventoService eventoService, IModeloCertificadoService modeloCertificadoService, IMapper mapper, ISubeventoService subeventoService)
        {
            _modeloCertificadoService = modeloCertificadoService;
            _eventoService = eventoService;
            _mapper = mapper;
            _subeventoService = subeventoService;
        }

        public IActionResult Index()
        {
            var evento = _eventoService.GetAll().ToList();
            if (evento.Count >= 1)
            {
                var modeloModel = _modeloCertificadoService.GetAll().ToList();
                return View(modeloModel);
            }
            else
            {
                return View();
            }

        }

        public IActionResult Create(uint idEvento)
        {
            var evento = _eventoService.GetEventoSimpleDto(idEvento);
            if (ModelState.IsValid)
            {
                if (evento.Id != null)
                {
                    var viewModel = new ModeloCertificadoModel();
                    return View(viewModel);
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModeloCertificadoModel modeloCertificadoModel)
        {
            if (ModelState.IsValid)
            {
                string? logoSuperiorBase64 = null;
                string? assinatura1Base64 = null;
                string? assinatura2Base64 = null;

                ProcessImageFile(modeloCertificadoModel.LogotipoSuperior, "LogotipoSuperior", out logoSuperiorBase64);
                ProcessImageFile(modeloCertificadoModel.Assinatura1, "Assinatura1", out assinatura1Base64);

                if (modeloCertificadoModel.Assinatura2 != null)
                {
                    ProcessImageFile(modeloCertificadoModel.Assinatura2, "Assinatura2", out assinatura2Base64);
                }

                modeloCertificadoModel.LogotipoSuperiorBase64 = logoSuperiorBase64;
                modeloCertificadoModel.Assinatura1Base64 = assinatura1Base64;
                modeloCertificadoModel.Assinatura2Base64 = assinatura2Base64;

                if (!ModelState.IsValid)
                {
                    return View(modeloCertificadoModel);
                }

                var modeloCertificado = _mapper.Map<Modelocertificado>(modeloCertificadoModel);

                try
                {
                    _modeloCertificadoService.Create(modeloCertificado);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar o modelo de certificado. Tente novamente.");
                    return View(modeloCertificadoModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = modeloCertificadoModel.IdEvento });
            }
            return View(modeloCertificadoModel);
        }

        private void ProcessImageFile(IFormFile file, string fieldName, out string? base64String)
        {
            base64String = null;
            if (file != null && file.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    if (memoryStream.Length <= 1048576) // 1 MB
                    {
                        base64String = Convert.ToBase64String(memoryStream.ToArray());
                    }
                    else
                    {
                        ModelState.AddModelError($"ModeloCertificado.{fieldName}", $"O arquivo {fieldName} é muito grande. Deve ser menor que 1 MB.");
                    }
                }
            }
            else if (fieldName != "Assinatura2") // Assinatura2 é opcional
            {
                ModelState.AddModelError($"ModeloCertificado.{fieldName}", $"O arquivo {fieldName} é obrigatório.");
            }
        }
    }
}
