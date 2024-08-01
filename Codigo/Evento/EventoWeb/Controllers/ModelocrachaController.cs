using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Service;
using Util;

namespace EventoWeb.Controllers
{
    public class ModelocrachaController : Controller
    {
        private readonly IModelocrachaService _modelocrachaService;
        private readonly IEventoService _eventoService;
        private readonly IMapper _mapper;

        public ModelocrachaController(IModelocrachaService modelocrachaService, IEventoService eventoService, IMapper mapper)
        {
            _modelocrachaService = modelocrachaService;
            _eventoService = eventoService;
            _mapper = mapper;
        }

        // GET: ModelocrachaController
        public ActionResult Index()
        {
            var listaModeloCrachas = _modelocrachaService.GetAll().ToList();
            var listaModeloCrachaModel = listaModeloCrachas.Select(m =>
            {
                var model = _mapper.Map<ModelocrachaModel>(m);
                var evento = _eventoService.Get(m.IdEvento);
                model.NomeEvento = evento != null ? evento.Nome : "Evento não encontrado"; 
                if (model.Qrcode == 1)
                {
                    var qrCodeBytes = QrCodeGenerator.GenerateQr(model.Texto);
                    model.QrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
                }
                return model;
            }).ToList();
            return View(listaModeloCrachaModel);
        }


        // GET: ModelocrachaController/Details/5
        public ActionResult Details(uint id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            var evento = _eventoService.GetEventoSimpleDto(modelocracha.IdEvento);
            var modelocrachaModel = _mapper.Map<ModelocrachaModel>(modelocracha);

            modelocrachaModel.NomeEvento = _eventoService.GetNomeById(modelocracha.IdEvento);
            modelocrachaModel.LogotipoBase64 = modelocracha.Logotipo != null
                ? Convert.ToBase64String(modelocracha.Logotipo)
                : null;

            if (modelocracha.Qrcode == 1)
            {
                var qrCodeBytes = QrCodeGenerator.GenerateQr(modelocracha.Texto);
                modelocrachaModel.QrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
            }

            return View(modelocrachaModel);
        }

        // GET: ModelocrachaController/Create
        public ActionResult Create(uint idEvento)
        {
            var modelocrachaModel = new ModelocrachaModel();
            var evento = _eventoService.GetEventoSimpleDto(idEvento);
            var viewModel = new ModelocrachaCreateModel
            {
                Modelocracha = modelocrachaModel,
                Evento = evento
            };
            return View(viewModel);
        }

        // POST: ModelocrachaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModelocrachaCreateModel modelocrachaModel)
        {
            if (ModelState.IsValid)
            {
                byte[] logoTipoSource = null;
                if (modelocrachaModel.Modelocracha.Logotipo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        modelocrachaModel.Modelocracha.Logotipo.CopyTo(memoryStream);
                        // Upload the file if less than 1 MB  
                        if (memoryStream.Length < 1046026)
                        {

                            logoTipoSource = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("File", "O arquivo é muito grande.");
                        }
                    }
                }

                modelocrachaModel.Modelocracha.IdEvento = modelocrachaModel.Evento.Id;
                var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel.Modelocracha);
                modelocracha.Logotipo = logoTipoSource;
                _modelocrachaService.Create(modelocracha);
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: ModelocrachaController/Edit/5
        public ActionResult Edit(uint id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            if (modelocracha == null)
            {
                return NotFound();
            }
            var modelocrachaModel = _mapper.Map<ModelocrachaModel>(modelocracha);
            var evento = _eventoService.GetEventoSimpleDto(modelocracha.IdEvento);
            var viewModel = new ModelocrachaCreateModel
            {
                Modelocracha = modelocrachaModel,
                Evento = evento
            };
            return View(viewModel);
        }

        // POST: ModelocrachaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, ModelocrachaCreateModel viewModel)
            {
            viewModel.Modelocracha.Id = id;
            viewModel.Modelocracha.IdEvento = viewModel.Evento.Id;
            var modelo = _mapper.Map<Modelocracha>(viewModel.Modelocracha);
            _modelocrachaService.Edit(modelo);
            return RedirectToAction(nameof(Index));
        }

        // GET: ModelocrachaController/Delete/5
        public ActionResult Delete(uint id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            var modelocrachaModel = _mapper.Map<ModelocrachaModel>(modelocracha);

            modelocrachaModel.NomeEvento = _eventoService.GetNomeById(modelocracha.IdEvento);
            modelocrachaModel.LogotipoBase64 = modelocracha.Logotipo != null
                ? Convert.ToBase64String(modelocracha.Logotipo)
                : null;

            if (modelocracha.Qrcode == 1)
            {
                var qrCodeBytes = QrCodeGenerator.GenerateQr(modelocracha.Texto);
                modelocrachaModel.QrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
            }
            return View(modelocrachaModel);
        }


        // POST: ModelocrachaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, ModelocrachaModel modelocrachaModel)
        {
            _modelocrachaService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
