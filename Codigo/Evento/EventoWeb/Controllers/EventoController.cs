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
		private readonly ISubeventoService _subeventoService;
		private readonly IMapper _mapper;

		public EventoController(IEventoService eventoService, IMapper mapper, IEstadosbrasilService estadosbrasilService, IInscricaoService inscricaoService, ITipoeventoService tipoeventoService, IAreaInteresseService areaInteresseService, IPessoaService pessoaService, ISubeventoService subeventoService)
		{
			_tipoEventoService = tipoeventoService;
			_estadosbrasilService = estadosbrasilService;
			_eventoService = eventoService;
			_inscricaoService = inscricaoService;
			_mapper = mapper;
			_areaInteresseService = areaInteresseService;
			_pessoaService = pessoaService;
			_subeventoService = subeventoService;
		}

		// GET: EventoController
		public ActionResult Index()
		{
			var listarEventos = _eventoService.GetAll().ToList();
			var listarEventosModel = listarEventos.Select(e => new EventoModel
			{
				Id = e.Id,
				DataInicio = (DateTime)e.DataInicio,
				Nome = e.Nome,
				Status = e.Status,
				IdTipoEvento = (uint)e.IdTipoEvento,
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
			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
			var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
			var areaInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
			var viewModel = new EventoModel();
			viewModel.Estados = new SelectList(estados, "Estado", "Nome");
			viewModel.TiposEventos = new SelectList(tiposEventos, "Id", "Nome");
			viewModel.AreaInteresse = new SelectList(areaInteresse, "Id", "Nome");

			return View(viewModel);
		}

		// POST: EventoController/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(EventoModel eventoModel)
		{
			ModelState.Remove("Estados");
			ModelState.Remove("TiposEventos");
			ModelState.Remove("AreaInteresse");

			if (ModelState.IsValid)
			{
				byte[] fotoSource = null;
				if (eventoModel.ImagemPortal != null && eventoModel.ImagemPortal.Length > 0)
				{
					using (var memoryStream = new MemoryStream())
					{
						eventoModel.ImagemPortal.CopyTo(memoryStream);

						if (memoryStream.Length <= 65535)
						{
							fotoSource = memoryStream.ToArray();
						}
						else
						{
							ModelState.AddModelError("Foto", "O arquivo é muito grande. Deve ser menor que 64 KB.");
							return View(eventoModel);
						}
					}
				}
				var evento = _mapper.Map<Evento>(eventoModel);
				_eventoService.Create(evento);
				evento.ImagemPortal = fotoSource;
				return RedirectToAction(nameof(Index));
			}



			return View(eventoModel);
		}

		// GET: EventoController/Edit/5
		public ActionResult Edit(uint id)
		{
			var evento = _eventoService.Get(id);
			if (evento == null)
			{
				return NotFound();
			}

			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
			var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
			var areaInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
			var viewModel = _mapper.Map<EventoModel>(evento);
			viewModel.Estados = new SelectList(estados, "Estado", "Nome", viewModel.Estado);
			viewModel.TiposEventos = new SelectList(tiposEventos, "Id", "Nome", viewModel.Id);
			viewModel.AreaInteresse = new SelectList(areaInteresse, "Id", "Nome", viewModel.Id);

			return View(viewModel);
		}

		// POST: EventoController/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(uint id, EventoModel viewModel)
		{
			ModelState.Remove("Estados");
			ModelState.Remove("TiposEventos");
			ModelState.Remove("AreaInteresse");

			if (ModelState.IsValid)
			{
				byte[] fotoSource = null;
				if (viewModel.ImagemPortal != null && viewModel.ImagemPortal.Length > 0)
				{
					using (var memoryStream = new MemoryStream())
					{
						viewModel.ImagemPortal.CopyTo(memoryStream);

						if (memoryStream.Length <= 65535)
						{
							fotoSource = memoryStream.ToArray();
						}
						else
						{
							ModelState.AddModelError("Foto", "O arquivo é muito grande. Deve ser menor que 64 KB.");
							return View(viewModel);
						}
					}
				}
				var evento = _mapper.Map<Evento>(viewModel);
				var idsAreaInteresse = new List<uint>
				{
					viewModel.IdAreaInteresse
				};
				_eventoService.Edit(evento, idsAreaInteresse); _eventoService.AtualizarVagasDisponiveis(evento.Id);
				evento.ImagemPortal = fotoSource;
				return RedirectToAction(nameof(Index));
			}
			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
			var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
			var areaInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
			viewModel.Estados = new SelectList(estados, "Estado", "Nome", viewModel.Estado);
			viewModel.TiposEventos = new SelectList(tiposEventos, "Id", "Nome", viewModel.Id);
			viewModel.AreaInteresse = new SelectList(areaInteresse, "Id", "Nome", viewModel.Id);

			return View(viewModel);
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


		// GET: EventoController/CreatePessoaPapel
		public ActionResult CreatePessoaPapel(uint idEvento, int idPapel)
		{
			var gestorModel = new GestaoPapelModel
			{
				IdPapel = idPapel,
				Evento = _eventoService.GetEventoSimpleDto(idEvento),
				Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, idPapel),
			};
			return View(gestorModel);
		}

		// POST: EventoController/CreatePessoaPapel
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CreatePessoaPapel(GestaoPapelModel gestaoPapelModel)
		{
			var pessoa = gestaoPapelModel.Pessoa;
			var idEvento = gestaoPapelModel.Evento.Id;
			var idPapel = gestaoPapelModel.IdPapel;
			pessoa.NomeCracha = pessoa.Nome;
			_pessoaService.CreatePessoaPapel(pessoa, idEvento, idPapel);

			_eventoService.AtualizarVagasDisponiveis(idEvento);

			return RedirectToAction("CreatePessoaPapel", new { idEvento, idPapel });
		}

		// POST: EventoController/DeletePessoaPapel
		public IActionResult DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel)
		{
			_inscricaoService.DeletePessoaPapel(idPessoa, idEvento, idPapel);

			_eventoService.AtualizarVagasDisponiveis(idEvento);

			return RedirectToAction("CreatePessoaPapel", new { idEvento, idPapel });
		}

		//GET: EventoController/GerenciarEvento
		public IActionResult GerenciarEventoListar()
		{
			var listarEventos = _eventoService.GetAll().ToList();
			var listarEventosModel = listarEventos.Select(e => new EventoModel
			{
				Id = e.Id,
				DataInicio = (DateTime)e.DataInicio,
				Nome = e.Nome,
				Status = e.Status,
				IdTipoEvento = (uint)e.IdTipoEvento,
				NomeTipoEvento = _tipoEventoService.GetNomeById((uint)e.IdTipoEvento)
			}).ToList();

			return View(listarEventosModel);
		}

		//GET: EventoController/GerenciarEventoListar
		public ActionResult GerenciarEvento(uint idEvento)
		{
			Evento evento = _eventoService.Get(idEvento);
			var viewModel = new GerenciarEventoModel()
			{
				Evento = _mapper.Map<EventoModel>(evento),
				Subeventos = _subeventoService.GetByIdEvento(idEvento)
			};
			return View(viewModel);

		}

		// GET: EventoController/GestorEditarEvento/5
		public ActionResult GestorEditarEvento(uint id)
		{
			var evento = _eventoService.Get(id);
			if (evento == null)
			{
				return NotFound();
			}

			var viewModel = _mapper.Map<EventoModel>(evento);
			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
			var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
			var areaInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
			viewModel.Estados = new SelectList(estados, "Estado", "Nome", viewModel.Estado);
			viewModel.TiposEventos = new SelectList(tiposEventos, "Id", "Nome", viewModel.Id);
			viewModel.AreaInteresse = new SelectList(areaInteresse, "Id", "Nome", viewModel.Id);

			return View(viewModel);
		}

		// POST: EventoController/GestorEditarEvento/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult GestorEditarEvento(uint id, EventoModel viewModel)
		{
			ModelState.Remove("Estados");
			ModelState.Remove("TiposEventos");
			ModelState.Remove("AreaInteresse");
			if (ModelState.IsValid)
			{
				byte[] fotoSource = null;
				if (viewModel.ImagemPortal != null && viewModel.ImagemPortal.Length > 0)
				{
					using (var memoryStream = new MemoryStream())
					{
						viewModel.ImagemPortal.CopyTo(memoryStream);

						if (memoryStream.Length <= 65535)
						{
							fotoSource = memoryStream.ToArray();
						}
						else
						{
							ModelState.AddModelError("Foto", "O arquivo é muito grande. Deve ser menor que 64 KB.");
							return View(viewModel);
						}
					}
				}
				var evento = _mapper.Map<Evento>(viewModel);
				var idsAreaInteresse = viewModel.IdAreaInteresses;
				_eventoService.Edit(evento, idsAreaInteresse);
				evento.ImagemPortal = fotoSource;
				_eventoService.AtualizarVagasDisponiveis(evento.Id);
				return RedirectToAction("GerenciarEvento", "Evento", new { idEvento = id });
			}
			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
			var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
			var areasInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
			viewModel.Estados = new SelectList(estados, "Estado", "Nome", viewModel.Estado);
			viewModel.TiposEventos = new SelectList(tiposEventos, "Id", "Nome", viewModel.IdTipoEvento);
			viewModel.AreaInteresse = new SelectList(areasInteresse, "Id", "Nome", viewModel.IdAreaInteresse);
			return View(viewModel);
		}
	}
}
