using AutoMapper;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    public class TipoInscricaoController : Controller
    {
        private readonly ITipoInscricaoService tipoInscricaoService;
        private readonly IMapper mapper;

        public TipoInscricaoController(ITipoInscricaoService tipoInscricaoService, IMapper mapper)
        {
            this.tipoInscricaoService = tipoInscricaoService;
            this.mapper = mapper;
        }


        // GET: TipoInscricaoController
        public ActionResult Index()
        {
            var listaTipoInscricao = tipoInscricaoService.GetAll();
            var listaTipoInscricaoModel = mapper.Map<List<TipoInscricaoModel>>(listaTipoInscricao);
            return View(listaTipoInscricaoModel);
        }

        // GET: TipoInscricaoController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TipoInscricaoController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TipoInscricaoController/Create
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

        // GET: TipoInscricaoController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: TipoInscricaoController/Edit/5
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

        // GET: TipoInscricaoController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: TipoInscricaoController/Delete/5
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
    }
}
