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
        private const int MaxLogotipoBytes = 65535;
        private const int MaxLogotipoBase64Chars = 87384;

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
                    texto = TextoPadraoCracha,
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
            const string nomeArquivo = "LOGOTIPO.png";
            string? caminhoArquivo = null;

            if (!string.IsNullOrEmpty(_environment?.WebRootPath))
            {
                caminhoArquivo = Path.Combine(_environment.WebRootPath, "img", nomeArquivo);
            }
            else if (!string.IsNullOrEmpty(_environment?.ContentRootPath))
            {
                caminhoArquivo = Path.Combine(_environment.ContentRootPath, "wwwroot", "img", nomeArquivo);
            }
            else
            {
                var baseDir = AppContext.BaseDirectory;
                var caminhoTeste = Path.Combine(baseDir, "..", "..", "..", "..", "EventoWeb", "wwwroot", "img", nomeArquivo);
                if (System.IO.File.Exists(caminhoTeste))
                {
                    caminhoArquivo = Path.GetFullPath(caminhoTeste);
                }
            }

            if (caminhoArquivo != null && System.IO.File.Exists(caminhoArquivo))
            {
                var bytes = System.IO.File.ReadAllBytes(caminhoArquivo);
                if (bytes.Length > 0 && bytes.Length <= MaxLogotipoBytes && bytes.IsImagem())
                {
                    var kb = Math.Round((double)bytes.Length / 1024.0, 1);
                    return (bytes, nomeArquivo, $"{kb} KB");
                }
            }

            return (null, nomeArquivo, "64 KB");
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

            // 1. Validação de tamanho ANTES de decodificar (evita decodificação e estouro de memória)
            long tamanhoEstimadoBytes = (base64Limpo.Length * 3L) / 4L;
            if (base64Limpo.Length > MaxLogotipoBase64Chars || tamanhoEstimadoBytes > MaxLogotipoBytes)
            {
                erroValidacao = "O arquivo de imagem deve ter menos de 64 KB.";
                return false;
            }

            // 2. Decodificação segura
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

            // 3. Validação do tamanho real decodificado (limite da coluna BLOB de 65.535 bytes)
            if (bytes.Length > MaxLogotipoBytes)
            {
                erroValidacao = "O arquivo de imagem deve ter menos de 64 KB.";
                return false;
            }

            // 4. Validação de magic bytes (PNG, JPG, JPEG, TIF, GIF) via ImageExtensions.IsImagem
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

            var viewModel = new ModelocrachaModel
            {
                Id = 0,
                IdEvento = idEventoAlvo,
                Evento = evento ?? (idEventoAlvo > 0 ? _eventoService.GetEventoSimpleDto(idEventoAlvo) : null),
                NomeEvento = evento?.Nome ?? (idEventoAlvo > 0 ? _eventoService.GetNomeById(idEventoAlvo) : "Selecione o Evento"),
                Texto = TextoPadraoCracha,
                Qrcode = 1,
                NomeArquivo = modeloComLogotipo != null ? "logo_institucional.png" : "logo_congresso_nacional_vetor.svg",
                TamanhoArquivo = "64 KB",
                LogotipoBase64 = modeloComLogotipo?.Logotipo != null && modeloComLogotipo.Logotipo.Length > 0
                    ? Convert.ToBase64String(modeloComLogotipo.Logotipo)
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
                        ModelState.AddModelError("Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
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
                        ModelState.AddModelError("Logotipo", "O arquivo é muito grande. Deve ser menor que 64 KB.");
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
