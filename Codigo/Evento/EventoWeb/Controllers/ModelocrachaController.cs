﻿using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Util;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize(Roles = "GESTOR")]
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
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index(uint? idEvento, uint? idPessoa)
        {
            if (idEvento.HasValue)
            {
                var listaModeloCrachas = _modelocrachaService.GetByEvento(idEvento.Value).ToList();
                var listaModeloCrachaModel = listaModeloCrachas.Select(m =>
                {
                    var model = _mapper.Map<ModelocrachaModel>(m);
                    var evento = _eventoService.Get(m.IdEvento);
                    model.NomeEvento = evento != null ? evento.Nome : "Evento não encontrado";
                    model.IdPessoa = idPessoa;
                    return model;
                }).ToList();
                if (idPessoa.HasValue)
                {
					ViewData["PessoaId"] = idPessoa.Value;
				}
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
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(uint id, uint? idPessoa)
        {
            var modelocracha = _modelocrachaService.Get(id);
            var modelocrachaModel = _mapper.Map<ModelocrachaModel>(modelocracha);
            modelocrachaModel.NomeEvento = _eventoService.GetNomeById(modelocracha.IdEvento);
            modelocrachaModel.LogotipoBase64 = modelocracha.Logotipo != null
                ? Convert.ToBase64String(modelocracha.Logotipo)
                : null;
            if (modelocracha.Qrcode == 1)
            {
                var inscricoessub = _inscricaoService.GetSubByEvento(modelocracha.IdEvento);
                var inscricoesev = _inscricaoService.GetByEvento(modelocracha.IdEvento);
                if (inscricoesev != null && inscricoessub != null && inscricoesev.Any())
                {
                    if (idPessoa.HasValue)
                    {
                        modelocrachaModel.IdPessoa = idPessoa.Value;
                        modelocrachaModel.QrCodes = inscricoesev
							.Where(inscricao => inscricao.IdPapel == 4 && inscricao.IdPessoa == idPessoa)
							.Select(inscricao =>
							{
								var subeventosIdsPessoa = inscricoessub
									.Where(sub => sub.IdPessoa == inscricao.IdPessoa)
									.Select(sub => sub.IdSubEvento)
									.Distinct()
									.ToList();
								var conteudoQrCode = $"[{inscricao.IdPessoa}] [{modelocracha.IdEvento}]";
								if (subeventosIdsPessoa.Any())
								{
									conteudoQrCode += $" {string.Join(" ", subeventosIdsPessoa.Select(idSubEvento => $"[{idSubEvento}]"))}";
								}
								var qrCodeBytes = QrCodeGenerator.GenerateQr(conteudoQrCode);
								return Convert.ToBase64String(qrCodeBytes);
							}).ToList();
					}
                    else
                    {
						modelocrachaModel.QrCodes = inscricoesev
							.Where(inscricao => inscricao.IdPapel == 4)
							.Select(inscricao =>
							{
								var subeventosIdsPessoa = inscricoessub
									.Where(sub => sub.IdPessoa == inscricao.IdPessoa)
									.Select(sub => sub.IdSubEvento)
									.Distinct()
									.ToList();
								var conteudoQrCode = $"[{inscricao.IdPessoa}] [{inscricao.NomeCracha}] [{modelocracha.IdEvento}]";
								if (subeventosIdsPessoa.Any())
								{
									conteudoQrCode += $" {string.Join(" ", subeventosIdsPessoa.Select(idSubEvento => $"[{idSubEvento}]"))}";
								}
								var qrCodeBytes = QrCodeGenerator.GenerateQr(conteudoQrCode);
								return Convert.ToBase64String(qrCodeBytes);
							}).ToList();
					}
                }
            }
            return View(modelocrachaModel);
        }

        // GET: ModelocrachaController/Create
        [HttpGet]
        [Route("Create/{idEvento}")]
        public ActionResult Create(uint idEvento)
        {
            var evento = _eventoService.GetEventoSimpleDto(idEvento);
            var viewModel = new ModelocrachaModel();
            viewModel.Evento = evento;
            return View(viewModel);
        }

        // POST: ModelocrachaController/Create
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModelocrachaModel modelocrachaModel)
        {
            if (ModelState.IsValid)
            {
                byte[] logoTipoSource = null;
                if (modelocrachaModel.Logotipo != null && modelocrachaModel.Logotipo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        modelocrachaModel.Logotipo.CopyTo(memoryStream);

                        if (memoryStream.Length <= 65535)
                        {
                            logoTipoSource = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("Modelocracha.Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            return View(modelocrachaModel);
                        }
                    }
                }

                modelocrachaModel.IdEvento = modelocrachaModel.Evento.Id;
                var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel);
                modelocracha.Logotipo = logoTipoSource;

                try
                {
                    _modelocrachaService.Create(modelocracha);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar o modelo de crachá. Tente novamente.");
                    return View(modelocrachaModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = modelocrachaModel.IdEvento });
            }

            return View(modelocrachaModel);
        }


        // GET: ModelocrachaController/Edit/5
        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(uint id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            if (modelocracha == null)
            {
                return NotFound();
            }
            var viewModel = _mapper.Map<ModelocrachaModel>(modelocracha);
            viewModel.Evento = _eventoService.GetEventoSimpleDto(modelocracha.IdEvento);
            
            return View(viewModel);
        }

        // POST: ModelocrachaController/Edit/5
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, ModelocrachaModel viewModel)
        {
            viewModel.Id = id;
            if (ModelState.IsValid)
            {
                byte[] logoTipoSource = null;
                if (viewModel.Logotipo != null && viewModel.Logotipo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        viewModel.Logotipo.CopyTo(memoryStream);

                        if (memoryStream.Length <= 65535)
                        {
                            logoTipoSource = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("Modelocracha.Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            return View(viewModel);
                        }
                    }
                }

                var modelocracha = _mapper.Map<Modelocracha>(viewModel);
                if (logoTipoSource != null)
                {
                    modelocracha.Logotipo = logoTipoSource;
                }

                try
                {
                    _modelocrachaService.Edit(modelocracha);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao atualizar o modelo de crachá. Tente novamente.");
                    return View(viewModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = modelocracha.IdEvento });
            }

            return View(viewModel);
        }

        // GET: ModelocrachaController/Delete/5
        [HttpGet]
        [Route("Delete/{id}")]
        public ActionResult Delete(uint id)
        {
            var modelocracha = _modelocrachaService.Get(id);
            if (modelocracha == null)
            {
                return NotFound();
            }
            var viewModel = _mapper.Map<ModelocrachaModel>(modelocracha);
            viewModel.Evento = _eventoService.GetEventoSimpleDto(modelocracha.IdEvento);
            return View(viewModel);
        }

        // POST: ModelocrachaController/Delete/5
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, uint idEvento)
        {
            _modelocrachaService.Delete(id);
            return RedirectToAction(nameof(Index), new { idEvento = idEvento });
        }
    }
}
