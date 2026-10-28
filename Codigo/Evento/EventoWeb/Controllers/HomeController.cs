using AutoMapper;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace EventoWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEventoService _eventoService;
        private readonly IMapper _mapper;
        private readonly ITipoeventoService _tipoEventoService;
        private readonly IAreaInteresseService _areaInteresseService;
        private readonly IEstadosbrasilService _estadosbrasilService;
        private readonly IPessoaService _pessoaService;

        public HomeController(ILogger<HomeController> logger, IEventoService eventoService, IMapper mapper,
            ITipoeventoService tipoEventoService, IAreaInteresseService areaInteresseService, IEstadosbrasilService estadosbrasilService,
            IPessoaService pessoaService)
        {
            _logger = logger;
            _eventoService = eventoService;
            _mapper = mapper;
            _tipoEventoService = tipoEventoService;
            _areaInteresseService = areaInteresseService;
            _estadosbrasilService = estadosbrasilService;
            _pessoaService = pessoaService;
        }

        public class UserLocationCookieModel
        {
            public string? Cidade { get; set; }
            public string? Estado { get; set; }
            public bool? Manual { get; set; }
        }

        public class LocalizacaoAtivaModel
        {
            public string? Estado { get; set; }
            public string? Cidade { get; set; }
            public bool Manual { get; set; }
            public string NomeExibicao { get; set; } = "Todo o Brasil";
        }

        public LocalizacaoAtivaModel ObterLocalizacaoAtiva(string? ufParam = null, string? cidadeParam = null)
        {
            if (!string.IsNullOrWhiteSpace(ufParam))
            {
                var ufTrim = ufParam.Trim();
                if (ufTrim.Equals("TODOS", StringComparison.OrdinalIgnoreCase) ||
                    ufTrim.Equals("BR", StringComparison.OrdinalIgnoreCase))
                {
                    return new LocalizacaoAtivaModel { Estado = null, Cidade = null, Manual = false, NomeExibicao = "Todo o Brasil" };
                }

                var ufUpper = ufTrim.ToUpper();
                try
                {
                    var estadoValido = _estadosbrasilService.Get(ufUpper);
                    if (estadoValido != null)
                    {
                        var cidadeLimpa = !string.IsNullOrWhiteSpace(cidadeParam) ? cidadeParam.Trim() : null;
                        var nomeExibicao = !string.IsNullOrEmpty(cidadeLimpa)
                            ? $"{cidadeLimpa} ({ufUpper})"
                            : $"{estadoValido.Nome} ({estadoValido.Estado})";

                        return new LocalizacaoAtivaModel
                        {
                            Estado = ufUpper,
                            Cidade = cidadeLimpa,
                            Manual = false,
                            NomeExibicao = nomeExibicao
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Erro ao validar UF fornecida na requisição.");
                }
                return new LocalizacaoAtivaModel { Estado = null, Cidade = null, Manual = false, NomeExibicao = "Todo o Brasil" };
            }

            UserLocationCookieModel? cookieLocation = null;
            if (Request?.Cookies != null && Request.Cookies.TryGetValue("UserLocation", out var cookieRaw) && !string.IsNullOrWhiteSpace(cookieRaw))
            {
                try
                {
                    var json = cookieRaw;
                    if (json.StartsWith("%"))
                    {
                        json = Uri.UnescapeDataString(json);
                    }
                    cookieLocation = System.Text.Json.JsonSerializer.Deserialize<UserLocationCookieModel>(json, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Erro ao deserializar cookie UserLocation.");
                }
            }

            // 1. Cookie manual prevalece sobre qualquer detecção ou cadastro
            if (cookieLocation != null && cookieLocation.Manual == true)
            {
                if (!string.IsNullOrWhiteSpace(cookieLocation.Estado))
                {
                    var ufUpper = cookieLocation.Estado.Trim().ToUpper();
                    try
                    {
                        var estadoValido = _estadosbrasilService.Get(ufUpper);
                        if (estadoValido != null)
                        {
                            var cidadeLimpa = !string.IsNullOrWhiteSpace(cookieLocation.Cidade) ? cookieLocation.Cidade.Trim() : null;
                            var nomeExibicao = !string.IsNullOrEmpty(cidadeLimpa)
                                ? $"{cidadeLimpa} ({ufUpper})"
                                : $"{estadoValido.Nome} ({estadoValido.Estado})";

                            return new LocalizacaoAtivaModel
                            {
                                Estado = ufUpper,
                                Cidade = cidadeLimpa,
                                Manual = true,
                                NomeExibicao = nomeExibicao
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Erro ao validar estado do cookie manual.");
                    }
                }
                return new LocalizacaoAtivaModel { Estado = null, Cidade = null, Manual = true, NomeExibicao = "Todo o Brasil" };
            }

            // 2. Usuário autenticado: fallback para o Estado cadastrado no perfil da pessoa
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var userCpf = User.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(userCpf))
                {
                    try
                    {
                        var pessoa = _pessoaService.GetByCpf(userCpf);
                        if (pessoa != null && !string.IsNullOrWhiteSpace(pessoa.Estado))
                        {
                            var ufUpper = pessoa.Estado.Trim().ToUpper();
                            var estadoValido = _estadosbrasilService.Get(ufUpper);
                            if (estadoValido != null)
                            {
                                return new LocalizacaoAtivaModel
                                {
                                    Estado = ufUpper,
                                    Cidade = null,
                                    Manual = false,
                                    NomeExibicao = $"{estadoValido.Nome} ({estadoValido.Estado})"
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Erro ao buscar Estado da pessoa autenticada.");
                    }
                }
            }

            // 3. Cookie automático (geolocalização/IP)
            if (cookieLocation != null && !string.IsNullOrWhiteSpace(cookieLocation.Estado))
            {
                var ufUpper = cookieLocation.Estado.Trim().ToUpper();
                try
                {
                    var estadoValido = _estadosbrasilService.Get(ufUpper);
                    if (estadoValido != null)
                    {
                        return new LocalizacaoAtivaModel
                        {
                            Estado = ufUpper,
                            Cidade = null,
                            Manual = false,
                            NomeExibicao = $"{estadoValido.Nome} ({estadoValido.Estado})"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Erro ao validar estado do cookie automático.");
                }
            }

            return new LocalizacaoAtivaModel { Estado = null, Cidade = null, Manual = false, NomeExibicao = "Todo o Brasil" };
        }

        public string? ObterEstadoAtivo(string? ufParam = null)
        {
            return ObterLocalizacaoAtiva(ufParam).Estado;
        }

        public IActionResult Index(string? uf = null, bool vitrine = false, bool adminRemovido = false)
        {
            
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            {
                TempData.Remove("Message");
            }
            TempData["SelecionarPerfil"] = "false";
            if (!vitrine && User?.Identity != null && User.Identity.IsAuthenticated)
            {
                if(User.IsInRole("ADMINISTRADOR") && (User.IsInRole("GESTOR") || User.IsInRole("PARTICIPANTE") || User.IsInRole("COLABORADOR") || User.IsInRole("USUARIO")))
                {
                    var perfilAtivo = HttpContext?.Session?.GetString("PerfilAtivo");
                    if (string.IsNullOrEmpty(perfilAtivo))
                    {
                       
                        TempData["SelecionarPerfil"] = "true";
                    }
                }
                else
                {
                   
                    if (User.IsInRole("GESTOR") || User.IsInRole("COLABORADOR"))
                    {
                        if (adminRemovido) return RedirectToAction("GerenciarEventoListar", "Evento", new { adminRemovido = true });

                        return RedirectToAction("GerenciarEventoListar", "Evento");
                    }

                    if (User.IsInRole("ADMINISTRADOR"))
                    {
                        return RedirectToAction("Index", "Evento");
                    }
                }
               
            }
            if (adminRemovido)
            {
                TempData["SuccessMessage"] = "Aviso: Seu cargo de administrador foi removido!";
                TempData["ToastTimeout"] = 5000;
            }

            var localizacao = ObterLocalizacaoAtiva(uf);
            var estadoAtivo = localizacao.Estado;
            var listarEventos = _eventoService.GetEventosVitrine(estadoAtivo).ToList();
            var listarEventosModel = _mapper.Map<List<EventoModel>>(listarEventos);

            foreach (var evento in listarEventosModel)
            {
                evento.Descricao = string.IsNullOrWhiteSpace(evento.Descricao) ? string.Empty : evento.Descricao;
            }


            var eventosGerenciadosIds = new HashSet<uint>();
            if (User?.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("GESTOR"))
            {
                var userCpf = User.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(userCpf))
                {
                    try
                    {
                        foreach (var evento in _eventoService.GetEventByCpf(userCpf, 2))
                        {
                            eventosGerenciadosIds.Add(evento.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Não foi possível obter os eventos gerenciados pelo usuário na vitrine.");
                    }
                }
            }
            ViewBag.EventosGerenciadosIds = eventosGerenciadosIds;

            ViewBag.EstadoAtivo = localizacao.Estado;
            ViewBag.CidadeAtiva = localizacao.Cidade;
            ViewBag.NomeEstadoAtivo = localizacao.NomeExibicao;

            return View(listarEventosModel);
        }

        [HttpGet]
        public IActionResult Buscar([FromQuery] Core.DTO.EventoFilterDTO filter, int pagina = 1)
        {
            int tamanhoPagina = 16; // Garante máximo de 4 linhas x 4 colunas (16 eventos por página)
            IEnumerable<Core.Evento> eventos = new List<Core.Evento>();

            string? estadoAtivo = null;
            string? cidadeAtiva = null;

            if (!string.IsNullOrWhiteSpace(filter.Estado))
            {
                var ufTrim = filter.Estado.Trim();
                if (!ufTrim.Equals("TODOS", StringComparison.OrdinalIgnoreCase) &&
                    !ufTrim.Equals("BR", StringComparison.OrdinalIgnoreCase))
                {
                    var ufUpper = ufTrim.ToUpper();
                    try
                    {
                        var estadoValido = _estadosbrasilService.Get(ufUpper);
                        if (estadoValido != null)
                        {
                            estadoAtivo = ufUpper;
                            filter.Estado = ufUpper;
                            if (!string.IsNullOrWhiteSpace(filter.Cidade))
                            {
                                cidadeAtiva = filter.Cidade.Trim();
                                filter.Cidade = cidadeAtiva;
                            }
                            else
                            {
                                filter.Cidade = null;
                            }
                        }
                        else
                        {
                            filter.Estado = null;
                            filter.Cidade = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Erro ao validar UF fornecida no filtro de busca.");
                        filter.Estado = null;
                        filter.Cidade = null;
                    }
                }
                else
                {
                    filter.Estado = null;
                    filter.Cidade = null;
                }
            }
            else
            {
                filter.Estado = null;
                filter.Cidade = null;
            }

            int totalRegistros;
            eventos = _eventoService.Search(filter, pagina, tamanhoPagina, out totalRegistros);

            var eventosModel = _mapper.Map<List<EventoModel>>(eventos);

            ViewBag.TiposEventos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_tipoEventoService.GetAll().OrderBy(t => t.Nome), "Id", "Nome", filter.IdTipoEvento);
            ViewBag.AreasInteresse = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_areaInteresseService.GetAll().OrderBy(a => a.Nome), "Id", "Nome", filter.IdAreaInteresse);
            ViewBag.Estados = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome", filter.Estado);
            ViewBag.FiltroAtual = filter;
            
            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / tamanhoPagina);
            ViewBag.TamanhoPagina = tamanhoPagina;
            ViewBag.TotalRegistros = totalRegistros;

            ViewBag.EstadoAtivo = estadoAtivo;
            ViewBag.CidadeAtiva = cidadeAtiva;

            string nomeEstado = "Todo o Brasil";
            if (!string.IsNullOrEmpty(estadoAtivo))
            {
                if (!string.IsNullOrEmpty(cidadeAtiva))
                {
                    nomeEstado = $"{cidadeAtiva} ({estadoAtivo})";
                }
                else
                {
                    try
                    {
                        var estadoObj = _estadosbrasilService.Get(estadoAtivo);
                        nomeEstado = estadoObj != null ? $"{estadoObj.Nome} ({estadoObj.Estado})" : estadoAtivo;
                    }
                    catch
                    {
                        nomeEstado = estadoAtivo;
                    }
                }
            }
            ViewBag.NomeEstadoAtivo = nomeEstado;

            return View(eventosModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
