using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Service;

namespace EventoWeb.Controllers
{
    [Authorize(Roles = "GESTOR")]
    public class TipoInscricaoController : Controller
    {
        private readonly ITipoInscricaoService _tipoInscricaoService;
        private readonly IEventoService _eventoService;
        private readonly ISubeventoService _subeventoService;
        private readonly IMapper _mapper;

        public TipoInscricaoController(ITipoInscricaoService tipoInscricaoService, IMapper mapper,IEventoService eventoService,ISubeventoService subeventoService)
        {
            this._tipoInscricaoService = tipoInscricaoService;
            this._eventoService = eventoService;
            this._subeventoService = subeventoService;
            this._mapper = mapper;
        }


        // GET: TipoInscricaoController
        public ActionResult Index(uint? idEvento)
        {
            if (idEvento.HasValue)
            {
                var listaTipoInscricao = _tipoInscricaoService.GetByEvento(idEvento.Value).ToList();
                var listaTipoInscricaoModel = listaTipoInscricao.Select(e => new TipoInscricaoModel
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

                ViewData["EventoId"] = idEvento.Value;
                ViewData["EventoNome"] = _eventoService.GetNomeById(idEvento.Value);

                return View(listaTipoInscricaoModel);
            }
            else
            {
                return RedirectToAction("Index", "Evento"); 
            }
        }




        // GET: TipoInscricaoController/Details/5
        public ActionResult Details(uint id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
            TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            return View(tipoInscricaoModel);
        }

        // GET: TipoInscricaoController/Create
        public ActionResult Create(uint idEvento)
        {
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new TipoInscricaoModel();
            viewModel.IdEvento = idEvento;
            viewModel.Evento = new SelectList(eventos, "Id", "Nome");

            return View(viewModel);
        }



        // POST: TipoInscricaoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TipoInscricaoModel tipoInscricaoModel)
        {
            ModelState.Remove("Evento");
            if (ModelState.IsValid)
            {
                var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel);
                _tipoInscricaoService.Create(tipoinscricao);
                return RedirectToAction(nameof(Index), new { idEvento = tipoInscricaoModel.IdEvento });
            }

            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            tipoInscricaoModel.Evento = new SelectList(eventos, "Id", "Nome");
            return View(tipoInscricaoModel);
        }




        // GET: TipoInscricaoController/Edit/5
        public ActionResult Edit(uint id)
        {
            var tipoinscricao = _tipoInscricaoService.Get(id);
            if (tipoinscricao == null)
            {
                return NotFound();
            }

            var tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);
            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            var viewModel = tipoInscricaoModel;
            viewModel.Evento = new SelectList(eventos, "Id", "Nome");
           
            return View(viewModel);
        }

        // POST: TipoInscricaoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, TipoInscricaoModel tipoInscricaoModel)
        {
            ModelState.Remove("Evento");
            if (ModelState.IsValid)
            {
                var tipoinscricao = _mapper.Map<Tipoinscricao>(tipoInscricaoModel);
                tipoinscricao.Id = id;
                _tipoInscricaoService.Edit(tipoinscricao);

                return RedirectToAction(nameof(Index), new { idEvento = tipoInscricaoModel.IdEvento });
            }

            var eventos = _eventoService.GetAll().OrderBy(e => e.Nome);
            tipoInscricaoModel.Evento = new SelectList(eventos, "Id", "Nome");
            return View(tipoInscricaoModel);
        }



        // GET: TipoInscricaoController/Delete/5
        public ActionResult Delete(uint id)
        {
            Tipoinscricao tipoinscricao = _tipoInscricaoService.Get(id);
            TipoInscricaoModel tipoInscricaoModel = _mapper.Map<TipoInscricaoModel>(tipoinscricao);

            string nomeEvento = _eventoService.GetNomeById(tipoinscricao.IdEvento);
            tipoInscricaoModel.NomeEvento = nomeEvento;

            return View(tipoInscricaoModel);
        }

        // POST: TipoInscricaoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, uint idEvento)
        {
            _tipoInscricaoService.Delete(id);
            return RedirectToAction(nameof(Index), new { idEvento = idEvento });
        }
        
        // GET: TipoInscricaoController/CreateTipoInscricaoSubevento
        public ActionResult CreateTipoInscricaoSubevento(uint idSubevento)
        {
            var subevento = _subeventoService.Get(idSubevento);
            var subeventoModel = _mapper.Map<SubeventoModel>(subevento);
            var tiposInscricaos = _tipoInscricaoService.GetByEventoUsadaSubevento(subevento.IdEvento);
            var tiposInscricaosSubevento = _tipoInscricaoService.GetTiposInscricaosSubevento(idSubevento);
            ViewData["EventoId"] = subevento.IdEvento;
            var view = new TipoInscricaoSubeventoModel()
            {
                Subevento = subeventoModel,
                TiposInscricaos = new SelectList(tiposInscricaos, "Id", "Nome"),
                TiposInscricaosSubevento = tiposInscricaosSubevento
            };
            return View(view);    
        }
        // POST: TipoInscricaoController/CreateTipoInscricaoSubevento   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTipoInscricaoSubevento(uint idSubevento, uint IdTipoInscricao)
        {
            _tipoInscricaoService.AssociacaoTipoInscricaoSubevento(idSubevento, IdTipoInscricao);
            return RedirectToAction("CreateTipoInscricaoSubevento", new { idSubevento });
        }
        
        
        // POST: TipoInscricaoController/DeleteTipoInscricaoSubevento
        public IActionResult DeleteTipoInscricaoSubevento(uint idSubevento,uint IdTipoInscricao)
        {
            _tipoInscricaoService.DeleteTipoInscricaoSubevento(idSubevento, IdTipoInscricao);
            var subevento = _subeventoService.Get(idSubevento);
            return RedirectToAction("CreateTipoInscricaoSubevento", new { idSubevento }); 
        }
    }
}
