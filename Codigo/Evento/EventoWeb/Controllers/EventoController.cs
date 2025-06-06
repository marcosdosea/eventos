﻿using System.Security.Claims;
using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Org.BouncyCastle.Bcpg;

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
		private readonly UserManager<UsuarioIdentity> _userManager;

		public EventoController(UserManager<UsuarioIdentity> userManager, IEventoService eventoService, IMapper mapper, IEstadosbrasilService estadosbrasilService, IInscricaoService inscricaoService, ITipoeventoService tipoeventoService, IAreaInteresseService areaInteresseService, IPessoaService pessoaService, ISubeventoService subeventoService)
		{
			_userManager = userManager;
			_tipoEventoService = tipoeventoService;
			_estadosbrasilService = estadosbrasilService;
			_eventoService = eventoService;
			_inscricaoService = inscricaoService;
			_mapper = mapper;
			_areaInteresseService = areaInteresseService;
			_pessoaService = pessoaService;
			_subeventoService = subeventoService;
		}

		[Authorize(Roles = "ADMINISTRADOR")]
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

		[Authorize(Roles = "ADMINISTRADOR")]
		// GET: EventoController/Details/5
		public ActionResult Details(uint id)
		{
			Evento evento = _eventoService.Get(id);
			EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
			return View(eventoModel);
		}

		[Authorize(Roles = "ADMINISTRADOR")]
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

		[Authorize(Roles = "ADMINISTRADOR")]
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

		[Authorize(Roles = "ADMINISTRADOR")]
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

		[Authorize(Roles = "ADMINISTRADOR")]
		// GET: EventoController/CreateGestor
		public ActionResult CreateGestor(uint idEvento)
		{
			var gestorModel = new GestaoPapelModel
			{
				Evento = _eventoService.GetEventoSimpleDto(idEvento),
				Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 2),
			};
			return View(gestorModel);
		}

		// POST: EventoController/CreateGestor
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CreateGestor(GestaoPapelModel gestaoPapelModel)
		{
			if (ModelState.IsValid)
			{
				var pessoa = _pessoaService.GetByCpf(gestaoPapelModel.Pessoa.Cpf);
				var idEvento = gestaoPapelModel.Evento.Id;

				var papel = _inscricaoService.GetPapelPessoaByEvento(pessoa.Id, idEvento);
				
				if (papel is 2 or 3)
				{
					var papelNome = "";
					
					switch (papel)
					{
						case 2:
							papelNome = "gestor";
							break;
						case 3:
							papelNome = "colaborador";
							break;
					}

					ModelState.AddModelError(string.Empty, $"A pessoa selecionada já é um {papelNome} do evento.");
    
					gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(idEvento);
					gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 2);
    
					return View(gestaoPapelModel);
				}


				pessoa.NomeCracha = pessoa.Nome;
				_pessoaService.CreatePessoaPapelAsync(pessoa, idEvento, 2);
				_eventoService.AtualizarVagasDisponiveis(idEvento);

				return RedirectToAction("CreateGestor", new { idEvento });
			}

			gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(gestaoPapelModel.Evento.Id);
			gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(gestaoPapelModel.Evento.Id, 2);
			return View(gestaoPapelModel);
		}

		
		[Authorize]
		// GET: EventoController/CreateColaborador
		public ActionResult CreateColaborador(uint idEvento)
		{
			
			var gestor = _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento);
			if(User.IsInRole("ADMINISTRADOR") || gestor != null){
			var gestorModel = new GestaoPapelModel
			{
				Evento = _eventoService.GetEventoSimpleDto(idEvento),
				Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 3),
			};
			return View(gestorModel);
			}else{
				TempData.Clear();	
				TempData["Message"] = "Você não tem permissão para criar um colaborador!";
				return RedirectToAction("GerenciarEvento","Evento", new { idEvento = idEvento});
			}
			
		}


		// POST: EventoController/CreateColaborador
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CreateColaborador(GestaoPapelModel gestaoPapelModel)
		{
			if (ModelState.IsValid)
			{
				var pessoa = _pessoaService.GetByCpf(gestaoPapelModel.Pessoa.Cpf);
				var idEvento = gestaoPapelModel.Evento.Id;

				var papel = _inscricaoService.GetPapelPessoaByEvento(pessoa.Id, idEvento);
				
				if (papel is 2 or 3)
				{
					var papelNome = "";
					
					switch (papel)
					{
						case 2:
							papelNome = "gestor";
							break;
						case 3:
							papelNome = "colaborador";
							break;
					}

					ModelState.AddModelError(string.Empty, $"A pessoa selecionada já é um {papelNome} do evento.");
    
					gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(idEvento);
					gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 3);
    
					return View(gestaoPapelModel);
				}


				pessoa.NomeCracha = pessoa.Nome;
				_pessoaService.CreatePessoaPapelAsync(pessoa, idEvento, 3);
				_eventoService.AtualizarVagasDisponiveis(idEvento);

				return RedirectToAction("CreateColaborador", new { idEvento });
			}

			gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(gestaoPapelModel.Evento.Id);
			gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(gestaoPapelModel.Evento.Id, 3);
			return View(gestaoPapelModel);
		}
		
		[Authorize]
		// GET: EventoController/CreateParticipante
		public ActionResult CreateParticipante(uint idEvento)
		{
			var gestor = _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento);
			var colaborador = _inscricaoService.GetColaboradorInEvent(User.Identity.Name, idEvento);

			if(gestor == null && colaborador == null){
				TempData.Clear();
				TempData["Message"] = "Você não tem permissão para criar um participante!";
				return RedirectToAction("GerenciarEvento");
			}else{
			var gestorModel = new GestaoPapelModel
			{
				Evento = _eventoService.GetEventoSimpleDto(idEvento),
				Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 4),
			};
			return View(gestorModel);
			}
		}

		// POST: EventoController/CreateParticipante
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CreateParticipante(GestaoPapelModel gestaoPapelModel)
		{
			if (ModelState.IsValid)
			{
				var pessoa = _pessoaService.GetByCpf(gestaoPapelModel.Pessoa.Cpf);
				var idEvento = gestaoPapelModel.Evento.Id;

				var existingInscricao = _inscricaoService.GetByEventoAndPapel(idEvento, 4)
					.FirstOrDefault(i => i.IdPessoa == pessoa.Id);

				if (existingInscricao != null)
				{
					ModelState.AddModelError(string.Empty, "A pessoa selecionada já é um participante.");
					gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(idEvento);
					gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 4);
					return View(gestaoPapelModel);
				}

				pessoa.NomeCracha = pessoa.Nome;
				_pessoaService.CreatePessoaPapelAsync(pessoa, idEvento, 4);
				_eventoService.AtualizarVagasDisponiveis(idEvento);

				return RedirectToAction("CreateParticipante", new { idEvento });
			}

			gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(gestaoPapelModel.Evento.Id);
			gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(gestaoPapelModel.Evento.Id, 4);
			return View(gestaoPapelModel);
		}

		// POST: EventoController/DeletePessoaPapel
		public IActionResult DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel)
		{
			var cpf = _pessoaService.Get(idPessoa).Cpf.Replace(".", "").Replace("-", "");
			
			_inscricaoService.DeletePessoaPapel(idPessoa, idEvento, idPapel,cpf);

			_eventoService.AtualizarVagasDisponiveis(idEvento);

			switch (idPapel)
			{
				case 2:
					return RedirectToAction("CreateGestor", new { idEvento });
				case 3:
					return RedirectToAction("CreateColaborador", new { idEvento });
				case 4:
					return RedirectToAction("CreateParticipante", new { idEvento });
				default:
					return RedirectToAction("Index");
			}
		}

		[Authorize]
		//GET: EventoController/GerenciarEventoListar
		public async Task<IActionResult> GerenciarEventoListar()
		{
			string userCpf = null;
			uint idPapel = 0;

			if (User.Identity.IsAuthenticated)
			{
				userCpf = User.FindFirstValue(ClaimTypes.Name);
				
				var user = await _userManager.GetUserAsync(User);

				if (user != null)
				{
					var roles = await _userManager.GetRolesAsync(user);
					
					if (roles.Contains("GESTOR"))
					{
						idPapel = 2;
					}
					else if (roles.Contains("COLABORADOR"))
					{
						idPapel = 3;
					}
				}
			}
			
			var listarEventos = _eventoService.GetEventByCpf(userCpf, idPapel).ToList();
			
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

		
		[Authorize]
		//GET: EventoController/GerenciarEvento
		public IActionResult GerenciarEvento(uint idEvento)
		{
			Evento evento = _eventoService.Get(idEvento);
			var gestor = _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento);
			var colaborador = _inscricaoService.GetColaboradorInEvent(User.Identity.Name, idEvento);

			if(gestor == null && colaborador == null){
				TempData.Clear();
				TempData["Message"] = "Você não tem permissão para gerenciar este evento!";
				return RedirectToAction("Index","Home");
			}else{
			var viewModel = new GerenciarEventoModel()
			{
				Evento = _mapper.Map<EventoModel>(evento),
				Subeventos = _subeventoService.GetByIdEvento(idEvento)
			};
			return View(viewModel);
			}
		}

		//[Authorize(Roles = "GESTOR, COLABORADOR")]
		// GET: EventoController/GestorEditarEvento/5
		public ActionResult GestorEditarEvento(uint id)
		{
			var evento = _eventoService.Get(id);
			
			if (evento == null)
			{
				return NotFound();
			}

			var viewModel = _mapper.Map<EventoModel>(evento);


			var todasAreasInteresse = _areaInteresseService.GetAll().OrderBy(a => a.Nome);
			viewModel.AreaInteresse = new SelectList(todasAreasInteresse, "Id", "Nome");

			var areasInteresseAssociadas = _eventoService.GetAreasInteresseByEventoId(id);
			viewModel.IdAreaInteresses = areasInteresseAssociadas.Select(a => a.Id).ToList();

			var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
			var tiposEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);

			viewModel.Estados = new SelectList(estados, "Estado", "Nome", viewModel.Estado);
			viewModel.TiposEventos = new SelectList(tiposEventos, "Id", "Nome", viewModel.IdTipoEvento);

			return View(viewModel);
		}



		// POST: EventoController/GestorEditarEvento/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult GestorEditarEvento(uint id, EventoModel viewModel)
		{
		//	ModelState.Remove("Estados");
		//	ModelState.Remove("TiposEventos");
		//	ModelState.Remove("AreaInteresse");
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
			viewModel.AreaInteresse = new SelectList(areasInteresse, "Id", "Nome");

			return View(viewModel);
		}
	}
}
