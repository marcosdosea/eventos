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

        // GET: ModelocrachaController/ObterModeloPorEvento/5
        [HttpGet]
        [Route("ObterModeloPorEvento/{idEvento}")]
        public IActionResult ObterModeloPorEvento(uint idEvento)
        {
            if (idEvento == 0)
            {
                return Json(new { existe = false });
            }

            if (!IsAuthorized(idEvento))
            {
                return Forbid();
            }

            var modelo = _modelocrachaService.GetByEvento(idEvento).FirstOrDefault();
            if (modelo == null)
            {
                return Json(new
                {
                    existe = false,
                    id = 0,
                    idEvento = idEvento,
                    nomeEvento = _eventoService.GetNomeById(idEvento),
                    texto = "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.",
                    qrcode = 1,
                    temLogotipo = false,
                    logotipoBase64 = (string?)null,
                    nomeArquivo = (string?)null,
                    tamanhoArquivo = (string?)null
                });
            }

            string? logotipoBase64 = null;
            string? tamanhoArquivo = null;
            string? nomeArquivo = null;

            if (modelo.Logotipo != null && modelo.Logotipo.Length > 0)
            {
                logotipoBase64 = Convert.ToBase64String(modelo.Logotipo);
                nomeArquivo = "logo_institucional.png";
                var kb = Math.Round((double)modelo.Logotipo.Length / 1024.0, 1);
                tamanhoArquivo = $"Binário BLOB • {kb} KB";
            }

            return Json(new
            {
                existe = true,
                id = modelo.Id,
                idEvento = modelo.IdEvento,
                nomeEvento = _eventoService.GetNomeById(modelo.IdEvento),
                texto = modelo.Texto,
                qrcode = (int)modelo.Qrcode,
                temLogotipo = !string.IsNullOrEmpty(logotipoBase64),
                logotipoBase64,
                nomeArquivo,
                tamanhoArquivo
            });
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
            var modeloExistente = idEventoAlvo > 0 ? _modelocrachaService.GetByEvento(idEventoAlvo).FirstOrDefault() : null;

            ModelocrachaModel viewModel;
            if (modeloExistente != null)
            {
                viewModel = _mapper.Map<ModelocrachaModel>(modeloExistente);
                viewModel.Evento = evento ?? _eventoService.GetEventoSimpleDto(idEventoAlvo);
                viewModel.NomeEvento = viewModel.Evento?.Nome ?? _eventoService.GetNomeById(idEventoAlvo);
                if (modeloExistente.Logotipo != null && modeloExistente.Logotipo.Length > 0)
                {
                    viewModel.LogotipoBase64 = Convert.ToBase64String(modeloExistente.Logotipo);
                    viewModel.NomeArquivo = "logo_institucional.png";
                    var kb = Math.Round((double)modeloExistente.Logotipo.Length / 1024.0, 1);
                    viewModel.TamanhoArquivo = $"Binário BLOB • {kb} KB";
                }
            }
            else
            {
                viewModel = new ModelocrachaModel
                {
                    IdEvento = idEventoAlvo,
                    Evento = evento,
                    NomeEvento = evento?.Nome ?? (idEventoAlvo > 0 ? _eventoService.GetNomeById(idEventoAlvo) : "Selecione o Evento"),
                    Texto = "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.",
                    Qrcode = 1,
                    NomeArquivo = "logo_congresso_nacional_vetor.svg",
                    TamanhoArquivo = "Binário BLOB • 64 KB"
                };
            }

            return View(viewModel);
        }

        // POST: ModelocrachaController/Create
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModelocrachaModel modelocrachaModel, string? btnRascunho = null)
        {
            var isRascunho = string.Equals(btnRascunho, "true", StringComparison.OrdinalIgnoreCase);

            var idEvento = modelocrachaModel.Evento?.Id ?? modelocrachaModel.IdEvento;
            if (idEvento == 0)
            {
                ModelState.AddModelError("IdEvento", "Informe qual o Evento");
            }
            else if (!IsAuthorized(idEvento))
            {
                return Forbid();
            }

            var modeloExistente = idEvento > 0 ? _modelocrachaService.GetByEvento(idEvento).FirstOrDefault() : null;

            if (isRascunho || (modelocrachaModel.Logotipo == null && (modeloExistente?.Logotipo != null || !string.IsNullOrEmpty(modelocrachaModel.LogotipoBase64))))
            {
                ModelState.Remove("Logotipo");
            }
            else if (modelocrachaModel.Logotipo == null || modelocrachaModel.Logotipo.Length == 0)
            {
                if (modeloExistente == null || modeloExistente.Logotipo == null)
                {
                    ModelState.AddModelError("Logotipo", "Informe a logotipo");
                }
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
                else if (modeloExistente?.Logotipo != null)
                {
                    logoTipoSource = modeloExistente.Logotipo;
                }
                else if (!string.IsNullOrEmpty(modelocrachaModel.LogotipoBase64))
                {
                    try
                    {
                        logoTipoSource = Convert.FromBase64String(modelocrachaModel.LogotipoBase64);
                    }
                    catch
                    {
                    }
                }
                else if (isRascunho)
                {
                    logoTipoSource = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                }

                modelocrachaModel.IdEvento = idEvento;

                try
                {
                    if (modeloExistente != null)
                    {
                        modeloExistente.Texto = modelocrachaModel.Texto;
                        modeloExistente.Qrcode = (sbyte)modelocrachaModel.Qrcode;
                        if (logoTipoSource != null)
                        {
                            modeloExistente.Logotipo = logoTipoSource;
                        }
                        _modelocrachaService.Edit(modeloExistente);
                    }
                    else
                    {
                        var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel);
                        modelocracha.Logotipo = logoTipoSource ?? new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                        _modelocrachaService.Create(modelocracha);
                    }

                    if (TempData != null)
                    {
                        TempData["SuccessMessage"] = isRascunho ? "Rascunho salvo com sucesso" : "Modelo salvo com sucesso";
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
        public ActionResult Edit(uint id, ModelocrachaModel viewModel, string? btnRascunho = null)
        {
            var isRascunho = string.Equals(btnRascunho, "true", StringComparison.OrdinalIgnoreCase);

            viewModel.Id = id;
            var existente = _modelocrachaService.Get(id);
            if (existente == null)
                return NotFound();

            var idEventoAlvo = viewModel.Evento?.Id ?? viewModel.IdEvento;
            if (idEventoAlvo == 0)
                idEventoAlvo = existente.IdEvento;

            if (!IsAuthorized(existente.IdEvento) || !IsAuthorized(idEventoAlvo))
                return Forbid();

            var modeloEventoAlvo = idEventoAlvo != existente.IdEvento
                ? _modelocrachaService.GetByEvento(idEventoAlvo).FirstOrDefault()
                : existente;

            var modeloBaseParaLogotipo = modeloEventoAlvo ?? existente;

            if (isRascunho || (viewModel.Logotipo == null && (modeloBaseParaLogotipo.Logotipo != null || !string.IsNullOrEmpty(viewModel.LogotipoBase64))))
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
                else if (modeloBaseParaLogotipo.Logotipo != null)
                {
                    logoTipoSource = modeloBaseParaLogotipo.Logotipo;
                }
                else if (!string.IsNullOrEmpty(viewModel.LogotipoBase64))
                {
                    try
                    {
                        logoTipoSource = Convert.FromBase64String(viewModel.LogotipoBase64);
                    }
                    catch
                    {
                    }
                }
                else if (isRascunho)
                {
                    logoTipoSource = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                }

                try
                {
                    if (modeloEventoAlvo != null)
                    {
                        modeloEventoAlvo.Texto = viewModel.Texto;
                        modeloEventoAlvo.Qrcode = (sbyte)viewModel.Qrcode;
                        if (logoTipoSource != null)
                        {
                            modeloEventoAlvo.Logotipo = logoTipoSource;
                        }
                        _modelocrachaService.Edit(modeloEventoAlvo);
                    }
                    else
                    {
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
                        _modelocrachaService.Edit(modelocracha);
                    }

                    if (TempData != null)
                    {
                        TempData["SuccessMessage"] = isRascunho ? "Rascunho salvo com sucesso" : "Modelo salvo com sucesso";
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao atualizar o modelo de crachá. Tente novamente.");
                    ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
                    return View(viewModel);
                }

                return RedirectToAction(nameof(Index), new { idEvento = idEventoAlvo });
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
