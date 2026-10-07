using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Helpers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Util;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
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

        private bool IsAuthorized(uint idEvento)
        {
            return AutorizacaoEventoHelper.IsAutorizado(User, _inscricaoService, idEvento);
        }

        private List<EventoSimpleDTO> ObterEventosDoUsuario()
        {
            var eventos = new List<EventoSimpleDTO>();
            if (User.IsInRole("ADMINISTRADOR"))
            {
                var todos = _eventoService.GetAll();
                if (todos != null)
                {
                    eventos = todos.Select(e => new EventoSimpleDTO { Id = e.Id, Nome = e.Nome }).ToList();
                }
            }
            else
            {
                var cpf = User.Identity?.Name;
                if (!string.IsNullOrEmpty(cpf))
                {
                    try
                    {
                        var eventosGestor = _eventoService.GetEventByCpf(cpf, 2);
                        if (eventosGestor != null)
                        {
                            eventos = eventosGestor.Select(e => new EventoSimpleDTO { Id = e.Id, Nome = e.Nome }).ToList();
                        }
                    }
                    catch
                    {
                        // Degradação graciosa
                    }
                }
            }
            return eventos;
        }

        // GET: ModelocrachaController
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index(uint? idEvento, uint? idPessoa)
        {
            if (idEvento.HasValue)
            {
                if (!IsAuthorized(idEvento.Value))
                    return Forbid();
                var listaModeloCrachas = _modelocrachaService.GetByEvento(idEvento.Value).ToList();
                var listaModeloCrachaModel = listaModeloCrachas.Select(m =>
                {
                    var model = _mapper.Map<ModelocrachaModel>(m);
                    var evento = _eventoService.Get(m.IdEvento);
                    model.NomeEvento = evento != null ? evento.Nome : "Evento não encontrado";
                    model.IdPessoa = idPessoa;
                    if (m.Logotipo != null && m.Logotipo.Length > 0)
                    {
                        model.LogotipoBase64 = Convert.ToBase64String(m.Logotipo);
                        model.NomeArquivo = "logo_institucional.png";
                        var kb = Math.Round((double)m.Logotipo.Length / 1024.0, 1);
                        model.TamanhoArquivo = $"Binário BLOB • {kb} KB";
                    }
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
                var items = _modelocrachaService.GetAll();
                if (!User.IsInRole("ADMINISTRADOR"))
                {
                    items = items.Where(x => IsAuthorized(x.IdEvento)).ToList();
                }
                var model = items.Select(x =>
                {
                    var m = _mapper.Map<ModelocrachaModel>(x);
                    m.NomeEvento = _eventoService.GetNomeById(x.IdEvento);
                    if (x.Logotipo != null && x.Logotipo.Length > 0)
                    {
                        m.LogotipoBase64 = Convert.ToBase64String(x.Logotipo);
                        m.NomeArquivo = "logo_institucional.png";
                        var kb = Math.Round((double)x.Logotipo.Length / 1024.0, 1);
                        m.TamanhoArquivo = $"Binário BLOB • {kb} KB";
                    }
                    return m;
                }).ToList();
                return View(model);
            }
        }

        // GET: ModelocrachaController/Details/5
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(uint id, uint? idPessoa)
        {
            var modelocracha = _modelocrachaService.Get(id);
            if (modelocracha == null) return NotFound();
            if (!IsAuthorized(modelocracha.IdEvento))
                return Forbid();
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
        [Route("Create")]
        [Route("Create/{idEvento}")]
        public ActionResult Create(uint? idEvento)
        {
            var eventosDisponiveis = ObterEventosDoUsuario();
            ViewBag.EventosDisponiveis = eventosDisponiveis;

            uint idEventoAlvo = idEvento ?? 0;
            if (idEventoAlvo > 0)
            {
                if (!IsAuthorized(idEventoAlvo))
                    return Forbid();
            }
            else if (eventosDisponiveis.Any())
            {
                idEventoAlvo = eventosDisponiveis.First().Id;
            }

            var evento = idEventoAlvo > 0 ? _eventoService.GetEventoSimpleDto(idEventoAlvo) : null;
            var viewModel = new ModelocrachaModel
            {
                IdEvento = idEventoAlvo,
                Evento = evento,
                NomeEvento = evento?.Nome ?? (idEventoAlvo > 0 ? _eventoService.GetNomeById(idEventoAlvo) : "Selecione o Evento"),
                Texto = "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.",
                Qrcode = 1,
                NomeArquivo = "logo_congresso_nacional_vetor.svg",
                TamanhoArquivo = "Binário BLOB • 142 KB"
            };

            return View(viewModel);
        }

        // POST: ModelocrachaController/Create
        [HttpPost]
        [Route("Create")]
        [Route("Create/{idEvento?}")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModelocrachaModel modelocrachaModel)
        {
            var idEvento = modelocrachaModel.Evento?.Id ?? modelocrachaModel.IdEvento;
            if (idEvento == 0)
            {
                ModelState.AddModelError("IdEvento", "Informe qual o Evento");
            }
            else if (!IsAuthorized(idEvento))
            {
                return Forbid();
            }

            if (modelocrachaModel.Logotipo == null || modelocrachaModel.Logotipo.Length == 0)
            {
                ModelState.AddModelError("Logotipo", "Informe a logotipo");
            }

            if (ModelState.IsValid)
            {
                byte[]? logoTipoSource = null;
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
                            ModelState.AddModelError("Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
                            return View(modelocrachaModel);
                        }
                    }
                }

                modelocrachaModel.IdEvento = idEvento;
                var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel);
                modelocracha.Logotipo = logoTipoSource!;

                try
                {
                    _modelocrachaService.Create(modelocracha);
                    if (TempData != null)
                    {
                        TempData["SuccessMessage"] = "Modelo salvo com sucesso";
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar o modelo de crachá. Tente novamente.");
                    ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
                    return View(modelocrachaModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = modelocrachaModel.IdEvento });
            }

            ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
            if (idEvento > 0 && modelocrachaModel.Evento == null)
            {
                modelocrachaModel.Evento = _eventoService.GetEventoSimpleDto(idEvento);
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
            if (!IsAuthorized(modelocracha.IdEvento))
                return Forbid();

            var viewModel = _mapper.Map<ModelocrachaModel>(modelocracha);
            viewModel.Evento = _eventoService.GetEventoSimpleDto(modelocracha.IdEvento);
            if (viewModel.Evento == null)
            {
                viewModel.Evento = new EventoSimpleDTO { Id = modelocracha.IdEvento, Nome = _eventoService.GetNomeById(modelocracha.IdEvento) };
            }
            viewModel.NomeEvento = viewModel.Evento?.Nome ?? _eventoService.GetNomeById(modelocracha.IdEvento);

            if (modelocracha.Logotipo != null && modelocracha.Logotipo.Length > 0)
            {
                viewModel.LogotipoBase64 = Convert.ToBase64String(modelocracha.Logotipo);
                viewModel.NomeArquivo = "logo_institucional.png";
                var kb = Math.Round((double)modelocracha.Logotipo.Length / 1024.0, 1);
                viewModel.TamanhoArquivo = $"Binário BLOB • {kb} KB";
            }

            ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
            return View(viewModel);
        }

        // POST: ModelocrachaController/Edit/5
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, ModelocrachaModel viewModel)
        {
            viewModel.Id = id;
            var existente = _modelocrachaService.Get(id);
            if (existente == null)
                return NotFound();

            var idEventoAlvo = viewModel.Evento?.Id ?? viewModel.IdEvento;
            if (idEventoAlvo == 0)
                idEventoAlvo = existente.IdEvento;

            if (!IsAuthorized(existente.IdEvento) || !IsAuthorized(idEventoAlvo))
                return Forbid();

            if (viewModel.Logotipo == null && existente.Logotipo != null)
            {
                ModelState.Remove("Logotipo");
            }

            if (ModelState.IsValid)
            {
                byte[]? logoTipoSource = null;
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
                            ModelState.AddModelError("Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
                            return View(viewModel);
                        }
                    }
                }

                var modelocracha = _mapper.Map<Modelocracha>(viewModel);
                modelocracha.IdEvento = idEventoAlvo;
                if (logoTipoSource != null)
                {
                    modelocracha.Logotipo = logoTipoSource;
                }
                else
                {
                    modelocracha.Logotipo = existente.Logotipo;
                }

                try
                {
                    _modelocrachaService.Edit(modelocracha);
                    if (TempData != null)
                    {
                        TempData["SuccessMessage"] = "Modelo salvo com sucesso";
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao atualizar o modelo de crachá. Tente novamente.");
                    ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
                    return View(viewModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = modelocracha.IdEvento });
            }

            ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
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
            if (!IsAuthorized(modelocracha.IdEvento))
                return Forbid();
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
            var existente = _modelocrachaService.Get(id);
            if (existente == null)
                return NotFound();
            if (!IsAuthorized(existente.IdEvento) || !IsAuthorized(idEvento))
                return Forbid();
            _modelocrachaService.Delete(id);
            return RedirectToAction(nameof(Index), new { idEvento = idEvento });
        }
    }
}
