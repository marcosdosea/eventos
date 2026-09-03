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
        private readonly ITipoeventoService _tipoEventoService;

        public HomeController(ILogger<HomeController> logger, IEventoService eventoService, IMapper mapper, ITipoeventoService tipoEventoService)
        {
            _logger = logger;
            _eventoService = eventoService;
            _mapper = mapper;
            _tipoEventoService = tipoEventoService;
        }

        // Ação Index para listar eventos
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData.Remove("Message");
            }

            if (User.IsInRole("GESTOR"))
            {
                return RedirectToAction("GerenciarEventoListar", "Evento");
            }

            if(User.IsInRole("ADMINISTRADOR"))
            {
                return RedirectToAction("Index", "Evento");
            }
            var listarEventos = _eventoService.GetAll().ToList();
            var listarEventosModel = _mapper.Map<List<EventoModel>>(listarEventos);

            foreach (var evento in listarEventosModel)
            {
                evento.Descricao = string.IsNullOrWhiteSpace(evento.Descricao) ? string.Empty : evento.Descricao;
            }

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
