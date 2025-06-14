using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    public class ColaboradorController : Controller
    {
        private readonly IColaboradorService _colaboradorService;
        private readonly IMapper _mapper;

        public ColaboradorController(IColaboradorService colaboradorService, IMapper mapper)
        {
            _colaboradorService = colaboradorService;
            _mapper = mapper;
        }

        public async Task<ActionResult> Index()
        {
            var colaboradores = await _colaboradorService.GetColaboradoresAsync();
            var colaboradorModel = new ColaboradorModel
            {
                Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores)
            };
            return View(colaboradorModel);
        }

        public async Task<ActionResult> Create()
        {
            var colaboradores = await _colaboradorService.GetColaboradoresAsync();
            var colaboradorModel = new ColaboradorModel
            {
                Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores)
            };
            return View(colaboradorModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ColaboradorModel colaboradorModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = _mapper.Map<Pessoa>(colaboradorModel.Colaborador);
                await _colaboradorService.CreateAsync(pessoa);
                colaboradorModel.Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync());
                return View(colaboradorModel);
            }
            colaboradorModel.Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync());
            return View(colaboradorModel);
        }

        public async Task<ActionResult> Edit(string cpf)
        {
            var colaboradores = await _colaboradorService.GetColaboradoresAsync();
            var colaborador = colaboradores.FirstOrDefault(c => c.Cpf == cpf);
            if (colaborador == null)
            {
                return NotFound();
            }
            var colaboradorModel = new ColaboradorModel
            {
                Colaborador = _mapper.Map<PessoaModel>(colaborador),
                Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores)
            };
            return View(colaboradorModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string cpf, ColaboradorModel colaboradorModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = _mapper.Map<Pessoa>(colaboradorModel.Colaborador);
                await _colaboradorService.CreateAsync(pessoa);
                colaboradorModel.Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync());
                return View(colaboradorModel);
            }
            colaboradorModel.Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync());
            return View(colaboradorModel);
        }

        public async Task<ActionResult> Details(string cpf)
        {
            var colaboradores = await _colaboradorService.GetColaboradoresAsync();
            var colaborador = colaboradores.FirstOrDefault(c => c.Cpf == cpf);
            if (colaborador == null)
            {
                return NotFound();
            }
            var colaboradorModel = new ColaboradorModel
            {
                Colaborador = _mapper.Map<PessoaModel>(colaborador),
                Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores)
            };
            return View(colaboradorModel);
        }

        public async Task<ActionResult> Delete(string cpf)
        {
            try
            {
                TempData["ErrorMessage"] = "Exclusão não permitida para colaboradores.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            var colaboradorModel = new ColaboradorModel
            {
                Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync())
            };
            return RedirectToAction("Create", colaboradorModel);
        }
    }
}