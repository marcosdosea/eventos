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

        public string? ObterEstadoAtivo(string? ufParam = null)
        {
            if (!string.IsNullOrWhiteSpace(ufParam))
            {
                var ufTrim = ufParam.Trim();
                if (ufTrim.Equals("TODOS", StringComparison.OrdinalIgnoreCase) ||
                    ufTrim.Equals("BR", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                var ufUpper = ufTrim.ToUpper();
                try
                {
                    var estadoValido = _estadosbrasilService.Get(ufUpper);
                    if (estadoValido != null)
                    {
                        return ufUpper;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Erro ao validar UF fornecida na requisição.");
                }
                return null;
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

            if (cookieLocation != null && cookieLocation.Manual == true)
            {
                if (!string.IsNullOrWhiteSpace(cookieLocation.Estado))
                {
                    return cookieLocation.Estado.Trim().ToUpper();
                }
                return null;
            }

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
                            return pessoa.Estado.Trim().ToUpper();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Erro ao buscar Estado da pessoa autenticada.");
                    }
                }
            }

            if (cookieLocation != null && !string.IsNullOrWhiteSpace(cookieLocation.Estado))
            {
                return cookieLocation.Estado.Trim().ToUpper();
            }

            return null;
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

            var estadoAtivo = ObterEstadoAtivo(uf);
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

            ViewBag.EstadoAtivo = estadoAtivo;
            string nomeEstado = "Todo o Brasil";
            if (!string.IsNullOrEmpty(estadoAtivo))
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
            ViewBag.NomeEstadoAtivo = nomeEstado;

            return View(listarEventosModel);
        }

        [HttpGet]
        public IActionResult Buscar([FromQuery] Core.DTO.EventoFilterDTO filter, int pagina = 1)
        {
            int tamanhoPagina = 16; // Garante máximo de 4 linhas x 4 colunas (16 eventos por página)
            IEnumerable<Core.Evento> eventos = new List<Core.Evento>();

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

            var estadoAtivo = ObterEstadoAtivo(filter.Estado);
            ViewBag.EstadoAtivo = estadoAtivo;
            string nomeEstado = "Todo o Brasil";
            if (!string.IsNullOrEmpty(estadoAtivo))
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
