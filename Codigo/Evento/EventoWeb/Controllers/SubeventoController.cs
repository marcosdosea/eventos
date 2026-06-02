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
    [Authorize(Roles = "GESTOR")]
    public class SubeventoController : Controller
    {
        private readonly ISubeventoService _subeventoService;
        private readonly IEventoService _eventoService;
        private readonly ITipoeventoService _tipoEventoService;
        private readonly ITipoInscricaoService _tipoInscricaoService;
        private readonly IMapper _mapper;
        public SubeventoController(ISubeventoService subeventoService, IMapper mapper, IEventoService eventoService, ITipoeventoService tipoeventoService, ITipoInscricaoService tipoInscricaoService)
        {
            _subeventoService = subeventoService;
            _eventoService = eventoService;
            _tipoEventoService = tipoeventoService;
            _mapper = mapper;
            _tipoInscricaoService = tipoInscricaoService;
        }

        // GET: SubeventoController
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            string userCpf = null;
            uint idPapel = 0;

            if (User?.Identity?.IsAuthenticated == true)
            {
                userCpf = User.FindFirstValue(ClaimTypes.Name);

                if (User.IsInRole("GESTOR"))
                {
                    idPapel = 2;
                }
                else if (User.IsInRole("COLABORADOR"))
                {
                    idPapel = 3;
                }
            }

            List<Subevento> listaSubeventos = new List<Subevento>();

            var todosSubeventos = _subeventoService.GetAll();
            if (todosSubeventos != null)
            {
                var eventosDoUsuario = _eventoService.GetEventByCpf(userCpf, idPapel);

                var idsEventosDoUsuario = eventosDoUsuario != null
                    ? eventosDoUsuario.Select(ev => ev.Id).ToList()
                    : new List<uint>();

                listaSubeventos = todosSubeventos
                    .Where(s => idsEventosDoUsuario.Contains(s.IdEvento))
                    .ToList();

            }

            var todosEventos = _eventoService.GetAll();
            var dicionarioEventos = todosEventos != null
                ? todosEventos.ToDictionary(e => e.Id, e => e.Nome)
                : new Dictionary<uint, string>();

            var todosTipos = _tipoEventoService.GetAll();
            var dicionarioTipos = todosTipos != null
                ? todosTipos.ToDictionary(t => t.Id, t => t.Nome)
                : new Dictionary<uint, string>();

            var listaSubeventosModel = listaSubeventos.Select(e =>
            {
                string nomeEvento = dicionarioEventos.TryGetValue(e.IdEvento, out var nomeE) ? nomeE : "Evento não encontrado";

                string nomeTipoEvento = dicionarioTipos.TryGetValue(e.IdTipoEvento, out var nomeT) ? nomeT : "Tipo não encontrado";

                return new SubeventoModel
                {
                    Id = e.Id,
                    Nome = e.Nome,
                    IdEvento = e.IdEvento,
                    NomeEvento = nomeEvento,
                    DataInicio = e.DataInicio,
                    Status = e.Status,
                    IdTipoEvento = e.IdTipoEvento,
                    NomeTipoEvento = nomeTipoEvento
                };
            }).ToList();

            return View(listaSubeventosModel);
        }

        // GET: SubeventoController/Details/5
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(uint id)
        {
            Subevento subevento = _subeventoService.Get(id);
            SubeventoModel subeventoModel = _mapper.Map<SubeventoModel>(subevento);
            return View(subeventoModel);
        }

        // GET: SubeventoController/CreateOrEdit/{idEvento}/{idSubevento?}
        [HttpGet]
        [Route("CreateOrEdit/{idEvento}/{idSubevento?}")]
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

            var viewModel = subeventoModel;

            viewModel.Evento = evento;
            viewModel.TiposEventos = new SelectList(tipoEventos, "Id", "Nome");

            return View(viewModel);
        }

        // POST: SubeventoController/CreateOrEdit/{idEvento}/{idSubevento?}
        [HttpPost]
        [Route("CreateOrEdit/{idEvento}/{idSubevento?}")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrEdit(uint idEvento, SubeventoModel subeventoModel)
        {
            ModelState.Remove("TiposEventos");
            ModelState.Remove("Evento.Nome");
            if (ModelState.IsValid)
            {
                var subevento = _mapper.Map<Subevento>(subeventoModel);

                var idSubevento = (uint?)subevento.Id;

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

                return RedirectToAction("GerenciarEvento", "Evento", new { idEvento = idEvento });
            }

            var tipoEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
            var evento = _eventoService.GetEventoSimpleDto(idEvento);
            subeventoModel.Evento = evento;
            subeventoModel.TiposEventos = new SelectList(tipoEventos, "Id", "Nome");
            return View(subeventoModel);
        }

        // GET: SubeventoController/Delete/5
        [HttpGet]
        [Route("Delete/{id}")]
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
        [Route("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, SubeventoModel subeventoModel)
        {
            var tiposInscricao = _tipoInscricaoService.GetTiposInscricaosSubevento(id);

            if (tiposInscricao.Any())
            {
                foreach (var tipoInscricao in tiposInscricao)
                {
                    _tipoInscricaoService.DeleteTipoInscricaoSubevento(id, tipoInscricao.Id);
                }
            }
            _subeventoService.Delete(id);
            return RedirectToAction(nameof(Index));
        }

    }
}
