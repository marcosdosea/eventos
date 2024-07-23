using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace EventoWeb.Controllers
{
    public class ModelocrachaController : Controller
    {
        private readonly IModelocrachaService _modelocrachaService;
        private readonly IMapper _mapper;

        public ModelocrachaController(IModelocrachaService modelocrachaService, IMapper mapper)
        {
            _modelocrachaService = modelocrachaService;
            _mapper = mapper;
        }

        // GET: ModelocrachaController
        public IActionResult Index()
        {
            var listaModeloCracha = _modelocrachaService.GetAll();
            var listaModeloCrachaModel = _mapper.Map<List<ModelocrachaModel>>(listaModeloCracha);
            return View(listaModeloCrachaModel);
        }

        // GET: ModelocrachaController/Details/5
        public ActionResult Details(uint id)
        {
            Modelocracha modelocracha = _modelocrachaService.Get(id);
            ModelocrachaModel modelocrachamodel = _mapper.Map<ModelocrachaModel>(modelocracha);
            return View(modelocrachamodel);
        }

        // GET: ModelocrachaController/Create
        public ActionResult Create()
        {
            var modelocrachaModel = new ModelocrachaModel();
            return View(modelocrachaModel);
        }

        // POST: ModelocrachaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModelocrachaModel modelocrachaModel)
        {
            if (ModelState.IsValid)
            {
                byte[] logoTipoSource = null;
                if (modelocrachaModel.Logotipo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        modelocrachaModel.Logotipo.CopyTo(memoryStream);
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

                var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel);
                modelocracha.Logotipo = logoTipoSource;
                _modelocrachaService.Create(modelocracha);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: ModelocrachaController/Edit/5
        public ActionResult Edit(uint id)
        {
            return Details(id);
        }

        // POST: ModelocrachaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, ModelocrachaModel modelocrachaModel)
        {
            if (ModelState.IsValid)
            {
                var modelo = _mapper.Map<Modelocracha>(modelocrachaModel);
                _modelocrachaService.Edit(modelo);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: ModelocrachaController/Delete/5
        public ActionResult Delete(uint id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            var modelocrachaModel = _mapper.Map<ModelocrachaModel>(modelocracha);
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
