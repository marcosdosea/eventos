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
        private readonly IPessoaService _pessoaService;
        private readonly IInscricaoService _inscricaoService;
        private readonly IMapper _mapper;

        public ModelocrachaController(IModelocrachaService modelocrachaService, IEventoService eventoService, IPessoaService pessoaSevice, IInscricaoService inscricaoService, IMapper mapper)
        {
            _modelocrachaService = modelocrachaService;
            _eventoService = eventoService;
            _pessoaService = pessoaSevice;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
        }

        // GET: ModelocrachaController
        public ActionResult Index(uint? idEvento)
        {
            if (idEvento.HasValue)
            {
                var listaModeloCrachas = _modelocrachaService.GetByEvento(idEvento.Value).ToList();
                var listaModeloCrachaModel = listaModeloCrachas.Select(m =>
                {
                    var model = _mapper.Map<ModelocrachaModel>(m);
                    var evento = _eventoService.Get(m.IdEvento);
                    model.NomeEvento = evento != null ? evento.Nome : "Evento não encontrado";
                    return model;
                }).ToList();

                ViewData["EventoId"] = idEvento.Value;
                ViewData["EventoNome"] = _eventoService.GetNomeById(idEvento.Value);
                return View(listaModeloCrachaModel);
            }
            else
            {
                return RedirectToAction("Index", "Evento");
            }

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
                var inscricoes = _inscricaoService.GetByEvento(modelocracha.IdEvento);

                if (inscricoes != null && inscricoes.Any())
                {
                    foreach (var inscricao in inscricoes)
                    {
                        modelocrachaModel.Inscricoes.Add($"Evento:{inscricao.IdEvento}, Pessoa:{inscricao.IdPessoa}, Papel:{inscricao.IdPapel}");
                    }

                    var conteudoQrCode = string.Join(";", modelocrachaModel.Inscricoes);
                    var qrCodeBytes = QrCodeGenerator.GenerateQr(conteudoQrCode);
                    modelocrachaModel.QrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
                }
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
                if (modelocrachaModel.Modelocracha.Logotipo != null && modelocrachaModel.Modelocracha.Logotipo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        modelocrachaModel.Modelocracha.Logotipo.CopyTo(memoryStream);

                        if (memoryStream.Length <= 65535)
                        {
                            logoTipoSource = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("Modelocracha.Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            return View(modelocrachaModel); // Retorna a view com a mensagem de erro
                        }
                    }
                }

                modelocrachaModel.Modelocracha.IdEvento = modelocrachaModel.Evento.Id;
                var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel.Modelocracha);
                modelocracha.Logotipo = logoTipoSource;

                try
                {
                    _modelocrachaService.Create(modelocracha);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar o modelo de crachá. Tente novamente.");
                    return View(modelocrachaModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = modelocrachaModel.Modelocracha.IdEvento });
            }

            return View(modelocrachaModel);
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
            if (ModelState.IsValid)
            {
                byte[] logoTipoSource = null;
                if (viewModel.Modelocracha.Logotipo != null && viewModel.Modelocracha.Logotipo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        viewModel.Modelocracha.Logotipo.CopyTo(memoryStream);

                        if (memoryStream.Length <= 65535)
                        {
                            logoTipoSource = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("Modelocracha.Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            return View(viewModel); // Retorna a view com a mensagem de erro
                        }
                    }
                }

                viewModel.Modelocracha.IdEvento = viewModel.Evento.Id;
                var modelocracha = _mapper.Map<Modelocracha>(viewModel.Modelocracha);
                modelocracha.Logotipo = logoTipoSource;

                try
                {
                    _modelocrachaService.Edit(modelocracha);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar o modelo de crachá. Tente novamente.");
                    return View(viewModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = viewModel.Modelocracha.IdEvento });
            }

            return View(viewModel);
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
            return View(modelocrachaModel);
        }


        // POST: ModelocrachaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, uint idEvento)
        {
            _modelocrachaService.Delete(id);
            return RedirectToAction(nameof(Index), new { idEvento = idEvento });
        }
    }
}
