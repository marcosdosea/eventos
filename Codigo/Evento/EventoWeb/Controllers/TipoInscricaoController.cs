using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Service;

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
            var listaTipoInscricao = _tipoInscricaoService.GetAll().ToList();
            var listaSubeventosModel = listaTipoInscricao.Select(e => new TipoInscricaoModel
            {
                Id = e.Id,
                Nome = e.Nome,
                IdEvento = e.IdEvento,
                NomeEvento = _eventoService.GetNomeById(e.IdEvento),
                Descricao = e.Descricao,
                Valor = e.Valor,
                DataInicio = e.DataInicio,
                Datafim = e.Datafim,
                UsadaEvento = e.UsadaEvento,
                UsadaSubevento = e.UsadaSubevento 

            }).ToList();
            return View(listaSubeventosModel);
        }

        // GET: TipoInscricaoController/Details/5
        public ActionResult Details(uint id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
            TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            return View(tipoInscricaoModel);
        }

        // GET: TipoInscricaoController/Create
        public ActionResult Create()
        {
            var tipoInscricaoModel = new TipoInscricaoModel();
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new TipoInscricaocreateModel
            {
                TipoInscricao = tipoInscricaoModel,
                Evento = new SelectList(eventos, "Id", "Nome")
            };

            return View(viewModel);
        }


        // POST: TipoInscricaoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TipoInscricaocreateModel tipoInscricaoModel)
        {
            var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel.TipoInscricao);
            _tipoInscricaoService.Create(tipoinscricao);
            return RedirectToAction(nameof(Index));
        }
        
        // GET: TipoInscricaoController/Edit/5
        public ActionResult Edit(uint id)
        {
            
            var tipoinscricao = _tipoInscricaoService.Get(id);
            if(tipoinscricao == null)
            {
                return NotFound();
            }
           
            var tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new TipoInscricaocreateModel
            {
                TipoInscricao = tipoInscricaoModel,
                Evento = new SelectList(eventos, "Id", "Nome")
            };
            return View(viewModel);

        }

        // POST: TipoInscricaoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, TipoInscricaocreateModel tipoInscricaoModel)
        {  
            var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel.TipoInscricao);
            tipoinscricao.Id = id;
            _tipoInscricaoService.Edit(tipoinscricao);
            return RedirectToAction(nameof(Index));
        }

        // GET: TipoInscricaoController/Delete/5
        public ActionResult Delete(uint id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
           TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            return View(tipoInscricaoModel);
        }

        // POST: TipoInscricaoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, TipoInscricaoModel tipoInscricaoModel)
        {
            _tipoInscricaoService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
