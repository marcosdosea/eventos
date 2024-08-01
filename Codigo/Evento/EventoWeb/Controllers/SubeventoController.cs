using AutoMapper;
using EventoWeb.Models;
using Core;
using Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Service;


namespace EventoWeb.Controllers
{
    public class SubeventoController : Controller
    {
        private readonly ISubeventoService _subeventoService;
        private readonly IEventoService _eventoService;
        private readonly ITipoeventoService _tipoEventoService;
        private readonly IInscricaoService _inscricaoService; 
        private readonly IMapper _mapper;
        public SubeventoController(ISubeventoService subeventoService, IMapper mapper, IEventoService eventoService, ITipoeventoService tipoeventoService, IInscricaoService inscricaoService)
        {
            _subeventoService = subeventoService;
            _eventoService = eventoService;
            _tipoEventoService = tipoeventoService;
            _mapper = mapper;
            _inscricaoService = inscricaoService;
        }
        // GET: SubeventoController
        public ActionResult Index()
        {
            var listaSubeventos = _subeventoService.GetAll().ToList(); ;
            var listaSubeventosModel = listaSubeventos.Select(e => new SubeventoModel
            {
                Id = e.Id,
                Nome = e.Nome,
                IdEvento = e.IdEvento,
                NomeEvento = _eventoService.GetNomeById(e.IdEvento),
                DataInicio = e.DataInicio,
                Status = e.Status,
                IdTipoEvento = e.IdTipoEvento,
                NomeTipoEvento = _tipoEventoService.GetNomeById(e.IdTipoEvento)

            }).ToList();
            return View(listaSubeventosModel);
        }
        // GET: SubeventoController/Details/5
        public ActionResult Details(uint id)
        {
            Subevento subevento = _subeventoService.Get(id);
            SubeventoModel subeventoModel = _mapper.Map<SubeventoModel>(subevento);
            return View(subeventoModel);
        }
        // GET: SubeventoController/CreateOrEdit/{idEvento}/{idSubevento?}
        public ActionResult CreateOrEdit(uint idEvento, uint? idSubevento)
        {
            SubeventoModel subeventoModel;
            if (idSubevento.HasValue)
            {
                var subevento = _subeventoService.Get(idSubevento.Value);
                if (subevento == null)
                {
                    return NotFound();
                }
                subeventoModel = _mapper.Map<SubeventoModel>(subevento);
            }
            else
            {
                subeventoModel = new SubeventoModel();
            }

            var tipoEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
            var evento = _eventoService.GetEventoSimpleDto(idEvento);
    
            var viewModel = new SubeventoCreateModel()
            {
                Subevento = subeventoModel,
                Evento = evento,
                TiposEventos = new SelectList(tipoEventos, "Id", "Nome"),
            };

            return View(viewModel);
        }

        // POST: SubeventoController/CreateOrEdit/{idEvento}/{idSubevento?}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrEdit(uint idEvento, uint? idSubevento, SubeventoCreateModel subeventoModel)
        {
            var subevento = _mapper.Map<Subevento>(subeventoModel.Subevento);

            if (idSubevento.HasValue)
            {
                subevento.IdEvento = idEvento;
                subevento.Id = idSubevento.Value;
                _subeventoService.Edit(subevento);
            }
            else
            {
                subevento.IdEvento = idEvento;
                _subeventoService.Create(subevento);
            }

            return RedirectToAction("index");
        }

        // GET: SubeventoController/Delete/5
        public ActionResult Delete(uint id)
        {
            
            var subevento = _subeventoService.Get(id);
            var subeventoModel = _mapper.Map<SubeventoModel>(subevento);

            string nomeEvento = _eventoService.GetNomeById(subevento.IdEvento);
            subeventoModel.NomeEvento = nomeEvento;

            string nomeTipoEvento = _tipoEventoService.GetNomeById(subevento.IdTipoEvento);
            subeventoModel.NomeTipoEvento = nomeTipoEvento; ;

            return View(subeventoModel);
        }
        // POST: SubeventoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, SubeventoModel subeventoModel)
        {
            
            _subeventoService.Delete(id);
            return RedirectToAction(nameof(Index));
        }

        //GET: SubeventoController/RegistrarFrequencia
        public IActionResult RegistrarFrequencia(uint idSubEvento, uint idPessoa, uint idEvento)
        {
            var Inscricao = _inscricaoService.GetInscricaoBySubEvento(idSubEvento, idPessoa);
            var messageError = "O evento ainda não foi finalizado!";
            var evento = _eventoService.Get(idEvento);
            ViewBag.Message = messageError;
            if (Inscricao != null && evento.Status == "F")
            {
                return View(Inscricao);
            }
            else
            {
                return Content("<script>alert('O evento ainda não foi finalizado!');</script>", "text/javascript");
            }
        }


        //POST: SubeventoController/RegistrarFrequencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarFrequencia(uint idEvento, InscricaoPessoaSubEventoModel inscricaopessoasubeventomodel,decimal frequencia)
        {
            var subevento = _subeventoService.Get(idEvento);
            if (subevento.Status == "F")
            {
                var inscricao = _mapper.Map<Inscricaopessoasubevento>(inscricaopessoasubeventomodel);
                _inscricaoService.RegistrarFrequenciaSubevento(inscricao,frequencia);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
