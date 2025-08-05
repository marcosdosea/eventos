using AutoMapper;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
            var listarEventos = _eventoService.GetAll().ToList();
            var listarEventosModel = _mapper.Map<List<EventoModel>>(listarEventos);
            return View(listarEventosModel);
        }

        [Authorize(Roles = "GESTOR")]
        public IActionResult GetGestorEventos()
        {
            string userCpf = User.FindFirstValue(ClaimTypes.Name);
            
            if (string.IsNullOrEmpty(userCpf))
            {
                return PartialView("_GestorEventosList", new List<EventoModel>());
            }

            var listarEventos = _eventoService.GetEventByCpf(userCpf, 2); // 2 = papel de gestor
            var eventosList = listarEventos.ToList();
            
            // Buscar todos os tipos de evento de uma vez
            var tiposEvento = _tipoEventoService.GetAll().ToDictionary(t => t.Id, t => t.Nome);
            
            var listarEventosModel = eventosList.Select(e => new EventoModel
            {
                Id = e.Id,
                DataInicio = e.DataInicio,
                Nome = e.Nome,
                Status = e.Status,
                IdTipoEvento = (uint)e.IdTipoEvento,
                NomeTipoEvento = tiposEvento.ContainsKey((uint)e.IdTipoEvento) ? tiposEvento[(uint)e.IdTipoEvento] : "Tipo não encontrado"
            }).ToList();

            return PartialView("_GestorEventosList", listarEventosModel);
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
