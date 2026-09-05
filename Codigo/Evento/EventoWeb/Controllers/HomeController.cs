using AutoMapper;
using Core.Service;
using EventoWeb.Models;
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

        public IActionResult Index(bool vitrine = false)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData.Remove("Message");
            }

            if (!vitrine)
            {
                if (User.IsInRole("GESTOR"))
                {
                    return RedirectToAction("GerenciarEventoListar", "Evento");
                }

                if(User.IsInRole("ADMINISTRADOR"))
                {
                    return RedirectToAction("Index", "Evento");
                }
            }
            var listarEventos = _eventoService.GetAll().ToList();
            var listarEventosModel = _mapper.Map<List<EventoModel>>(listarEventos);

            foreach (var evento in listarEventosModel)
            {
                evento.Descricao = string.IsNullOrWhiteSpace(evento.Descricao) ? string.Empty : evento.Descricao;
            }

            return View(listarEventosModel);
        }

        [HttpGet]
        public IActionResult Buscar([FromQuery] Core.DTO.EventoFilterDTO filter)
        {
            IEnumerable<Core.Evento> eventos = new List<Core.Evento>();

            bool temFiltro = !string.IsNullOrWhiteSpace(filter.TermoBusca) ||
                             filter.IdAreaInteresse.HasValue ||
                             filter.IdTipoEvento.HasValue ||
                             filter.Data.HasValue ||
                             !string.IsNullOrWhiteSpace(filter.Estado) ||
                             !string.IsNullOrWhiteSpace(filter.Cidade);

            if (temFiltro)
            {
                eventos = _eventoService.Search(filter);
            }

            var eventosModel = _mapper.Map<List<EventoModel>>(eventos);

            ViewBag.TiposEventos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_tipoEventoService.GetAll().OrderBy(t => t.Nome), "Id", "Nome", filter.IdTipoEvento);
            ViewBag.AreasInteresse = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_areaInteresseService.GetAll().OrderBy(a => a.Nome), "Id", "Nome", filter.IdAreaInteresse);
            ViewBag.Estados = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome", filter.Estado);
            ViewBag.FiltroAtual = filter;

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
