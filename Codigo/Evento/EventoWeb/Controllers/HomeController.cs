using AutoMapper;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EventoWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEventoService _eventoService;
        private readonly IMapper _mapper;

        public HomeController(ILogger<HomeController> logger, IEventoService eventoService, IMapper mapper)
        {
            _logger = logger;
            _eventoService = eventoService;
            _mapper = mapper;
        }

        // Ação Index para listar eventos
        public IActionResult Index()
        {
            var listarEventos = _eventoService.GetAll();
            var listarEventosModel = _mapper.Map<List<EventoModel>>(listarEventos);
            return View(listarEventosModel);
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
