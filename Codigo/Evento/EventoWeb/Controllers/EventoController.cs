using System.Drawing.Printing;
using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    public class EventoController : Controller
    {
        private readonly IEventoService _eventoService;
        private readonly IPessoaService _pessoaService;
        private readonly IMapper _mapper;

        public EventoController(IEventoService eventoService, IPessoaService pessoaService, IMapper mapper)
        {
            _eventoService = eventoService;
            _pessoaService = pessoaService;
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
            Evento evento = _eventoService.Get(id);
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
                _eventoService.Create(evento);
                return RedirectToAction(nameof(Index));
            }

            return View(eventoModel);
        }

        // GET: EventoController/Edit/5
        public ActionResult Edit(uint id)
        {
            var evento = _eventoService.Get(id);
            var eventoModel = _mapper.Map<EventoModel>(evento);
            return View(eventoModel);
        }

        // POST: EventoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, EventoModel eventoModel)
        {
            if (ModelState.IsValid)
            {
                var evento = _mapper.Map<Evento>(eventoModel);
                _eventoService.Edit(evento);
                return RedirectToAction(nameof(Index));
            }

            return View(eventoModel);
        }

        // GET: EventoController/Delete/5
        public ActionResult Delete(uint id)
        {
            var evento = _eventoService.Get(id);
            var eventoModel = _mapper.Map<EventoModel>(evento);
            return View(eventoModel);
        }

        // POST: EventoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, EventoModel eventoModel)
        {
            _eventoService.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        
        // GET: EventoController/CreateGestorEvento
        public ActionResult CreateGestorEvento()
        {
            var gestorModel = new GestorEventoModel();
            SetEventosInViewBag();
            return View(gestorModel);
        }

        // POST: EventoController/CreateGestorEvento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateGestorEvento(GestorEventoModel gestorEventoModel)
        {
            gestorEventoModel.NomeCracha = gestorEventoModel.Nome;
            var gestorEvento = _mapper.Map<Pessoa>(gestorEventoModel);
            _eventoService.CreateGestorModel(gestorEvento,gestorEventoModel.IdEvento);
            
            return RedirectToAction("Index");
        }


        private void SetEventosInViewBag()
        {
            ViewBag.Eventos = _mapper.Map<List<EventoModel>>(_eventoService.GetAll());
        }
    }
}

