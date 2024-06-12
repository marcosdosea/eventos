using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    public class EventoController : Controller
    {
        private readonly IEstadosbrasilService _estadosbrasilService;
        private readonly ITipoeventoService _tipoEventoService;
        private readonly IEventoService _eventoService;
        private readonly IInscricaoService _inscricaoService;
        private readonly IMapper _mapper;

        public EventoController(IEventoService eventoService, IMapper mapper, IEstadosbrasilService estadosbrasilService,IInscricaoService inscricaoService, ITipoeventoService tipoeventoService)
        {
            _tipoEventoService = tipoeventoService;
            _estadosbrasilService = estadosbrasilService;
            _eventoService = eventoService;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
        }

        // GET: EventoController
        public ActionResult Index()
        {
			var listarEventos = _eventoService.GetAll().ToList();
			var listarEventosModel = listarEventos.Select(e => new EventoModel
			{
				Id = e.Id,
				DataInicio = e.DataInicio,
				Nome = e.Nome,
				Status = e.Status,
				IdTipoEvento = e.IdTipoEvento,
				NomeTipoEvento = _tipoEventoService.GetNomeById(e.IdTipoEvento)
			}).ToList();

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
			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
            var viewModel = new EventocreateModel
			{
				Evento = eventoModel,
				Estados = new SelectList(estados, "Estado", "Nome"),
                TiposEventos = new SelectList(tiposEventos, "Id", "Nome")
            };

			return View(viewModel);
		}

		// POST: EventoController/Create
		[HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EventocreateModel eventoModel)
        {
			var evento = _mapper.Map<Evento>(eventoModel.Evento);
			_eventoService.Create(evento);
			return RedirectToAction(nameof(Index));
		}

		// GET: EventoController/Edit/5
		public ActionResult Edit(uint id)
		{
			var evento = _eventoService.Get(id);
			if (evento == null)
			{
				return NotFound();
			}

			var eventoModel = _mapper.Map<EventoModel>(evento);
			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
			var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
			var viewModel = new EventocreateModel
			{
				Evento = eventoModel,
				Estados = new SelectList(estados, "Estado", "Nome", eventoModel.Estado),
				TiposEventos = new SelectList(tiposEventos, "Id", "Nome", eventoModel.Id)
			};

			return View(viewModel);
		}

		// POST: EventoController/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(uint id, EventocreateModel viewModel)
		{
			var evento = _mapper.Map<Evento>(viewModel.Evento);
			_eventoService.Edit(evento);
			return RedirectToAction(nameof(Index));
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
        public ActionResult CreateGestorEvento(uint id)
        {
            var gestorModel = new GestorEventoModel
            {
                Evento = _eventoService.GetEventoSimpleDto(id),
                Inscricoes = _inscricaoService.GetInscricaoPessoaEvento(id)
            };
            return View(gestorModel);
        }

        // POST: EventoController/CreateGestorEvento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateGestorEvento( GestorEventoModel gestorEventoModel)
        {
            var pessoa = gestorEventoModel.Pessoa;
            var idEvento = gestorEventoModel.Evento.Id;
            pessoa.NomeCracha = pessoa.Nome;
            _eventoService.CreateGestorModel(pessoa, idEvento);
            return RedirectToAction("CreateGestorEvento", new { id = idEvento });

        } 
        // POST: EventoController/DeletePessoaPapel
        public IActionResult DeletePessoaPapel(uint idPessoa, uint idEvento)
        {
            _inscricaoService.DeletePessoaPapel(idPessoa, idEvento);

            return RedirectToAction("CreateGestorEvento", new { id = idEvento });
        }
    }
}
