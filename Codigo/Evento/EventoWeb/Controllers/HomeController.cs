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

        public HomeController(ILogger<HomeController> logger, IEventoService eventoService, IMapper mapper,
            ITipoeventoService tipoEventoService, IAreaInteresseService areaInteresseService, IEstadosbrasilService estadosbrasilService)
        {
            _logger = logger;
            _eventoService = eventoService;
            _mapper = mapper;
            _tipoEventoService = tipoEventoService;
            _areaInteresseService = areaInteresseService;
            _estadosbrasilService = estadosbrasilService;
        }

        public IActionResult Index(bool vitrine = false, bool adminRemovido = false)
        {
            
            if (!User.Identity.IsAuthenticated)
            {
                TempData.Remove("Message");
            }
            TempData["SelecionarPerfil"] = "false";
            if (!vitrine)
            {
                if(User.IsInRole("ADMINISTRADOR") && (User.IsInRole("GESTOR") || User.IsInRole("PARTICIPANTE") || User.IsInRole("COLABORADOR") || User.IsInRole("USUARIO")))
                {
                    var perfilAtivo = HttpContext.Session.GetString("PerfilAtivo");
                    if (string.IsNullOrEmpty(perfilAtivo))
                    {
                        ViewBag.ExibirModal = "true";
                        TempData["SelecionarPerfil"] = "true";
                    }
                }
                else
                {
                   
                    if (User.IsInRole("GESTOR"))
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
            var listarEventos = _eventoService.GetAll().ToList();
            var listarEventosModel = _mapper.Map<List<EventoModel>>(listarEventos);

            foreach (var evento in listarEventosModel)
            {
                evento.Descricao = string.IsNullOrWhiteSpace(evento.Descricao) ? string.Empty : evento.Descricao;
            }


            var eventosGerenciadosIds = new HashSet<uint>();
            if (User.Identity.IsAuthenticated && User.IsInRole("GESTOR"))
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
                        _logger.LogWarning(ex, "Não foi possível obter os eventos gerenciados pelo usuário na vitrine.");
                    }
                }
            }
            ViewBag.EventosGerenciadosIds = eventosGerenciadosIds;

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
            
            // Dados de paginação para a view
            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / tamanhoPagina);
            ViewBag.TamanhoPagina = tamanhoPagina;
            ViewBag.TotalRegistros = totalRegistros;

            return View(eventosModel);
        }

        // Outras ações do controlador
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
