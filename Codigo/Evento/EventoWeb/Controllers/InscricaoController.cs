using AutoMapper;
using Core.Service;
using Core;
using Core.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class InscricaoController : Controller
    {
        private readonly IEventoService _eventoService;
        private readonly IPessoaService _pessoaService;
        private readonly IInscricaoService _inscricaoService;
        private readonly ISubeventoService _subeventoService;
        private readonly UserManager<UsuarioIdentity> _userManager;
        private readonly ITipoInscricaoService _tipoinscricaoService;
        private readonly IMapper _mapper;

        public InscricaoController(UserManager<UsuarioIdentity> userManager, ITipoInscricaoService tipoinscricaoService, IEventoService eventoService, IMapper mapper, IInscricaoService inscricaoService, IPessoaService pessoaService, ISubeventoService subeventoService)
        {
            _tipoinscricaoService = tipoinscricaoService;
            _eventoService = eventoService;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
            _pessoaService = pessoaService;
            _subeventoService = subeventoService;
            _userManager = userManager;
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(int id)
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Create")]
        public ActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(int id)
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpGet]
        [Route("Delete/{id}")]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [Authorize]
        [HttpGet]
        [Route("PessoaAllInscricao")]
        public IActionResult pessoaAllInscricao()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        [Route("RealizarInscricao/{idEvento}/{idSubevento?}")]
        public IActionResult realizarInscricao(uint idEvento, uint? idSubevento)
        {
            Evento evento = _eventoService.Get(idEvento);
            if (evento == null)
            {
                return RedirectToAction("Index", "Home");
            }

            EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
            var tipoInscricaoModel = _tipoinscricaoService.GetByEvento(idEvento).ToList();

            var model = new InscricaoEventoViewModel(){
                tipoInscricao = tipoInscricaoModel,
                eventoNavigation = eventoModel
            };

            if (User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name))
            {
                var pessoa = _pessoaService.GetByCpf(User.Identity.Name);
                if (pessoa != null && _inscricaoService.IsInscrito(pessoa.Id, idEvento))
                {
                    ViewBag.JaInscrito = true;
                }
            }
            
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [Route("RealizarInscricao/{idEvento}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> realizarInscricao(uint idEvento, InscricaoEventoModel inscricaoEvento)
        {
            var pessoa = _pessoaService.GetByCpf(User.Identity.Name);
            if (pessoa == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (_inscricaoService.IsInscrito(pessoa.Id, idEvento))
            {
                TempData["ParticipanteMessage"] = "Você já está inscrito neste evento!";
                return RedirectToAction("minhasInscricoes", new { idEvento = idEvento });
            }

            var novaInscricao = new InscricaoEventoModel()
            {
                IdPessoa = pessoa.Id,
                IdEvento = idEvento,
                IdPapel = 4,
                DataInscricao = DateTime.Now,
                NomeCracha = User.Identity.Name,
                Status = "S",
                IdTipoInscricao = inscricaoEvento.IdTipoInscricao,
                FrequenciaFinal = 0m,
                ValorTotal = inscricaoEvento.ValorTotal 
            };

            var inscricao = _mapper.Map<Inscricaopessoaevento>(novaInscricao);
            _inscricaoService.CreateInscricaoEvento(inscricao);
            _eventoService.AtualizarVagasDisponiveis(idEvento);

            TempData["ParticipanteSuccessMessage"] = "Inscrição realizada com sucesso!";
            return RedirectToAction("minhasInscricoes", new { idEvento = idEvento });
        }

        [Authorize]
        [HttpGet]
        [Route("MinhasInscricoes")]
        public async Task<IActionResult> minhasInscricoes(uint? idEvento)
        {
            var inscricaoUser = _inscricaoService.GetAllEventsByUserId(User.Identity.Name);
            var listarEventosModel = inscricaoUser.Select(i => new InscricaoEventoModel
            {
                Id = i.Id,
                IdEvento = i.IdEvento,
                DataInscricao = (DateTime)i.DataInscricao,
                NomeCracha = i.NomeCracha,
                Status = i.Status,
                FrequenciaFinal = i.FrequenciaFinal,
                IdEventoNavigation = i.IdEventoNavigation
            }).ToList();

            ViewBag.EventoId = idEvento ?? listarEventosModel.FirstOrDefault()?.IdEvento;
            return View(listarEventosModel);
        }

    }
}
