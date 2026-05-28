using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize]
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

        public EventoController(
            UserManager<UsuarioIdentity> userManager,
            IEventoService eventoService,
            IMapper mapper,
            IEstadosbrasilService estadosbrasilService,
            IInscricaoService inscricaoService,
            ITipoeventoService tipoeventoService,
            IAreaInteresseService areaInteresseService,
            IPessoaService pessoaService,
            ISubeventoService subeventoService)
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

        // =====================================================================
        // LISTAGEM
        // =====================================================================

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            var eventos = _eventoService.GetAll().ToList();
            var model = eventos.Select(e => new EventoModel
            {
                Id = e.Id,
                DataInicio = e.DataInicio ?? DateTime.Now,
                Nome = e.Nome,
                Status = e.Status,
                IdTipoEvento = (uint)e.IdTipoEvento,
                NomeTipoEvento = _tipoEventoService.GetNomeById((uint)e.IdTipoEvento)
            }).ToList();
            return View(model);
        }

        // =====================================================================
        // CREATE
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("Create")]
        public ActionResult Create()
        {
            CarregarViewBags();
            return View(new EventoModel());
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EventoModel eventoModel)
        {
            LimparValidacoesSelects();
            if (ModelState.IsValid)
            {
                var evento = _mapper.Map<Evento>(eventoModel);
                _eventoService.Create(evento);
                return RedirectToAction(nameof(Index));
            }
            CarregarViewBags();
            return View(eventoModel);
        }

        // =====================================================================
        // DETAILS
        // =====================================================================

        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(uint id)
        {
            var evento = _eventoService.Get(id);
            if (evento == null) return NotFound();

            var model = _mapper.Map<EventoModel>(evento);
            model.NomeTipoEvento = _tipoEventoService.GetNomeById(evento.IdTipoEvento);
            return View(model);
        }

        // =====================================================================
        // EDIT
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(uint id)
        {
            var evento = _eventoService.Get(id);
            if (evento == null) return NotFound();

            var model = _mapper.Map<EventoModel>(evento);

            var areasDoEvento = _eventoService.GetAreasInteresseByEventoId(id);
            model.IdAreaInteresses = areasDoEvento.Select(a => a.Id).ToList();

            CarregarViewBags();
            return View(model);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, EventoModel eventoModel)
        {
            LimparValidacoesSelects();
            if (ModelState.IsValid)
            {
                var evento = _mapper.Map<Evento>(eventoModel);
                evento.Id = id;
                _eventoService.Edit(evento, eventoModel.IdAreaInteresses);
                return RedirectToAction(nameof(Index));
            }
            CarregarViewBags();
            return View(eventoModel);
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("Delete/{id}")]
        public ActionResult Delete(uint id)
        {
            var evento = _eventoService.Get(id);
            if (evento == null) return NotFound();

            var model = _mapper.Map<EventoModel>(evento);
            model.NomeTipoEvento = _tipoEventoService.GetNomeById(evento.IdTipoEvento);
            return View(model);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, EventoModel eventoModel)
        {
            _eventoService.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // GERENCIAR EVENTO
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("GerenciarEvento")]
        public IActionResult GerenciarEvento(uint idEvento)
        {
            var evento = _eventoService.Get(idEvento);
            if (evento == null) return NotFound();

            var userCpf = User.Identity.Name;
            var isAdmin = User.IsInRole("ADMINISTRADOR");
            var gestor = _inscricaoService.GetGestorInEvent(userCpf, idEvento);
            var colaborador = _inscricaoService.GetColaboradorInEvent(userCpf, idEvento);

            if (!isAdmin && gestor == null && colaborador == null)
            {
                TempData["Message"] = "Você não tem permissão para gerenciar este evento.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new GerenciarEventoModel
            {
                Evento = _mapper.Map<EventoModel>(evento),
                Subeventos = _subeventoService.GetByIdEvento(idEvento)
            };
            return View(viewModel);
        }

        // =====================================================================
        // ATRIBUIÇÃO DE PAPÉIS NO EVENTO
        // (Substitui ColaboradorController e ParticipanteController)
        // Papéis: 2 = Gestor | 3 = Colaborador | 4 = Participante
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("AtribuirPapel")]
        public ActionResult AtribuirPapel(uint idEvento, int idPapel)
        {
            var eventoDto = _eventoService.GetEventoSimpleDto(idEvento);
            if (eventoDto == null) return NotFound();

            // Apenas ADM pode atribuir Gestor
            if (idPapel == 2 && !User.IsInRole("ADMINISTRADOR"))
            {
                TempData["Message"] = "Apenas administradores podem atribuir gestores.";
                return RedirectToAction(nameof(GerenciarEvento), new { idEvento });
            }

            var viewModel = new GestaoPapelModel
            {
                Evento = eventoDto,
                Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, idPapel).ToList()
            };

            ViewBag.IdPapelRequisitado = idPapel;
            ViewBag.PapelNome = NomePapel(idPapel);

            return View(ViewPapel(idPapel), viewModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("AtribuirPapel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtribuirPapel(GestaoPapelModel model, int idPapelRequisitado)
        {
            ModelState.Remove("Inscricoes");

            if (!ModelState.IsValid)
            {
                model.Evento = _eventoService.GetEventoSimpleDto(model.Evento.Id);
                model.Inscricoes = _inscricaoService.GetByEventoAndPapel(model.Evento.Id, idPapelRequisitado).ToList();
                ViewBag.IdPapelRequisitado = idPapelRequisitado;
                ViewBag.PapelNome = NomePapel(idPapelRequisitado);
                return View(ViewPapel(idPapelRequisitado), model);
            }

            try
            {
                var pessoa = _mapper.Map<Pessoa>(model.Pessoa);

                // 1. Garante Pessoa no banco + usuário Identity + role
                await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, idPapelRequisitado);

                // 2. Vincula Pessoa ao Evento com o Papel (inscrição)
                await _inscricaoService.AtribuirPapelNoEventoAsync(pessoa, model.Evento.Id, idPapelRequisitado);

                // 3. Atualiza vagas se for participante
                if (idPapelRequisitado == 4)
                    _eventoService.AtualizarVagasDisponiveis(model.Evento.Id);

                return RedirectToAction(nameof(AtribuirPapel),
                    new { idEvento = model.Evento.Id, idPapel = idPapelRequisitado });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            model.Evento = _eventoService.GetEventoSimpleDto(model.Evento.Id);
            model.Inscricoes = _inscricaoService.GetByEventoAndPapel(model.Evento.Id, idPapelRequisitado).ToList();
            ViewBag.IdPapelRequisitado = idPapelRequisitado;
            ViewBag.PapelNome = NomePapel(idPapelRequisitado);
            return View(ViewPapel(idPapelRequisitado), model);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("DeletePessoaPapel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePessoaPapel(uint idPessoa, uint idEvento, uint idPapel)
        {
            var pessoa = _pessoaService.Get(idPessoa);
            if (pessoa != null)
            {
                await _inscricaoService.DeletePessoaPapelAsync(idPessoa, idEvento, idPapel, pessoa.Cpf);

                if (idPapel == 4)
                    _eventoService.AtualizarVagasDisponiveis(idEvento);
            }
            return RedirectToAction(nameof(AtribuirPapel), new { idEvento, idPapel });
        }

        // =====================================================================
        // HELPERS PRIVADOS
        // =====================================================================

        private void CarregarViewBags()
        {
            ViewBag.Estados = new SelectList(
                _estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome");
            ViewBag.TiposEventos = new SelectList(
                _tipoEventoService.GetAll().OrderBy(t => t.Nome), "Id", "Nome");
            ViewBag.AreaInteresse = new SelectList(
                _areaInteresseService.GetAll().OrderBy(a => a.Nome), "Id", "Nome");
        }

        private void LimparValidacoesSelects()
        {
            ModelState.Remove("Estados");
            ModelState.Remove("TiposEventos");
            ModelState.Remove("AreaInteresse");
        }

        private static string NomePapel(int idPapel) => idPapel switch
        {
            2 => "Gestor",
            3 => "Colaborador",
            4 => "Participante",
            _ => "Desconhecido"
        };

        private static string ViewPapel(int idPapel) => idPapel switch
        {
            2 => "CreateGestor",
            3 => "CreateColaborador",
            _ => "CreateParticipante"
        };
    }
}
