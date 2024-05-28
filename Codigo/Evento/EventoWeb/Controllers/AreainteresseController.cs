using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace EventoWeb.Controllers
{
    public class AreainteresseController : Controller
    {
        private readonly IAreaInteresseService _areainteresseservice;
        private readonly IMapper _mapper;
        public AreainteresseController(IAreaInteresseService areainteresseservice, IMapper mapper)
        {
            _areainteresseservice = areainteresseservice;
            _mapper = mapper;
        }
        public ActionResult Index()
        {
            var areainteresse = _areainteresseservice.GetAll();
            var areainteresseModel = _mapper.Map<List<AreainteresseModel>>(areainteresse);
            return View(areainteresseModel);
        }
        public ActionResult Details(uint id)
        {
            Areainteresse areainteresse = _areainteresseservice.Get(id);
            AreainteresseModel areainteressemodel = _mapper.Map<AreainteresseModel>(areainteresse);
            return View(areainteressemodel);
        }
        public ActionResult Create()
        {
            var areainteresseModel = new AreainteresseModel();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AreainteresseModel areainteresseModel)
        {
            if (ModelState.IsValid)
            {
                var areainteresse = _mapper.Map<Areainteresse>(areainteresseModel);
                _areainteresseservice.Create(areainteresse);
            }
            return RedirectToAction(nameof(Index));
        }
        public ActionResult Edit(uint id)
        {
            return Details(id);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, AreainteresseModel areainteresseModel)
        {
            if (ModelState.IsValid)
            {
                var areainteresse = _mapper.Map<Areainteresse>(areainteresseModel);
                _areainteresseservice.Edit(areainteresse);
            }
            return RedirectToAction(nameof(Index));
        }
        public ActionResult Delete(uint id)
        {
            var areainteresse = _areainteresseservice.Get(id);
            var areainteresseModel = _mapper.Map<AreainteresseModel>(areainteresse);
            return View(areainteresseModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(uint id, AreainteresseModel areainteresseModel)
        {
            _areainteresseservice.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
