using AutoMapper;
using Core;
using Core.DTO;
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
            var tipoInscricaoModel = new TipoInscricaoModel
            {
                IdEvento = idEvento
            };
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
                return RedirectToAction(nameof(Index), new { idEvento = tipoInscricaoModel.TipoInscricao.IdEvento });
           
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

            // Redireciona para a tela correta com o idEvento
            return RedirectToAction(nameof(Index), new { idEvento = tipoInscricaoModel.TipoInscricao.IdEvento });
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
        
        // GET: TipoInscricaoSubevento/CreateTipoInscricaoSubevento
        public ActionResult CreateTipoInscricaoSubevento(uint idSubevento)
        {
            var subevento = _subeventoService.Get(idSubevento);
            var tiposInscricaos = _tipoInscricaoService.GetByEventoUsadaSubevento(subevento.IdEvento);
            var tiposInscricaosSubevento = _tipoInscricaoService.GetTiposInscricaosSubevento(idSubevento);
            var view = new TipoInscricaoSubeventoModel()
            {
                Subevento = subevento,
                TiposInscricaos = new SelectList(tiposInscricaos, "Id", "Nome"),
                TiposInscricaosSubevento = tiposInscricaosSubevento
            };
            return View(view);    
        }
        // POST: TipoInscricaoSubevento/CreateTipoInscricaoSubevento   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTipoInscricaoSubevento(uint idSubevento, uint IdTipoInscricao)
        {
            _tipoInscricaoService.AssociacaoTipoInscricaoSubecento(idSubevento, IdTipoInscricao);
            return RedirectToAction("CreateTipoInscricaoSubevento", new { idSubevento });
        }
        
        
        // POST: EventoController/DeletePessoaPapel
        public IActionResult DeleteTipoInscricaoSubevento(uint idSubevento,uint IdTipoInscricao)
        {
            _tipoInscricaoService.DeleteTipoInscricaoSubevento(idSubevento, IdTipoInscricao);

            return RedirectToAction("CreateTipoInscricaoSubevento", new { idSubevento }); 
        }
    }
}
