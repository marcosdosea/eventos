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

            if (cookieLocation != null)
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
                                Manual = cookieLocation.Manual ?? true,
                                NomeExibicao = nomeExibicao
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Erro ao validar estado do cookie UserLocation.");
                    }
                }

                return new LocalizacaoAtivaModel { Estado = null, Cidade = null, Manual = true, NomeExibicao = "Todo o Brasil" };
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
            ViewBag.CategoriasHome = ObterCategoriasHome();

            return View(listarEventosModel);
        }

        public Dictionary<string, CategoriaHomeRouteModel> ObterCategoriasHome()
        {
            IEnumerable<Core.Tipoevento> tipos = Enumerable.Empty<Core.Tipoevento>();
            try
            {
                tipos = _tipoEventoService.GetAll() ?? Enumerable.Empty<Core.Tipoevento>();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erro ao obter tipos de evento para categorias da vitrine.");
            }

            IEnumerable<Core.Areainteresse> areas = Enumerable.Empty<Core.Areainteresse>();
            try
            {
                areas = _areaInteresseService.GetAll() ?? Enumerable.Empty<Core.Areainteresse>();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erro ao obter áreas de interesse para categorias da vitrine.");
            }

            CategoriaHomeRouteModel Resolver(string chave, string[] termosTipo, string[] termosArea, uint? defaultTipo = null, uint? defaultArea = null, string? defaultTermo = null)
            {
                foreach (var termo in termosTipo)
                {
                    var matchTipo = tipos.FirstOrDefault(t => !string.IsNullOrEmpty(t.Nome) && t.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
                    if (matchTipo != null)
                        return new CategoriaHomeRouteModel { IdTipoEvento = matchTipo.Id };
                }

                if (defaultTipo.HasValue)
                {
                    return new CategoriaHomeRouteModel { IdTipoEvento = defaultTipo.Value };
                }

                foreach (var termo in termosArea)
                {
                    var matchArea = areas.FirstOrDefault(a => !string.IsNullOrEmpty(a.Nome) && a.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
                    if (matchArea != null)
                        return new CategoriaHomeRouteModel { IdAreaInteresse = matchArea.Id };
                }

                if (defaultArea.HasValue)
                {
                    return new CategoriaHomeRouteModel { IdAreaInteresse = defaultArea.Value };
                }

                return new CategoriaHomeRouteModel { TermoBusca = defaultTermo ?? chave };
            }

            return new Dictionary<string, CategoriaHomeRouteModel>(StringComparer.OrdinalIgnoreCase)
            {
                ["Palestras"] = Resolver("Palestras", new[] { "Palestra" }, Array.Empty<string>(), defaultTipo: 1),
                ["Teatros"] = Resolver("Teatros", new[] { "Teatro" }, new[] { "Teatro" }, defaultTermo: "Teatro"),
                ["Shows"] = Resolver("Shows", new[] { "Show" }, new[] { "Show" }, defaultTipo: 6),
                ["Stand-Up"] = Resolver("Stand-Up", new[] { "Stand-Up", "Standup", "Stand Up" }, new[] { "Stand-Up", "Standup", "Stand Up" }, defaultTermo: "Stand-Up"),
                ["Culturais"] = Resolver("Culturais", new[] { "Cultur" }, new[] { "Cultur" }, defaultTermo: "Cultural"),
                ["Religiosos"] = Resolver("Religiosos", new[] { "Religio" }, new[] { "Religio" }, defaultTermo: "Religioso")
            };
        }

        [HttpGet]
        public IActionResult Buscar([FromQuery] Core.DTO.EventoFilterDTO filter, [FromQuery] string? categoria = null, int pagina = 1)
        {
            if (!string.IsNullOrWhiteSpace(categoria) &&
                !filter.IdTipoEvento.HasValue &&
                !filter.IdAreaInteresse.HasValue &&
                string.IsNullOrWhiteSpace(filter.TermoBusca))
            {
                var categoriasMapeadas = ObterCategoriasHome();
                var chave = categoria.Trim();
                if (!categoriasMapeadas.TryGetValue(chave, out var rota))
                {
                    if (chave.Equals("Palestra", StringComparison.OrdinalIgnoreCase)) rota = categoriasMapeadas["Palestras"];
                    else if (chave.Equals("Teatro", StringComparison.OrdinalIgnoreCase)) rota = categoriasMapeadas["Teatros"];
                    else if (chave.Equals("Show", StringComparison.OrdinalIgnoreCase)) rota = categoriasMapeadas["Shows"];
                    else if (chave.Equals("Standup", StringComparison.OrdinalIgnoreCase) || chave.Equals("Stand up", StringComparison.OrdinalIgnoreCase)) rota = categoriasMapeadas["Stand-Up"];
                    else if (chave.Equals("Cultural", StringComparison.OrdinalIgnoreCase)) rota = categoriasMapeadas["Culturais"];
                    else if (chave.Equals("Religioso", StringComparison.OrdinalIgnoreCase)) rota = categoriasMapeadas["Religiosos"];
                }

                if (rota != null)
                {
                    if (rota.IdTipoEvento.HasValue) filter.IdTipoEvento = rota.IdTipoEvento;
                    if (rota.IdAreaInteresse.HasValue) filter.IdAreaInteresse = rota.IdAreaInteresse;
                    if (!string.IsNullOrWhiteSpace(rota.TermoBusca)) filter.TermoBusca = rota.TermoBusca;
                }
            }
            int tamanhoPagina = 16; 
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
