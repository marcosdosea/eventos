using AutoMapper;
using EventoWeb.Models;
using Core;
using Core.Service;
using Microsoft.AspNetCore.Mvc;


namespace EventoWeb.Controllers
{
    public class SubeventoController : Controller
    {
        private readonly ISubeventoService _subeventoService;
        private readonly IMapper _mapper;
        public SubeventoController(ISubeventoService subeventoService, IMapper mapper)
        {
            _subeventoService = subeventoService;
            _mapper = mapper;
        }
        // GET: SubeventoController
        public ActionResult Index()
        {
            var listaSubeventos = _subeventoService.GetAll();
            var listaSubeventosModel = _mapper.Map<List<SubeventoModel>>(listaSubeventos);
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
            return View(subeventoModel);
        }
        // POST: SubeventoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SubeventoModel subeventoModel)
        {
            if (ModelState.IsValid)
            {
                var subevento = _mapper.Map<Subevento>(subeventoModel);
                _subeventoService.Create(subevento);
            }
            return RedirectToAction(nameof(Index));
        }
        // GET: SubeventoController/Edit/5
        public ActionResult Edit(uint id)
        {
            return Details(id);
        }
        // POST: SubeventoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, SubeventoModel subeventoModel)
        {
            if (ModelState.IsValid)
            {
                var subevento = _mapper.Map<Subevento>(subeventoModel);
                _subeventoService.Edit(subevento);
            }
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
