using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Mappers;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public class AreaInteresseController : Controller
    {
        private readonly IAreaInteresseService _areaInteresseService;
        public AreaInteresseController(IAreaInteresseService areaInteresseService)
        {
            _areaInteresseService = areaInteresseService;
        }

        // GET: AreainteresseController
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            var listaAreainteresse = _areaInteresseService.GetAll();
            var listaAreainteresseModel = AreaInteresseMapper.ToViewModelList(listaAreainteresse);
            return View(listaAreainteresseModel);
        }

        // GET: AreainteresseController/Details/5
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(uint id)
        {
            Areainteresse areainteresse = _areaInteresseService.Get(id);
            if (areainteresse == null)
            {
                return NotFound();
            }
            AreaInteresseModel areaInteresseModel = AreaInteresseMapper.ToViewModel(areainteresse);
            return View(areaInteresseModel);
        }

        // GET: AreainteresseController/Create
        [HttpGet]
        [Route("Create")]
        public ActionResult Create()
        {
            var areaInteresseModel = new AreaInteresseModel();
            return View(areaInteresseModel);
        }

        // POST: AreainteresseController/Create
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AreaInteresseModel areaInteresseModel)
        {
            if (ModelState.IsValid)
            {
                var areainteresse = AreaInteresseMapper.ToEntity(areaInteresseModel);
                _areaInteresseService.Create(areainteresse);
                return RedirectToAction(nameof(Index));
            }
            return View(areaInteresseModel);
        }

        // GET: AreainteresseController/Edit/5
        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(uint id)
        {
            Areainteresse areainteresse = _areaInteresseService.Get(id);
            if (areainteresse == null)
            {
                return NotFound();
            }
            AreaInteresseModel areaInteresseModel = AreaInteresseMapper.ToViewModel(areainteresse);
            return View(areaInteresseModel);
        }

        // POST: AreainteresseController/Edit/5
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, AreaInteresseModel areaInteresseModel)
        {
            if (ModelState.IsValid)
            {
                var areainteresse = AreaInteresseMapper.ToEntity(areaInteresseModel);
                _areaInteresseService.Edit(areainteresse);
                return RedirectToAction(nameof(Index));
            }
            return View(areaInteresseModel);
        }

        // GET: AreainteresseController/Delete/5
        [HttpGet]
        [Route("Delete/{id}")]
        public ActionResult Delete(uint id)
        {
            Areainteresse areainteresse = _areaInteresseService.Get(id);
            if (areainteresse == null)
            {
                return NotFound();
            }
            AreaInteresseModel areaInteresseModel = AreaInteresseMapper.ToViewModel(areainteresse);
            return View(areaInteresseModel);
        }

        // POST: AreainteresseController/Delete/5
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, AreaInteresseModel areaInteresseModel)
        {
            _areaInteresseService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}


