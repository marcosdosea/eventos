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
    [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
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

        private bool IsAuthorizedParaEmissao(uint idEvento)
        {
            if (User == null) return false;
            if (User.IsInRole("ADMINISTRADOR")) return true;
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            return _inscricaoService.GetGestorInEvent(username, idEvento) != null ||
                   _inscricaoService.GetColaboradorInEvent(username, idEvento) != null;
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
                        if (User.IsInRole("GESTOR"))
                        {
                            var eventosGestor = _eventoService.GetEventByCpf(cpf, 2);
                            if (eventosGestor != null)
                            {
                                eventos.AddRange(eventosGestor.Select(e => new EventoSimpleDTO { Id = e.Id, Nome = e.Nome }));
                            }
                        }
                        if (User.IsInRole("COLABORADOR"))
                        {
                            var eventosColab = _eventoService.GetEventByCpf(cpf, 3);
                            if (eventosColab != null)
                            {
                                foreach (var ev in eventosColab)
                                {
                                    if (!eventos.Any(e => e.Id == ev.Id))
                                    {
                                        eventos.Add(new EventoSimpleDTO { Id = ev.Id, Nome = ev.Nome });
                                    }
                                }
                            }
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

        private static string ObterMimeType(string nomeArquivo, string base64)
        {
            if (base64.StartsWith("/9j/", StringComparison.Ordinal)) return "image/jpeg";
            if (base64.StartsWith("UklGR", StringComparison.Ordinal)) return "image/webp";
            if (base64.StartsWith("iVBOR", StringComparison.Ordinal)) return "image/png";
            if (base64.StartsWith("R0lGOD", StringComparison.Ordinal)) return "image/gif";
            if (base64.StartsWith("SUkq", StringComparison.Ordinal) || base64.StartsWith("TU0A", StringComparison.Ordinal)) return "image/tiff";

            if (!string.IsNullOrEmpty(nomeArquivo))
            {
                var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
                return ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    ".svg" => "image/svg+xml",
                    ".tif" or ".tiff" => "image/tiff",
                    ".gif" => "image/gif",
                    _ => "image/png"
                };
            }
            return "image/png";
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

        [HttpGet]
        [Route("EmitirCracha/{idEvento?}")]
        [Route("Emitir/{idEvento?}")]
        public ActionResult EmitirCracha(uint? idEvento, uint? idModelo, uint? idPessoa)
        {
            var eventosDisponiveis = ObterEventosDoUsuario();
            ViewBag.EventosDisponiveis = eventosDisponiveis;

            uint idEventoAlvo = idEvento ?? 0;
            if (idEventoAlvo > 0)
            {
                if (!IsAuthorizedParaEmissao(idEventoAlvo))
                    return Forbid();
            }
            else if (eventosDisponiveis.Any())
            {
                idEventoAlvo = eventosDisponiveis.First().Id;
            }

            var eventoDto = idEventoAlvo > 0 ? _eventoService.GetEventoSimpleDto(idEventoAlvo) : null;
            var nomeEvento = eventoDto?.Nome ?? (idEventoAlvo > 0 ? _eventoService.GetNomeById(idEventoAlvo) : "Evento Vinculado") ?? "Evento Vinculado";

            var modelosDoEvento = idEventoAlvo > 0
                ? _modelocrachaService.GetByEvento(idEventoAlvo).ToList()
                : new List<Modelocracha>();

            var predefinido = ObterLogotipoPredefinido();
            string? logoPadraoDataUrl = null;
            if (predefinido.bytes != null && predefinido.bytes.Length > 0)
            {
                var nomePadrao = ObterNomeArquivoLogo(predefinido.bytes);
                var mimePadrao = ObterMimeType(nomePadrao, Convert.ToBase64String(predefinido.bytes));
                logoPadraoDataUrl = $"data:{mimePadrao};base64,{Convert.ToBase64String(predefinido.bytes)}";
            }

            string? ObterDataUrlLogo(byte[]? bytes)
            {
                if (bytes != null && bytes.Length > 0)
                {
                    var nome = ObterNomeArquivoLogo(bytes);
                    var mime = ObterMimeType(nome, Convert.ToBase64String(bytes));
                    return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
                return logoPadraoDataUrl;
            }

            var modelosDTO = modelosDoEvento.Select(m => new ModelocrachaItemDTO
            {
                Id = m.Id,
                Nome = $"Gabarito #{m.Id} - 90×130mm",
                Texto = m.Texto,
                TemLogotipo = m.Logotipo != null && m.Logotipo.Length > 0,
                LogotipoDataUrl = ObterDataUrlLogo(m.Logotipo),
                Qrcode = m.Qrcode
            }).ToList();

            if (!modelosDTO.Any())
            {
                modelosDTO.Add(new ModelocrachaItemDTO
                {
                    Id = 0,
                    Nome = "Modelo Padrão Vertical 9×13cm",
                    Texto = TextoPadraoCracha,
                    TemLogotipo = true,
                    LogotipoDataUrl = logoPadraoDataUrl,
                    Qrcode = 1
                });
            }

            Modelocracha? modeloSelecionado = null;
            if (idModelo.HasValue && idModelo.Value > 0)
            {
                modeloSelecionado = modelosDoEvento.FirstOrDefault(m => m.Id == idModelo.Value);
            }
            modeloSelecionado ??= modelosDoEvento.FirstOrDefault();

            string? logoDataUrl = ObterDataUrlLogo(modeloSelecionado?.Logotipo);


            var textoModelo = modeloSelecionado?.Texto ?? TextoPadraoCracha;
            var qrcodeHabilitado = modeloSelecionado?.Qrcode != 0;

            var participantes = new List<ParticipanteCrachaItemModel>();

            if (idEventoAlvo > 0)
            {
                var inscricoes = _inscricaoService.GetByEvento(idEventoAlvo);
                var subeventos = _inscricaoService.GetSubByEvento(idEventoAlvo);

                if (inscricoes != null && inscricoes.Any())
                {
                    var inscricoesOrdenadas = inscricoes.OrderBy(i => i.IdPessoaNavigation?.Nome ?? "").ToList();

                    foreach (var ins in inscricoesOrdenadas)
                    {
                        var pessoa = ins.IdPessoaNavigation;
                        var nomeCompleto = pessoa?.Nome ?? "Participante";
                        var nomeCracha = !string.IsNullOrWhiteSpace(ins.NomeCracha)
                            ? ins.NomeCracha
                            : (!string.IsNullOrWhiteSpace(pessoa?.NomeCracha)
                                ? pessoa.NomeCracha
                                : nomeCompleto);

                        var cpfPuro = pessoa?.Cpf ?? string.Empty;
                        var cpfMascarado = MascararCpfParaExibicao(cpfPuro);

                        var subeventosIdsPessoa = subeventos?
                            .Where(sub => sub.IdPessoa == ins.IdPessoa)
                            .Select(sub => sub.IdSubEvento)
                            .Distinct()
                            .ToList() ?? new List<uint>();

                        var conteudoQrCode = $"[{ins.IdPessoa}] [{nomeCracha}] [{idEventoAlvo}] [{ins.Id}]";
                        if (subeventosIdsPessoa.Any())
                        {
                            conteudoQrCode += $" {string.Join(" ", subeventosIdsPessoa.Select(idSub => $"[{idSub}]"))}";
                        }

                        string qrCodeBase64 = string.Empty;
                        try
                        {
                            var qrBytes = QrCodeGenerator.GenerateQr(conteudoQrCode);
                            qrCodeBase64 = Convert.ToBase64String(qrBytes);
                        }
                        catch
                        {
                        }

                        if (string.IsNullOrEmpty(qrCodeBase64))
                        {
                            qrCodeBase64 = GerarQrCodeSeguro($"[{ins.IdPessoa}] [{idEventoAlvo}] [{ins.Id}]");
                        }

                        string? fotoDataUrl = null;
                        bool temFoto = pessoa?.Foto != null && pessoa.Foto.Length > 0;
                        if (temFoto)
                        {
                            fotoDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(pessoa!.Foto)}";
                        }

                        bool inscricaoAtiva = ins.Status == "A" || ins.Status == "1" || string.IsNullOrEmpty(ins.Status);
                        bool statusApto = inscricaoAtiva && temFoto;
                        string statusDescricao = statusApto ? "Apto" : (!temFoto ? "Pendente Foto" : "Inscrição Pendente");

                        participantes.Add(new ParticipanteCrachaItemModel
                        {
                            IdInscricao = ins.Id,
                            IdPessoa = ins.IdPessoa,
                            NomeCompleto = nomeCompleto,
                            NomeCracha = nomeCracha,
                            OrganizacaoOuDepto = ObterOrganizacaoOuDepto(ins),
                            CpfPuro = cpfPuro,
                            CpfMascarado = cpfMascarado,
                            Papel = ObterDescricaoPapel(ins.IdPapel, ins.IdPapelNavigation?.Nome),
                            TipoInscricao = ins.IdTipoInscricaoNavigation?.Nome ?? "Geral",
                            CodigoCredencial = $"#{ins.Id:D5}",
                            CodigoPresenca = $"BL - EVT - {DateTime.Now.Year} - {ins.Id:D5}",
                            StatusApto = statusApto,
                            StatusDescricao = statusDescricao,
                            FotoDataUrl = fotoDataUrl,
                            QrCodeBase64 = qrCodeBase64,
                            Selecionado = true
                        });
                    }
                }
            }

            if (!participantes.Any())
            {
                participantes = ObterParticipantesDemonstracao(idEventoAlvo);
            }

            if (idPessoa.HasValue)
            {
                var pAlvo = participantes.FirstOrDefault(p => p.IdPessoa == idPessoa.Value);
                if (pAlvo != null)
                {
                    participantes.Remove(pAlvo);
                    participantes.Insert(0, pAlvo);
                }
            }

            var model = new EmitirCrachaModel
            {
                IdEvento = idEventoAlvo,
                NomeEvento = nomeEvento,
                SubtituloEvento = null,
                IdModelo = modeloSelecionado?.Id,
                NomeModelo = modeloSelecionado != null ? $"Gabarito #{modeloSelecionado.Id} - 90×130mm" : "Modelo Padrão Vertical 9×13cm",
                TextoModelo = textoModelo,
                LogotipoDataUrl = logoDataUrl,
                QrcodeHabilitado = qrcodeHabilitado,
                EventosDisponiveis = eventosDisponiveis,
                ModelosDisponiveis = modelosDTO,
                Participantes = participantes
            };

            return View(model);
        }

        private static string MascararCpfParaExibicao(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return "***.***.***-**";
            var limpo = Util.Methods.RemoveNaoNumericos(cpf);
            if (limpo.Length == 11)
            {
                return $"{limpo.Substring(0, 3)}.***.***-{limpo.Substring(9, 2)}";
            }
            return "***.***.***-**";
        }

        private static string ObterDescricaoPapel(int idPapel, string? nomePapel)
        {
            if (!string.IsNullOrWhiteSpace(nomePapel))
                return nomePapel.ToUpperInvariant();
            return idPapel switch
            {
                1 => "ADMINISTRADOR",
                2 => "GESTOR",
                3 => "COLABORADOR",
                4 => "PARTICIPANTE",
                _ => "PARTICIPANTE"
            };
        }

        private static string ObterOrganizacaoOuDepto(Inscricaopessoaevento inscricao)
        {
            var p = inscricao.IdPessoaNavigation;
            if (p != null)
            {
                if (!string.IsNullOrWhiteSpace(p.Cidade) && !string.IsNullOrWhiteSpace(p.Estado))
                    return $"{p.Cidade} - {p.Estado}";
                if (!string.IsNullOrWhiteSpace(p.Email))
                    return p.Email;
            }
            return "Participante Homologado";
        }

        private static List<ParticipanteCrachaItemModel> ObterParticipantesDemonstracao(uint idEvento)
        {
            var ano = DateTime.Now.Year;
            var demo = new List<ParticipanteCrachaItemModel>
            {
                new ParticipanteCrachaItemModel
                {
                    IdInscricao = 142,
                    IdPessoa = 1,
                    NomeCompleto = "Cassiano Menezes",
                    NomeCracha = "Cassiano Menezes",
                    OrganizacaoOuDepto = "UFS - Depto. Sistemas",
                    CpfPuro = "04299988818",
                    CpfMascarado = "042.***.***-18",
                    Papel = "PALESTRANTE",
                    TipoInscricao = "Convidado Especial",
                    CodigoCredencial = "#00142",
                    CodigoPresenca = $"BL - EVT - {ano} - 00142",
                    StatusApto = true,
                    StatusDescricao = "Apto",
                    QrCodeBase64 = GerarQrCodeSeguro($"[1] [Cassiano Menezes] [{idEvento}] [#00142]"),
                    Selecionado = true
                },
                new ParticipanteCrachaItemModel
                {
                    IdInscricao = 143,
                    IdPessoa = 2,
                    NomeCompleto = "Carlos E. M. Santos",
                    NomeCracha = "Carlos E. M. Santos",
                    OrganizacaoOuDepto = "Coordenação Geral",
                    CpfPuro = "11988877745",
                    CpfMascarado = "119.***.***-45",
                    Papel = "GESTOR",
                    TipoInscricao = "Organizador",
                    CodigoCredencial = "#00143",
                    CodigoPresenca = $"BL - EVT - {ano} - 00143",
                    StatusApto = true,
                    StatusDescricao = "Apto",
                    QrCodeBase64 = GerarQrCodeSeguro($"[2] [Carlos E. M. Santos] [{idEvento}] [#00143]"),
                    Selecionado = true
                },
                new ParticipanteCrachaItemModel
                {
                    IdInscricao = 144,
                    IdPessoa = 3,
                    NomeCompleto = "Mariana Silveira Rocha",
                    NomeCracha = "Mariana Silveira Rocha",
                    OrganizacaoOuDepto = "USP - Congressista",
                    CpfPuro = "83277766601",
                    CpfMascarado = "832.***.***-01",
                    Papel = "PARTICIPANTE",
                    TipoInscricao = "Estudante",
                    CodigoCredencial = "#00144",
                    CodigoPresenca = $"BL - EVT - {ano} - 00144",
                    StatusApto = true,
                    StatusDescricao = "Apto",
                    QrCodeBase64 = GerarQrCodeSeguro($"[3] [Mariana Silveira Rocha] [{idEvento}] [#00144]"),
                    Selecionado = true
                },
                new ParticipanteCrachaItemModel
                {
                    IdInscricao = 145,
                    IdPessoa = 4,
                    NomeCompleto = "Rafael Lima Mendes",
                    NomeCracha = "Rafael Lima Mendes",
                    OrganizacaoOuDepto = "Equipe Técnica / TI",
                    CpfPuro = "55466655590",
                    CpfMascarado = "554.***.***-90",
                    Papel = "COLABORADOR",
                    TipoInscricao = "Equipe de Apoio",
                    CodigoCredencial = "#00145",
                    CodigoPresenca = $"BL - EVT - {ano} - 00145",
                    StatusApto = true,
                    StatusDescricao = "Apto",
                    QrCodeBase64 = GerarQrCodeSeguro($"[4] [Rafael Lima Mendes] [{idEvento}] [#00145]"),
                    Selecionado = true
                },
                new ParticipanteCrachaItemModel
                {
                    IdInscricao = 146,
                    IdPessoa = 5,
                    NomeCompleto = "Júlia Prado Fontes",
                    NomeCracha = "Júlia Prado Fontes",
                    OrganizacaoOuDepto = "Revista Inovação & Foco",
                    CpfPuro = "90155544422",
                    CpfMascarado = "901.***.***-22",
                    Papel = "PARTICIPANTE",
                    TipoInscricao = "Imprensa",
                    CodigoCredencial = "#00146",
                    CodigoPresenca = $"BL - EVT - {ano} - 00146",
                    StatusApto = false,
                    StatusDescricao = "Pendente Foto",
                    QrCodeBase64 = GerarQrCodeSeguro($"[5] [Júlia Prado Fontes] [{idEvento}] [#00146]"),
                    Selecionado = false
                }
            };
            return demo;
        }

        private static string GerarQrCodeSeguro(string conteudo)
        {
            try
            {
                var bytes = QrCodeGenerator.GenerateQr(conteudo);
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
