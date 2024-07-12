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
        private readonly IPessoaService _pessoaService;
        private readonly IInscricaoService _inscricaoService;
        private readonly IAreaInteresseService _areaInteresseService;
        private readonly IMapper _mapper;

        public EventoController(IEventoService eventoService, IMapper mapper, IEstadosbrasilService estadosbrasilService,IInscricaoService inscricaoService, ITipoeventoService tipoeventoService, IAreaInteresseService areaInteresseService, IPessoaService pessoaService)
        {
            _tipoEventoService = tipoeventoService;
            _estadosbrasilService = estadosbrasilService;
            _eventoService = eventoService;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
            _areaInteresseService = areaInteresseService;
            _pessoaService = pessoaService;
        }

        // GET: EventoController
        public ActionResult Index()
        {
			var listarEventos = _eventoService.GetAll().ToList();
			var listarEventosModel = listarEventos.Select(e => new EventoModel
			{
				Id = e.Id,
				DataInicio = (DateTime) e.DataInicio,
				Nome = e.Nome,
				Status = e.Status,
				IdTipoEvento = (uint) e.IdTipoEvento,
				NomeTipoEvento = _tipoEventoService.GetNomeById((uint)e.IdTipoEvento)

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
            var areaInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
            var viewModel = new EventocreateModel
			{
				Evento = eventoModel,
				Estados = new SelectList(estados, "Estado", "Nome"),
                TiposEventos = new SelectList(tiposEventos, "Id", "Nome"),
                AreaInteresse = new SelectList(areaInteresse, "Id","Nome")

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
            var areaInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
			var viewModel = new EventocreateModel
			{
				Evento = eventoModel,
				Estados = new SelectList(estados, "Estado", "Nome", eventoModel.Estado),
				TiposEventos = new SelectList(tiposEventos, "Id", "Nome", eventoModel.Id),
                AreaInteresse = new SelectList(areaInteresse, "Id","Nome", eventoModel.Id)
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

            string nomeTipoEvento = _tipoEventoService.GetNomeById(evento.IdTipoEvento);
			eventoModel.NomeTipoEvento = nomeTipoEvento;

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

        
        // GET: EventoController/GestaoPapel
        public ActionResult GestaoPapel(uint idEvento, int idPapel)
        {
            var gestorModel = new GestaoPapelModel
            {
	            IdPapel = idPapel,
                Evento = _eventoService.GetEventoSimpleDto(idEvento),
                Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento,idPapel),
            };
            return View(gestorModel);
        }

        // POST: EventoController/GestaoPapel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GestaoPapel( GestaoPapelModel gestaoPapelModel)
        {
            var pessoa = gestaoPapelModel.Pessoa;
            var idEvento = gestaoPapelModel.Evento.Id;
            var idPapel = gestaoPapelModel.IdPapel;
            pessoa.NomeCracha = pessoa.Nome;
            _pessoaService.CreatePessoaPapel(pessoa, idEvento, idPapel);
            return RedirectToAction("GestaoPapel", new { idEvento, idPapel });

        } 
        // POST: EventoController/DeletePessoaPapel
        public IActionResult DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel)
        {
            _inscricaoService.DeletePessoaPapel(idPessoa, idEvento, idPapel);

            return RedirectToAction("GestaoPapel", new { idEvento, idPapel });
        }
    }
}
