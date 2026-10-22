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
        private const string TextoPadraoCracha = "Acesso pessoal e intransferível. Obrigatório porte visível em todas as atividades do congresso e catracas credenciadas.";
        private const int MaxLogotipoBytes = 64 * 1024;
        private const int MaxLogotipoBase64Chars = ((MaxLogotipoBytes + 2) / 3) * 4;

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
                        model.NomeArquivo = ObterNomeArquivoLogo(m.Logotipo);
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
                        m.NomeArquivo = ObterNomeArquivoLogo(x.Logotipo);
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
                var (logoPadrao, nomePadrao, tamanhoPadrao) = ObterLogotipoPredefinido();
                var logoPadraoB64 = logoPadrao != null && logoPadrao.Length > 0 ? Convert.ToBase64String(logoPadrao) : null;

                return Json(new
                {
                    existe = false,
                    id = 0,
                    idEvento = idEvento,
                    nomeEvento = _eventoService.GetNomeById(idEvento),
                    texto = TextoPadraoCracha,
                    qrcode = 1,
                    temLogotipo = !string.IsNullOrEmpty(logoPadraoB64),
                    logotipoBase64 = logoPadraoB64,
                    nomeArquivo = nomePadrao,
                    tamanhoArquivo = tamanhoPadrao
                });
            }

            string? logotipoBase64 = null;
            string? tamanhoArquivo = null;
            string? nomeArquivo = null;

            if (modelo.Logotipo != null && modelo.Logotipo.Length > 0)
            {
                logotipoBase64 = Convert.ToBase64String(modelo.Logotipo);
                nomeArquivo = ObterNomeArquivoLogo(modelo.Logotipo);
                var kb = Math.Round((double)modelo.Logotipo.Length / 1024.0, 1);
                tamanhoArquivo = $"{kb} KB";
            }
            else
            {
                var (logoPadrao, nomePadrao, tamanhoPadrao) = ObterLogotipoPredefinido();
                if (logoPadrao != null && logoPadrao.Length > 0)
                {
                    logotipoBase64 = Convert.ToBase64String(logoPadrao);
                    nomeArquivo = nomePadrao;
                    tamanhoArquivo = tamanhoPadrao;
                }
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
            var nomesCandidatos = new[] { "LOGOTIPO.png", "LOGOTIPO.jpg", "LOGOTIPO.jpeg", "LOGOTIPO.webp" };

            string? pastaImg = null;
            if (!string.IsNullOrEmpty(_environment?.WebRootPath))
            {
                pastaImg = Path.Combine(_environment.WebRootPath, "img");
            }
            else if (!string.IsNullOrEmpty(_environment?.ContentRootPath))
            {
                pastaImg = Path.Combine(_environment.ContentRootPath, "wwwroot", "img");
            }
            else
            {
                var baseDir = AppContext.BaseDirectory;
                var caminhoTeste = Path.Combine(baseDir, "..", "..", "..", "..", "EventoWeb", "wwwroot", "img");
                if (Directory.Exists(caminhoTeste))
                {
                    pastaImg = Path.GetFullPath(caminhoTeste);
                }
            }

            if (pastaImg != null && Directory.Exists(pastaImg))
            {
                foreach (var nome in nomesCandidatos)
                {
                    var caminhoCompleto = Path.Combine(pastaImg, nome);
                    if (System.IO.File.Exists(caminhoCompleto))
                    {
                        var bytes = System.IO.File.ReadAllBytes(caminhoCompleto);
                        if (bytes.Length > 0 && bytes.Length <= MaxLogotipoBytes && bytes.IsImagem())
                        {
                            var kb = Math.Round((double)bytes.Length / 1024.0, 1);
                            return (bytes, nome, $"{kb} KB");
                        }
                    }
                }
            }

            return (null, string.Empty, string.Empty);
        }

        private static string ObterNomeArquivoLogo(byte[] logo)
        {
            if (logo.Length >= 3 && logo[0] == 0xFF && logo[1] == 0xD8 && logo[2] == 0xFF)
                return "logo_institucional.jpg";

            if (logo.Length >= 12 &&
                logo[0] == 0x52 && logo[1] == 0x49 && logo[2] == 0x46 && logo[3] == 0x46 &&
                logo[8] == 0x57 && logo[9] == 0x45 && logo[10] == 0x42 && logo[11] == 0x50)
                return "logo_institucional.webp";

            if (logo.Length >= 4 &&
                ((logo[0] == 0x49 && logo[1] == 0x49 && logo[2] == 0x2A && logo[3] == 0x00) ||
                 (logo[0] == 0x4D && logo[1] == 0x4D && logo[2] == 0x00 && logo[3] == 0x2A)))
                return "logo_institucional.tif";

            if (logo.Length >= 3 && logo[0] == 0x47 && logo[1] == 0x49 && logo[2] == 0x46)
                return "logo_institucional.gif";

            return "logo_institucional.png";
        }

        private static bool ValidarLogotipoBase64(string? base64Logo, out byte[]? bytesValidos, out string? erroValidacao)
        {
            bytesValidos = null;
            erroValidacao = null;

            if (string.IsNullOrWhiteSpace(base64Logo))
                return true;

            var base64Limpo = base64Logo.Trim();
            var indexVirgula = base64Limpo.IndexOf(',');
            if (indexVirgula >= 0 && base64Limpo.Substring(0, indexVirgula).Contains("base64", StringComparison.OrdinalIgnoreCase))
            {
                base64Limpo = base64Limpo.Substring(indexVirgula + 1).Trim();
            }

            if (base64Limpo.Length > MaxLogotipoBase64Chars)
            {
                erroValidacao = "O arquivo de imagem deve ter no máximo 64 KB.";
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64Limpo);
            }
            catch
            {
                erroValidacao = "Formato de Imagem Inválido";
                return false;
            }

            if (bytes.Length > MaxLogotipoBytes)
            {
                erroValidacao = "O arquivo de imagem deve ter no máximo 64 KB.";
                return false;
            }

            if (!bytes.IsImagem())
            {
                erroValidacao = "Formato de Imagem Inválido";
                return false;
            }

            bytesValidos = bytes;
            return true;
        }

        [HttpGet("ObterPredefinido/{idEvento?}")]
        public IActionResult ObterPredefinido(uint? idEvento)
        {
            if (!idEvento.HasValue || idEvento.Value == 0)
            {
                if (!User.IsInRole("ADMINISTRADOR"))
                    return Forbid();
            }
            else
            {
                if (!IsAuthorized(idEvento.Value))
                    return Forbid();
            }

            var (logoBytes, nomeArquivo, tamanhoArquivo) = ObterLogotipoPredefinido();
            string? logotipoBase64 = logoBytes != null && logoBytes.Length > 0 ? Convert.ToBase64String(logoBytes) : null;

            return Json(new
            {
                sucesso = true,
                texto = TextoPadraoCracha,
                qrcode = 1,
                temLogotipo = !string.IsNullOrEmpty(logotipoBase64),
                logotipoBase64,
                nomeArquivo,
                tamanhoArquivo
            });
        }

        [HttpGet("Create/{idEvento?}")]
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

            byte[]? logoBytes = modeloComLogotipo?.Logotipo;
            string? nomeArquivo = null;
            string? tamanhoArquivo = null;

            if (logoBytes != null && logoBytes.Length > 0)
            {
                nomeArquivo = ObterNomeArquivoLogo(logoBytes);
                var kb = Math.Round((double)logoBytes.Length / 1024.0, 1);
                tamanhoArquivo = $"{kb} KB";
            }
            else
            {
                var predefinido = ObterLogotipoPredefinido();
                if (predefinido.bytes != null && predefinido.bytes.Length > 0)
                {
                    logoBytes = predefinido.bytes;
                    nomeArquivo = predefinido.nomeArquivo;
                    tamanhoArquivo = predefinido.tamanhoArquivo;
                }
            }

            var viewModel = new ModelocrachaModel
            {
                Id = 0,
                IdEvento = idEventoAlvo,
                Evento = evento ?? (idEventoAlvo > 0 ? _eventoService.GetEventoSimpleDto(idEventoAlvo) : null),
                NomeEvento = evento?.Nome ?? (idEventoAlvo > 0 ? _eventoService.GetNomeById(idEventoAlvo) : "Selecione o Evento"),
                Texto = TextoPadraoCracha,
                Qrcode = 1,
                NomeArquivo = nomeArquivo,
                TamanhoArquivo = tamanhoArquivo,
                LogotipoBase64 = logoBytes != null && logoBytes.Length > 0
                    ? Convert.ToBase64String(logoBytes)
                    : null
            };

            return View(viewModel);
        }

        [HttpPost("Create/{idEvento?}")]
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
            byte[]? logoTipoSource = null;

            if (modelocrachaModel.Logotipo != null && modelocrachaModel.Logotipo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    modelocrachaModel.Logotipo.CopyTo(memoryStream);

                    if (memoryStream.Length <= MaxLogotipoBytes)
                    {
                        logoTipoSource = memoryStream.ToArray();
                    }
                    else
                    {
                        ModelState.AddModelError("Logotipo", "O arquivo é muito grande. Deve ter no máximo 64 KB.");
                        ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
                        return View(modelocrachaModel);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(base64Logo))
            {
                if (ValidarLogotipoBase64(base64Logo, out var logoValido, out var erroValidacao))
                {
                    logoTipoSource = logoValido;
                    ModelState.Remove("Logotipo");
                }
                else
                {
                    ModelState.AddModelError("Logotipo", erroValidacao!);
                }
            }
            else if (modeloComLogotipo?.Logotipo != null)
            {
                logoTipoSource = modeloComLogotipo.Logotipo;
                ModelState.Remove("Logotipo");
            }
            else if (isRascunho)
            {
                logoTipoSource = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                ModelState.Remove("Logotipo");
            }
            else
            {
                ModelState.AddModelError("Logotipo", "Informe a logotipo");
            }

            if (ModelState.IsValid)
            {
                modelocrachaModel.IdEvento = idEventoAlvo;

                try
                {
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
                viewModel.NomeArquivo = ObterNomeArquivoLogo(modelocracha.Logotipo);
                var kb = Math.Round((double)modelocracha.Logotipo.Length / 1024.0, 1);
                viewModel.TamanhoArquivo = $"{kb} KB";
            }

            ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
            return View(viewModel);
        }

        [HttpPost("Edit/{id}")]
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
            byte[]? logoTipoSource = null;

            if (viewModel.Logotipo != null && viewModel.Logotipo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    viewModel.Logotipo.CopyTo(memoryStream);

                    if (memoryStream.Length <= MaxLogotipoBytes)
                    {
                        logoTipoSource = memoryStream.ToArray();
                    }
                    else
                    {
                        ModelState.AddModelError("Logotipo", "O arquivo é muito grande. Deve ter no máximo 64 KB.");
                        ViewBag.EventosDisponiveis = ObterEventosDoUsuario();
                        return View(viewModel);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(base64Logo))
            {
                if (ValidarLogotipoBase64(base64Logo, out var logoValido, out var erroValidacao))
                {
                    logoTipoSource = logoValido;
                    ModelState.Remove("Logotipo");
                }
                else
                {
                    ModelState.AddModelError("Logotipo", erroValidacao!);
                }
            }
            else if (modeloBaseParaLogotipo.Logotipo != null)
            {
                logoTipoSource = modeloBaseParaLogotipo.Logotipo;
                ModelState.Remove("Logotipo");
            }
            else if (isRascunho)
            {
                logoTipoSource = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                ModelState.Remove("Logotipo");
            }

            if (ModelState.IsValid)
            {
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
