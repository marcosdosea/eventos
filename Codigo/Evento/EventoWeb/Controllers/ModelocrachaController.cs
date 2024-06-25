using AutoMapper;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Core.Service;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public ActionResult Details(int id)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ModelocrachaModel modelocrachaModel)
        {
            // Verifica se o arquivo foi fornecido e está dentro do tamanho permitido
            if (modelocrachaModel.LogotipoFile != null)
            {
                if (modelocrachaModel.LogotipoFile.Length > 65536)
                {
                    ModelState.AddModelError("LogotipoFile", "O tamanho máximo permitido para o arquivo é de 64 KB.");
                    return View(modelocrachaModel);
                }
                
                if (!_modelocrachaService.IsImage(modelocrachaModel.LogotipoFile))
                {
                    ModelState.AddModelError("LogotipoFile", "O arquivo fornecido não é uma imagem válida.");
                    return View(modelocrachaModel);
                }
                
                using (var memoryStream = new MemoryStream())
                {
                    await modelocrachaModel.LogotipoFile.CopyToAsync(memoryStream);
                    modelocrachaModel.Logotipo = memoryStream.ToArray();
                }
            }
            var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel);
            _modelocrachaService.Create(modelocracha);
                             
            return RedirectToAction(nameof(Index));
        }
        
        // GET: ModelocrachaController/Edit/5
        public ActionResult Edit(int id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            var modelocrachaModel = _mapper.Map<ModelocrachaModel>(modelocracha);
            return View(modelocrachaModel);
        }

        // POST: ModelocrachaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, ModelocrachaModel modelocrachaModel)
        {
            if (ModelState.IsValid)
            {
                // Verifica se foi fornecido um novo arquivo de logotipo
                if (modelocrachaModel.LogotipoFile != null && modelocrachaModel.LogotipoFile.Length > 0)
                {
                    // Verifica o tamanho do arquivo
                    if (modelocrachaModel.LogotipoFile.Length > 65536)
                    {
                        ModelState.AddModelError("LogotipoFile", "O tamanho máximo permitido para o arquivo é de 64 KB.");
                        return View(modelocrachaModel);
                    }

                    // Verifica se é uma imagem válida
                    if (!_modelocrachaService.IsImage(modelocrachaModel.LogotipoFile))
                    {
                        ModelState.AddModelError("LogotipoFile", "O arquivo fornecido não é uma imagem válida.");
                        return View(modelocrachaModel);
                    }

                    // Se tudo estiver correto, converte o arquivo para byte[]
                    using (var memoryStream = new MemoryStream())
                    {
                        await modelocrachaModel.LogotipoFile.CopyToAsync(memoryStream);
                        modelocrachaModel.Logotipo = memoryStream.ToArray();
                    }
                }

                // Mapeia e atualiza o modelo
                var modelo = _mapper.Map<Modelocracha>(modelocrachaModel);
                _modelocrachaService.Edit(modelo);

                return RedirectToAction(nameof(Index));
            }

            return View(modelocrachaModel);
        }


        // GET: ModelocrachaController/Delete/5
        public ActionResult Delete(int id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            var modelocrachaModel = _mapper.Map<ModelocrachaModel>(modelocracha);
            return View(modelocrachaModel);
        }

        // POST: ModelocrachaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, ModelocrachaModel modelocrachaModel)
        {
            _modelocrachaService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
