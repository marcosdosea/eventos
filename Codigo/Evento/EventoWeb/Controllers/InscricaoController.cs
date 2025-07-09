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
            EventoModel eventoModel = _mapper.Map<EventoModel>(evento);
            var tipoInscricaoModel = _tipoinscricaoService.GetByEvento(idEvento).ToList();

            var model = new InscricaoEventoViewModel(){
                tipoInscricao = tipoInscricaoModel,
                eventoNavigation = eventoModel
            };
            
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [Route("RealizarInscricao/{idEvento}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> realizarInscricao(uint idEvento, InscricaoEventoModel inscricaoEvento)
        {
           
                var pessoaId = _pessoaService.GetByCpf(User.Identity.Name);
                var novaInscricao = new InscricaoEventoModel()
                {
                    IdPessoa = pessoaId.Id,
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
                
            
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        [Route("MinhasInscricoes")]
        public async Task<IActionResult> minhasInscricoes(){
        var inscricaoUser = _inscricaoService.GetAllEventsByUserId(User.Identity.Name);
        var listarEventosModel = inscricaoUser.Select(i => new InscricaoEventoModel
			{
				Id = i.Id,
				DataInscricao = (DateTime)i.DataInscricao,
				NomeCracha = i.NomeCracha,
				Status = i.Status,
                FrequenciaFinal = i.FrequenciaFinal,
                IdEventoNavigation = i.IdEventoNavigation
			}).ToList();
         return View(listarEventosModel);
        }

    }
}
