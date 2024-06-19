using AutoMapper;
using EventoWeb.Models;
using Core;
using Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace EventoWeb.Controllers
{
    public class SubeventoController : Controller
    {
        private readonly ISubeventoService _subeventoService;
        private readonly IEventoService _eventoService;
        private readonly ITipoeventoService _tipoEventoService;
        private readonly IMapper _mapper;
        public SubeventoController(ISubeventoService subeventoService, IMapper mapper, IEventoService eventoService, ITipoeventoService tipoeventoService)
        {
            _subeventoService = subeventoService;
            _eventoService = eventoService;
            _tipoEventoService = tipoeventoService;
            _mapper = mapper;
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
        // GET: SubeventoController/Create
        public ActionResult Create()
        {
            var subeventoModel = new SubeventoModel();
            var tipoEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new SubeventocreateModel
            {
                Subevento = subeventoModel,
                Eventos = new SelectList(eventos,"Id", "Nome"),
                TiposEventos = new SelectList(tipoEventos, "Id", "Nome")
            };
            return View(viewModel);
        }
        // POST: SubeventoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SubeventocreateModel subeventoModel)
        {
            var subevento = _mapper.Map<Subevento>(subeventoModel.Subevento);
            _subeventoService.Create(subevento);
            return RedirectToAction(nameof(Index));
        }
        // GET: SubeventoController/Edit/5
        public ActionResult Edit(uint id)
        {
            var subevento = _subeventoService.Get(id);
            if(subevento == null)
            {
                return NotFound();
            }
            var subeventoModel = _mapper.Map<SubeventoModel>(subevento);
            var tipoEventos = _tipoEventoService.GetAll().OrderBy(t => t.Nome);
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new SubeventocreateModel
            {
                Subevento = subeventoModel,
                Eventos = new SelectList(eventos, "Id", "Nome"),
                TiposEventos = new SelectList(tipoEventos, "Id", "Nome")
            };
            return View(viewModel);
        }
        // POST: SubeventoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, SubeventocreateModel subeventoModel)
        {
            var subevento = _mapper.Map<Subevento>(subeventoModel.Subevento);
            subevento.Id = id;
            _subeventoService.Edit(subevento);
            return RedirectToAction(nameof(Index));
        }
        // GET: SubeventoController/Delete/5
        public ActionResult Delete(uint id)
        {
            
            var subevento = _subeventoService.Get(id);
            var subeventoModel = _mapper.Map<SubeventoModel>(subevento);
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
    }
}
