using AutoMapper;
using Core;
using Core.Service;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace EventoWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR")]
    public class AreaInteresseController : Controller
    {
        private readonly IAreaInteresseService _areaInteresseService;
        private readonly IMapper _mapper;

        public AreaInteresseController(IAreaInteresseService areaInteresseService, IMapper mapper)
        {
            _areaInteresseService = areaInteresseService;
            _mapper = mapper;
        }

        // GET: AreainteresseController
        public ActionResult Index()
        {
            var listaAreainteresse = _areaInteresseService.GetAll();
            var listaAreainteresseModel = _mapper.Map<List<AreaInteresseModel>>(listaAreainteresse);
            return View(listaAreainteresseModel);
        }

        // GET: AreainteresseController/Details/5
        public ActionResult Details(uint id)
        {
            Areainteresse areainteresse = _areaInteresseService.Get(id);
            if (areainteresse == null)
            {
                return NotFound();
            }
            AreaInteresseModel areaInteresseModel = _mapper.Map<AreaInteresseModel>(areainteresse);
            return View(areaInteresseModel);
        }

        // GET: AreainteresseController/Create
        public ActionResult Create()
        {
            var areaInteresseModel = new AreaInteresseModel();
            return View(areaInteresseModel);
        }

        // POST: AreainteresseController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AreaInteresseModel areaInteresseModel)
        {
            if (ModelState.IsValid)
            {
                var areainteresse = _mapper.Map<Areainteresse>(areaInteresseModel);
                _areaInteresseService.Create(areainteresse);
                return RedirectToAction(nameof(Index));
            }
            return View(areaInteresseModel);
        }

        // GET: AreainteresseController/Edit/5
        public ActionResult Edit(uint id)
        {
            Areainteresse areainteresse = _areaInteresseService.Get(id);
            if (areainteresse == null)
            {
                return NotFound();
            }
            AreaInteresseModel areaInteresseModel = _mapper.Map<AreaInteresseModel>(areainteresse);
            return View(areaInteresseModel);
        }

        // POST: AreainteresseController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, AreaInteresseModel areaInteresseModel)
        {
            if (ModelState.IsValid)
            {
                var areainteresse = _mapper.Map<Areainteresse>(areaInteresseModel);
                _areaInteresseService.Edit(areainteresse);
                return RedirectToAction(nameof(Index));
            }
            return View(areaInteresseModel);
        }

        // GET: AreainteresseController/Delete/5
        public ActionResult Delete(uint id)
        {
            Areainteresse areainteresse = _areaInteresseService.Get(id);
            if (areainteresse == null)
            {
                return NotFound();
            }
            AreaInteresseModel areaInteresseModel = _mapper.Map<AreaInteresseModel>(areainteresse);
            return View(areaInteresseModel);
        }

        // POST: AreainteresseController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, AreaInteresseModel areaInteresseModel)
        {
            _areaInteresseService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}


