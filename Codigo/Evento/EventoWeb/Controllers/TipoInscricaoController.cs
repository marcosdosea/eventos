using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    public class TipoInscricaoController : Controller
    {
        private readonly ITipoInscricaoService _tipoInscricaoService;
        private readonly IEventoService _eventoService;
        private readonly IMapper _mapper;

        public TipoInscricaoController(ITipoInscricaoService tipoInscricaoService, IMapper mapper,IEventoService eventoService)
        {
            this._tipoInscricaoService = tipoInscricaoService;
            this._eventoService = eventoService;
            this._mapper = mapper;
        }


        // GET: TipoInscricaoController
        public ActionResult Index()
        {
            var listaTipoInscricao = _tipoInscricaoService.GetAll();
            var listaTipoInscricaoModel = _mapper.Map<List<TipoInscricaoModel>>(listaTipoInscricao);
            return View(listaTipoInscricaoModel);
        }

        // GET: TipoInscricaoController/Details/5
        public ActionResult Details(int id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
            TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            return View(tipoInscricaoModel);
        }

        // GET: TipoInscricaoController/Create
        public ActionResult Create()
        {
            var tipoInscricaoModel = new TipoInscricaoModel();
            
            var eventos = _eventoService.GetAll();
            ViewBag.IdEvento = new SelectList(eventos, "Id", "Nome");
            
            return View(tipoInscricaoModel);
        }


        // POST: TipoInscricaoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TipoInscricaoModel tipoInscricaoModel)
        {
            if (ModelState.IsValid)
            {
                var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel);
                _tipoInscricaoService.Create(tipoinscricao);
            }

            return RedirectToAction(nameof(Index));
        }
        
        // GET: TipoInscricaoController/Edit/5
        public ActionResult Edit(int id)
        {
            var tipoinscricao = _tipoInscricaoService.Get(id);
            
            var tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);

            var eventos = _eventoService.GetAll();
            ViewBag.IdEvento = new SelectList(eventos, "Id", "Nome", tipoinscricao.IdEvento);

            return View(tipoInscricaoModel);
        }

        // POST: TipoInscricaoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, TipoInscricaoModel tipoInscricaoModel)
        {
            if (id != tipoInscricaoModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel);
                tipoinscricao.Id = id;
                _tipoInscricaoService.Edit(tipoinscricao);
                return RedirectToAction(nameof(Index));
            }

            var eventos = _eventoService.GetAll();
            ViewBag.IdEvento = new SelectList(eventos, "Id", "Nome", tipoInscricaoModel.IdEvento);

            return View(tipoInscricaoModel);
        }

        // GET: TipoInscricaoController/Delete/5
        public ActionResult Delete(int id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
           TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            return View(tipoInscricaoModel);
        }

        // POST: TipoInscricaoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, TipoInscricaoModel tipoInscricaoModel)
        {
            _tipoInscricaoService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
