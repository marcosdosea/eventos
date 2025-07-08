using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    public class ColaboradorController : Controller
    {
        private readonly IColaboradorService _colaboradorService;
        private readonly IMapper _mapper;

        public ColaboradorController(IColaboradorService colaboradorService, IMapper mapper)
        {
            _colaboradorService = colaboradorService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<ActionResult> Index()
        {
            try
            {
                var colaboradores = await _colaboradorService.GetColaboradoresAsync();
                var colaboradorModel = new ColaboradorModel
                {
                    Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores)
                };
                return View(colaboradorModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao carregar colaboradores: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                return View(new ColaboradorModel());
            }
        }

        [HttpGet]
        [Route("Create")]
        public async Task<ActionResult> Create()
        {
            try
            {
                var colaboradores = await _colaboradorService.GetColaboradoresAsync();
                var colaboradorModel = new ColaboradorModel
                {
                    Colaborador = new PessoaModel(),
                    Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(colaboradores)
                };
                return View(colaboradorModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao carregar formulário: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ColaboradorModel colaboradorModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var pessoa = _mapper.Map<Pessoa>(colaboradorModel.Colaborador);
                    System.Diagnostics.Debug.WriteLine($"Criando colaborador: {pessoa.Nome}, CPF: {pessoa.Cpf}");
                    await _colaboradorService.CreateAsync(pessoa);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Erro ao criar colaborador: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                }
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine(error.ErrorMessage);
                }
            }
            colaboradorModel.Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync());
            return View(colaboradorModel);
        }

        [HttpGet]
        [Route("Edit/{cpf}")]
        public async Task<ActionResult> Edit(string cpf)
        {
            try
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao carregar colaborador: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Route("Edit/{cpf}")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string cpf, ColaboradorModel colaboradorModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var pessoa = _mapper.Map<Pessoa>(colaboradorModel.Colaborador);
                    await _colaboradorService.UpdateAsync(pessoa);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Erro ao atualizar colaborador: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                }
            }
            colaboradorModel.Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync());
            return View(colaboradorModel);
        }

        [HttpGet]
        [Route("Details/{cpf}")]
        public async Task<ActionResult> Details(string cpf)
        {
            try
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao carregar detalhes: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Route("Delete/{cpf}")]
        public async Task<ActionResult> Delete(string cpf)
        {
            try
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao carregar colaborador: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Route("Delete/{cpf}")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string cpf, ColaboradorModel colaboradorModel)
        {
            try
            {
                await _colaboradorService.DeleteAsync(cpf);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao excluir colaborador: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Erro: {ex.Message}");
                colaboradorModel.Colaboradores = _mapper.Map<IEnumerable<ColaboradorDTO>>(await _colaboradorService.GetColaboradoresAsync());
                return View(colaboradorModel);
            }
        }
    }
}