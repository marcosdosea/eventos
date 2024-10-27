using AutoMapper;
using Core.Service;
using Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;

namespace EventoWeb.Controllers
{
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

        // GET: InscricaoController
        public ActionResult Index()
        {
            return View();
        }

        // GET: InscricaoController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: InscricaoController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: InscricaoController/Create
        [HttpPost]
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

        // GET: InscricaoController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: InscricaoController/Edit/5
        [HttpPost]
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

        // GET: InscricaoController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: InscricaoController/Delete/5
        [HttpPost]
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


        // GET: InscricaoController/pessoaAllInscricao/id
        public IActionResult pessoaAllInscricao()
        {
            return View();
        }

        // GET: InscricaoController/realizarInscricao/id
        public IActionResult realizarInscricao(uint idEvento,uint? idSubevento)
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

        // POST: InscricaoController/realizarInscricao/id
        [HttpPost]
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

    }
}
