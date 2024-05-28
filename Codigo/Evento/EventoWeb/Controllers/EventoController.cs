using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    public class EventoController : Controller
    {
        private readonly IEventoService _eventoService;
        private readonly IMapper _mapper;

        public EventoController(IEventoService eventoService, IMapper mapper)
        {
            _eventoService = eventoService;
            _mapper = mapper;
        }


        // GET: EventoController
        public ActionResult Index()
        {
            var listarEventos = _eventoService.GetAll();
            var listarEventosModel = _mapper.Map<List<EventoModel>>(listarEventos);
            return View(listarEventosModel);
        }

        // GET: EventoController/Details/5
        public ActionResult Details(uint id)
        {
            Evento evento = _eventoService.Obter(id);
            EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
            return View(eventoModel);
        }

        // GET: EventoController/Create
        public ActionResult Create()
        {
            var eventoModel = new EventoModel();
            return View(eventoModel);
        }

        // POST: EventoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EventoModel eventoModel)
        {
            if (ModelState.IsValid)
            {
                var evento = _mapper.Map<Evento>(eventoModel);
                _eventoService.Inserir(evento);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: EventoController/Edit/5
        public ActionResult Edit(uint id)
        {
            Evento evento = _eventoService.Obter(id);
            EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
            return View(eventoModel);
        }

        // POST: EventoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, EventoModel eventoModel)
        {
            if (ModelState.IsValid){
                var evento = _mapper.Map<Evento>(eventoModel);
                _eventoService.Atualizar(evento);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: EventoController/Delete/5
        public ActionResult Delete(uint id)
        {
            Evento evento = _eventoService.Obter(id);
            EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
            return View(eventoModel);
        }

        // POST: EventoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, EventoModel eventoModel)
        {
            _eventoService.Remover(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
