using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

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

        [Authorize(Roles = "ADMINISTRADOR")]
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

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
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
                            CarregarViewBags();
                            return View(eventoModel);
                        }
                    }
                }
                var evento = _mapper.Map<Evento>(eventoModel);
                _eventoService.Create(evento);
                evento.ImagemPortal = fotoSource;
                return RedirectToAction(nameof(Index));
            }
            CarregarViewBags();
            return View(eventoModel);
        }

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
        public ActionResult Edit(uint id, EventoModel viewModel)
        {
            LimparValidacoesSelects();
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
                            CarregarViewBags();
                            return View(viewModel);
                        }
                    }
                }
                var evento = _mapper.Map<Evento>(viewModel);
                evento.Id = id;
                _eventoService.Edit(evento, viewModel.IdAreaInteresses);
                _eventoService.AtualizarVagasDisponiveis(evento.Id);
                evento.ImagemPortal = fotoSource;
                return RedirectToAction(nameof(Index));
            }
            CarregarViewBags();
            return View(viewModel);
        }

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

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("CreateGestor")]
        public ActionResult CreateGestor(uint idEvento)
        {
            var gestorModel = new GestaoPapelModel
            {
                Evento = _eventoService.GetEventoSimpleDto(idEvento),
                Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 2).ToList(),
            };
            return View(gestorModel);
        }

        [HttpPost]
        [Route("CreateGestor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGestor(GestaoPapelModel gestaoPapelModel)
        {
            if (ModelState.IsValid)
            {
                var pessoaExistente = _pessoaService.GetByCpf(gestaoPapelModel.Pessoa.Cpf);
                var idEvento = gestaoPapelModel.Evento.Id;
                if (pessoaExistente is null)
                {
                    Pessoa pessoa = _mapper.Map<Pessoa>(gestaoPapelModel.Pessoa);
                    pessoaExistente = pessoa;
                }
                else
                {
                    var papel = _inscricaoService.GetPapelPessoaByEvento(pessoaExistente.Id, idEvento);
                    if (papel is 2 or 3)
                    {
                        var papelNome = papel == 2 ? "gestor" : "colaborador";
                        ModelState.AddModelError(string.Empty, $"A pessoa selecionada já é um {papelNome} do evento.");
                        gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(idEvento);
                        gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 2).ToList();
                        return View(gestaoPapelModel);
                    }
                }

                await _pessoaService.CreatePessoaPapelAsync(pessoaExistente, idEvento, 2);
                _eventoService.AtualizarVagasDisponiveis(idEvento);
                return RedirectToAction("GerenciarEvento", new { idEvento });
            }

            gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(gestaoPapelModel.Evento.Id);
            gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(gestaoPapelModel.Evento.Id, 2).ToList();
            return View(gestaoPapelModel);
        }

        [Authorize(Roles = "GESTOR")]
        [HttpGet]
        [Route("CreateColaborador")]
        public ActionResult CreateColaborador(uint idEvento)
        {
            var gestor = _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento);
            if (gestor != null)
            {
                var gestorModel = new GestaoPapelModel
                {
                    Evento = _eventoService.GetEventoSimpleDto(idEvento),
                    Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 3).ToList(),
                };
                return View(gestorModel);
            }
            else
            {
                TempData.Clear();
                TempData["Message"] = "Você não tem permissão para criar um colaborador!";
                return RedirectToAction("GerenciarEvento", "Evento", new { idEvento = idEvento });
            }
        }

        [HttpPost]
        [Route("CreateColaborador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateColaborador(GestaoPapelModel gestaoPapelModel)
        {
            if (ModelState.IsValid)
            {
                var pessoaExistente = _pessoaService.GetByCpf(gestaoPapelModel.Pessoa.Cpf);
                var idEvento = gestaoPapelModel.Evento.Id;
                if (pessoaExistente is null)
                {
                    Pessoa pessoa = _mapper.Map<Pessoa>(gestaoPapelModel.Pessoa);
                    pessoaExistente = pessoa;
                }
                else
                {
                    var papel = _inscricaoService.GetPapelPessoaByEvento(pessoaExistente.Id, idEvento);
                    if (papel is 2 or 3)
                    {
                        var papelNome = papel == 2 ? "gestor" : "colaborador";
                        ModelState.AddModelError(string.Empty, $"A pessoa selecionada já é um {papelNome} do evento.");
                        gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(idEvento);
                        gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 3).ToList();
                        return View(gestaoPapelModel);
                    }
                }

                await _pessoaService.CreatePessoaPapelAsync(pessoaExistente, idEvento, 3);
                _eventoService.AtualizarVagasDisponiveis(idEvento);
                return RedirectToAction("CreateColaborador", new { idEvento });
            }

            gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(gestaoPapelModel.Evento.Id);
            gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(gestaoPapelModel.Evento.Id, 3).ToList();
            return View(gestaoPapelModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("CreateParticipante")]
        public ActionResult CreateParticipante(uint idEvento)
        {
            var gestorModel = new GestaoPapelModel
            {
                Evento = _eventoService.GetEventoSimpleDto(idEvento),
                Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 4).ToList(),
            };
            return View(gestorModel);
        }

        [HttpPost]
        [Route("CreateParticipante")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateParticipante(GestaoPapelModel gestaoPapelModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = _pessoaService.GetByCpf(gestaoPapelModel.Pessoa.Cpf);
                var idEvento = gestaoPapelModel.Evento.Id;
                var papel = _inscricaoService.GetPapelPessoaByEvento(pessoa.Id, idEvento);

                if (papel == 4)
                {
                    ModelState.AddModelError(string.Empty, "A pessoa selecionada já é um participante.");
                    gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(idEvento);
                    gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(idEvento, 4).ToList();
                    return View(gestaoPapelModel);
                }

                pessoa.NomeCracha = pessoa.Nome;
                _pessoaService.CreatePessoaPapelAsync(pessoa, idEvento, 4);
                _eventoService.AtualizarVagasDisponiveis(idEvento);
                return RedirectToAction("GerenciarEvento", new { idEvento });
            }

            gestaoPapelModel.Evento = _eventoService.GetEventoSimpleDto(gestaoPapelModel.Evento.Id);
            gestaoPapelModel.Inscricoes = _inscricaoService.GetByEventoAndPapel(gestaoPapelModel.Evento.Id, 4).ToList();
            return View(gestaoPapelModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("RemoverPapel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverPapel(uint idInscricao, uint idEvento)
        {
            var inscricao = _inscricaoService.GetById(idInscricao);
            if (inscricao == null)
            {
                TempData["Message"] = "Inscrição não encontrada para exclusão.";
                return RedirectToAction("GerenciarEvento", new { idEvento });
            }

            var pessoa = _pessoaService.Get(inscricao.IdPessoa);
            if (pessoa == null || string.IsNullOrEmpty(pessoa.Cpf))
            {
                TempData["Message"] = "Pessoa não encontrada para exclusão.";
                return RedirectToAction("GerenciarEvento", new { idEvento });
            }

            var cpf = pessoa.Cpf.Replace(".", "").Replace("-", "");
            var idPapel = inscricao.IdPapel;

            await _inscricaoService.DeletePessoaPapelAsync(inscricao.IdPessoa, idEvento, (uint)idPapel, cpf);
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
                    return RedirectToAction("GerenciarEvento", new { idEvento });
            }
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("GerenciarEventoListar")]
        public async Task<IActionResult> GerenciarEventoListar()
        {
            string userCpf = null;
            uint idPapel = 0;
            bool isAdmin = false;

            if (User.Identity.IsAuthenticated)
            {
                userCpf = User.FindFirstValue(ClaimTypes.Name);
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("ADMINISTRADOR"))
                    {
                        isAdmin = true;
                    }
                    else if (roles.Contains("GESTOR"))
                    {
                        idPapel = 2;
                    }
                    else if (roles.Contains("COLABORADOR"))
                    {
                        idPapel = 3;
                    }
                }
            }

            IEnumerable<Evento> listarEventos;
            if (isAdmin)
            {
                listarEventos = _eventoService.GetAll();
            }
            else
            {
                listarEventos = _eventoService.GetEventByCpf(userCpf, idPapel);
            }

            var eventosList = listarEventos.ToList();
            var tiposEvento = _tipoEventoService.GetAll().ToDictionary(t => t.Id, t => t.Nome);

            var listarEventosModel = eventosList.Select(e => new EventoModel
            {
                Id = e.Id,
                DataInicio = e.DataInicio ?? DateTime.Now,
                Nome = e.Nome,
                Status = e.Status,
                IdTipoEvento = (uint)e.IdTipoEvento,
                NomeTipoEvento = tiposEvento.ContainsKey((uint)e.IdTipoEvento) ? tiposEvento[(uint)e.IdTipoEvento] : "Tipo não encontrado"
            }).ToList();

            return View(listarEventosModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("GerenciarEvento")]
        public IActionResult GerenciarEvento(uint idEvento)
        {
            Evento evento = _eventoService.Get(idEvento);
            if (evento == null)
            {
                TempData.Clear();
                TempData["Message"] = "Evento não encontrado!";
                return RedirectToAction("Index", "Home");
            }

            var gestor = _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento);
            var colaborador = _inscricaoService.GetColaboradorInEvent(User.Identity.Name, idEvento);
            var isAdmin = User.IsInRole("ADMINISTRADOR");

            if (!isAdmin && gestor == null && colaborador == null)
            {
                TempData.Clear();
                TempData["Message"] = "Você não tem permissão para gerenciar este evento!";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new GerenciarEventoModel()
            {
                Evento = _mapper.Map<EventoModel>(evento),
                Subeventos = _subeventoService.GetByIdEvento(idEvento)
            };
            return View(viewModel);
        }

        private void CarregarViewBags()
        {
            ViewBag.Estados = new SelectList(_estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome");
            ViewBag.TiposEventos = new SelectList(_tipoEventoService.GetAll().OrderBy(t => t.Nome), "Id", "Nome");
            ViewBag.AreaInteresse = new SelectList(_areaInteresseService.GetAll().OrderBy(a => a.Nome), "Id", "Nome");
        }

        private void LimparValidacoesSelects()
        {
            ModelState.Remove("Estados");
            ModelState.Remove("TiposEventos");
            ModelState.Remove("AreaInteresse");
        }
    }
}