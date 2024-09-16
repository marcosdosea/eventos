using AutoMapper;
using Core.Service;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    public class ModeloCertificadoController : Controller
    {
        private readonly IModeloCertificadoService _modeloCertificadoService;
        private readonly IMapper _mapper;
        private readonly IEventoService _eventoService;

        public ModeloCertificadoController(IEventoService eventoService, IModeloCertificadoService modeloCertificadoService, IMapper mapper)
        {
            _modeloCertificadoService = modeloCertificadoService;
            _eventoService = eventoService;
            _mapper = mapper;
        }
        public IActionResult Index()
        {
            var modeloModel = _modeloCertificadoService.GetAll().ToList();
            return View(modeloModel);
        }

     //   [HttpGet]
    //    public IActionResult Create(uint id)
    //    {
    //        var evento = _eventoService.Get(id);
     //       if (ModelState.IsValid) { 
      //          if(evento.Id != null)
      //          {
                 
        //        }
            //}
      //  }

    //    [HttpPost]

    }
}
