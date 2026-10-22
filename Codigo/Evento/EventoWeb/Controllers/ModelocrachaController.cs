using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Helpers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
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
        private readonly IWebHostEnvironment? _environment;

        public ModelocrachaController(
            IModelocrachaService modelocrachaService,
            IEventoService eventoService,
            IPessoaService pessoaSevice,
            IInscricaoService inscricaoService,
            IMapper mapper,
            IWebHostEnvironment? environment = null)
        {
            _modelocrachaService = modelocrachaService;
            _eventoService = eventoService;
            _pessoaService = pessoaSevice;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
            _environment = environment;
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
                    model.NomeEvento = _eventoService.GetNomeById(m.IdEvento) ?? "Evento não encontrado";
                    model.IdPessoa = idPessoa;
                    if (m.Logotipo != null && m.Logotipo.Length > 0)
                    {
                        model.LogotipoBase64 = Convert.ToBase64String(m.Logotipo);
                        model.NomeArquivo = "logo_institucional.png";
                        var kb = Math.Round((double)m.Logotipo.Length / 1024.0, 1);
                        model.TamanhoArquivo = $"{kb} KB";
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
                        m.TamanhoArquivo = $"{kb} KB";
                    }
                    return m;
                }).ToList();
                return View(model);
            }
        }

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
                    var inscricoesFiltradas = idPessoa.HasValue
                        ? inscricoesev.Where(inscricao => inscricao.IdPapel == 4 && inscricao.IdPessoa == idPessoa)
                        : inscricoesev.Where(inscricao => inscricao.IdPapel == 4);

                    if (idPessoa.HasValue)
                    {
                        modelocrachaModel.IdPessoa = idPessoa.Value;
                    }

                    var crachas = new List<CrachaParticipanteModel>();
                    var qrCodes = new List<string>();

                    foreach (var inscricao in inscricoesFiltradas)
                    {
                        var subeventosIdsPessoa = inscricoessub
                            .Where(sub => sub.IdPessoa == inscricao.IdPessoa)
                            .Select(sub => sub.IdSubEvento)
                            .Distinct()
                            .ToList();

                        var nomeCracha = !string.IsNullOrWhiteSpace(inscricao.NomeCracha)
                            ? inscricao.NomeCracha
                            : (!string.IsNullOrWhiteSpace(inscricao.IdPessoaNavigation?.NomeCracha)
                                ? inscricao.IdPessoaNavigation.NomeCracha
                                : (inscricao.IdPessoaNavigation?.Nome ?? "Participante"));

                        var conteudoQrCode = $"[{inscricao.IdPessoa}] [{nomeCracha}] [{modelocracha.IdEvento}]";
                        if (subeventosIdsPessoa.Any())
                        {
                            conteudoQrCode += $" {string.Join(" ", subeventosIdsPessoa.Select(idSubEvento => $"[{idSubEvento}]"))}";
                        }

                        var qrCodeBytes = QrCodeGenerator.GenerateQr(conteudoQrCode);
                        var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

                        qrCodes.Add(qrCodeBase64);
                        crachas.Add(new CrachaParticipanteModel
                        {
                            IdPessoa = inscricao.IdPessoa,
                            Nome = inscricao.IdPessoaNavigation?.Nome ?? "Participante",
                            NomeCracha = nomeCracha,
                            QrCodeBase64 = qrCodeBase64
                        });
                    }

                    modelocrachaModel.QrCodes = qrCodes;
                    modelocrachaModel.Crachas = crachas;
                }
            }
            return View(modelocrachaModel);
        }

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
                tamanhoArquivo = $"{kb} KB";
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

        private (byte[]? bytes, string nomeArquivo, string tamanhoArquivo) ObterLogotipoPredefinido()
        {
            var nomesArquivos = new[] { "LOGOTIPO.png", "LOGOTIPO.svg" };
            var caminhosCandidatos = new List<string>();

            if (!string.IsNullOrEmpty(_environment?.WebRootPath))
            {
                foreach (var nome in nomesArquivos)
                    caminhosCandidatos.Add(Path.Combine(_environment.WebRootPath, "img", nome));
            }

            if (!string.IsNullOrEmpty(_environment?.ContentRootPath))
            {
                foreach (var nome in nomesArquivos)
                    caminhosCandidatos.Add(Path.Combine(_environment.ContentRootPath, "wwwroot", "img", nome));
            }

            var baseDir = AppContext.BaseDirectory;
            foreach (var nome in nomesArquivos)
            {
                caminhosCandidatos.Add(Path.Combine(baseDir, "wwwroot", "img", nome));
                caminhosCandidatos.Add(Path.Combine(baseDir, "..", "..", "..", "..", "EventoWeb", "wwwroot", "img", nome));
            }

            var currentDir = Directory.GetCurrentDirectory();
            foreach (var nome in nomesArquivos)
            {
                caminhosCandidatos.Add(Path.Combine(currentDir, "wwwroot", "img", nome));
                caminhosCandidatos.Add(Path.Combine(currentDir, "EventoWeb", "wwwroot", "img", nome));
                caminhosCandidatos.Add(Path.Combine(currentDir, "Codigo", "Evento", "EventoWeb", "wwwroot", "img", nome));
            }

            foreach (var caminho in caminhosCandidatos)
            {
                try
                {
                    var caminhoNormalizado = Path.GetFullPath(caminho);
                    if (System.IO.File.Exists(caminhoNormalizado))
                    {
                        var bytes = System.IO.File.ReadAllBytes(caminhoNormalizado);
                        if (bytes.Length > 0 && bytes.Length <= 65535)
                        {
                            var nome = Path.GetFileName(caminhoNormalizado);
                            var kb = Math.Round((double)bytes.Length / 1024.0, 1);
                            return (bytes, nome, $"{kb} KB");
                        }
                    }
                }
                catch
                {
                    // Fallback para próximo candidato
                }
            }

            return (null, "LOGOTIPO.png", "64 KB");
        }

        [HttpGet]
        [Route("ObterPredefinido")]
        [Route("ObterPredefinido/{idEvento?}")]
        public IActionResult ObterPredefinido(uint? idEvento)
        {
            var textoPadrao = "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.";
            var qrcodePadrao = 1;

            if (idEvento.HasValue && idEvento.Value > 0)
            {
                if (!IsAuthorized(idEvento.Value))
                    return Forbid();
            }

            var (logoBytes, nomeArquivo, tamanhoArquivo) = ObterLogotipoPredefinido();
            string? logotipoBase64 = logoBytes != null && logoBytes.Length > 0 ? Convert.ToBase64String(logoBytes) : null;

            return Json(new
            {
                sucesso = true,
                texto = textoPadrao,
                qrcode = qrcodePadrao,
                temLogotipo = !string.IsNullOrEmpty(logotipoBase64),
                logotipoBase64,
                nomeArquivo,
                tamanhoArquivo
            });
        }

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
            var modelosDoEvento = idEventoAlvo > 0 ? _modelocrachaService.GetByEvento(idEventoAlvo).ToList() : new List<Modelocracha>();
            var modeloComLogotipo = modelosDoEvento.FirstOrDefault(m => m.Logotipo != null && m.Logotipo.Length > 0);

            var viewModel = new ModelocrachaModel
            {
                Id = 0,
                IdEvento = idEventoAlvo,
                Evento = evento ?? (idEventoAlvo > 0 ? _eventoService.GetEventoSimpleDto(idEventoAlvo) : null),
                NomeEvento = evento?.Nome ?? (idEventoAlvo > 0 ? _eventoService.GetNomeById(idEventoAlvo) : "Selecione o Evento"),
                Texto = "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.",
                Qrcode = 1,
                NomeArquivo = modeloComLogotipo != null ? "logo_institucional.png" : "logo_congresso_nacional_vetor.svg",
                TamanhoArquivo = "64 KB",
                LogotipoBase64 = modeloComLogotipo?.Logotipo != null && modeloComLogotipo.Logotipo.Length > 0
                    ? Convert.ToBase64String(modeloComLogotipo.Logotipo)
                    : null
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route("Create")]
        [Route("Create/{idEvento?}")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ModelocrachaModel modelocrachaModel, string? btnRascunho = null, uint? idEvento = null)
        {
            var isRascunho = string.Equals(btnRascunho, "true", StringComparison.OrdinalIgnoreCase);

            var idEventoAlvo = modelocrachaModel.Evento?.Id ?? (modelocrachaModel.IdEvento > 0 ? modelocrachaModel.IdEvento : idEvento ?? 0);
            if (idEventoAlvo == 0)
            {
                ModelState.AddModelError("IdEvento", "Informe qual o Evento");
            }
            else if (!IsAuthorized(idEventoAlvo))
            {
                return Forbid();
            }

            var modelosExistentes = idEventoAlvo > 0 ? _modelocrachaService.GetByEvento(idEventoAlvo).ToList() : new List<Modelocracha>();
            var modeloComLogotipo = modelosExistentes.FirstOrDefault(m => m.Logotipo != null && m.Logotipo.Length > 0);

            var base64Logo = modelocrachaModel.LogotipoBase64;
            if (string.IsNullOrEmpty(base64Logo) && Request?.HasFormContentType == true && Request.Form.ContainsKey("LogotipoBase64"))
            {
                base64Logo = Request.Form["LogotipoBase64"].ToString();
            }

            if (isRascunho || (modelocrachaModel.Logotipo == null && (modeloComLogotipo?.Logotipo != null || !string.IsNullOrEmpty(base64Logo))))
            {
                ModelState.Remove("Logotipo");
            }
            else if (modelocrachaModel.Logotipo == null || modelocrachaModel.Logotipo.Length == 0)
            {
                if (modeloComLogotipo == null || modeloComLogotipo.Logotipo == null)
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
                else if (!string.IsNullOrEmpty(base64Logo))
                {
                    try
                    {
                        logoTipoSource = Convert.FromBase64String(base64Logo);
                    }
                    catch
                    {
                    }
                }
                else if (modeloComLogotipo?.Logotipo != null)
                {
                    logoTipoSource = modeloComLogotipo.Logotipo;
                }
                else if (isRascunho)
                {
                    logoTipoSource = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                }

                modelocrachaModel.IdEvento = idEventoAlvo;

                try
                {
                    // Regra de negócio: permite múltiplos modelos por evento, sempre criando nova entidade
                    var modelocracha = _mapper.Map<Modelocracha>(modelocrachaModel);
                    modelocracha.Id = 0;
                    modelocracha.IdEvento = idEventoAlvo;
                    modelocracha.Logotipo = logoTipoSource ?? new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                    _modelocrachaService.Create(modelocracha);

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
            if (idEventoAlvo > 0 && modelocrachaModel.Evento == null)
            {
                modelocrachaModel.Evento = _eventoService.GetEventoSimpleDto(idEventoAlvo);
            }
            return View(modelocrachaModel);
        }

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
                viewModel.TamanhoArquivo = $"{kb} KB";
            }

            ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
            return View(viewModel);
        }

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

            var base64Logo = viewModel.LogotipoBase64;
            if (string.IsNullOrEmpty(base64Logo) && Request?.HasFormContentType == true && Request.Form.ContainsKey("LogotipoBase64"))
            {
                base64Logo = Request.Form["LogotipoBase64"].ToString();
            }

            if (isRascunho || (viewModel.Logotipo == null && (modeloBaseParaLogotipo.Logotipo != null || !string.IsNullOrEmpty(base64Logo))))
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
                else if (!string.IsNullOrEmpty(base64Logo))
                {
                    try
                    {
                        logoTipoSource = Convert.FromBase64String(base64Logo);
                    }
                    catch
                    {
                    }
                }
                else if (modeloBaseParaLogotipo.Logotipo != null)
                {
                    logoTipoSource = modeloBaseParaLogotipo.Logotipo;
                }
                else if (isRascunho)
                {
                    logoTipoSource = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                }

                try
                {
                    existente.IdEvento = idEventoAlvo;
                    existente.Texto = viewModel.Texto;
                    existente.Qrcode = (sbyte)viewModel.Qrcode;
                    if (logoTipoSource != null)
                    {
                        existente.Logotipo = logoTipoSource;
                    }
                    _modelocrachaService.Edit(existente);

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
