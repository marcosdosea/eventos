using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace EventoWeb.Controllers;

public class ColaboradorController : Controller
{
    private readonly IColaboradorService _colaboradorService;
    private readonly IMapper _mapper;

    public ColaboradorController(IColaboradorService colaboradorService, IMapper mapper)
    {
        _colaboradorService = colaboradorService;
        _mapper = mapper;
    }

    // GET: ColaboradorController/Create
    public async Task<ActionResult> Create()
    {
        var colaboradores = await _colaboradorService.GetColaboradoresAsync();
        var colaboradorModel = new ColaboradorModel
        {
            Colaboradores = colaboradores
        };
        return View(colaboradorModel);
    }

    // POST: ColaboradorController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(ColaboradorModel colaboradorModel)
    {
        if (ModelState.IsValid)
        {
            var colaborador = colaboradorModel.Colaborador;
            var pessoa = _mapper.Map<Pessoa>(colaborador);
            await _colaboradorService.CreateAsync(pessoa);
            colaboradorModel.Colaboradores = await _colaboradorService.GetColaboradoresAsync();
            return View(colaboradorModel);
        }
        colaboradorModel.Colaboradores = await _colaboradorService.GetColaboradoresAsync();
        return View(colaboradorModel);
    }

    // POST: ColaboradorController/Delete/5
    public async Task<ActionResult> Delete(string cpf)
    {
        try
        {
            await _colaboradorService.DeleteAsync(cpf);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        var colaboradorModel = new ColaboradorModel();
        colaboradorModel.Colaboradores = await _colaboradorService.GetColaboradoresAsync();
        return RedirectToAction("Create", new { colaboradorModel });
    }
}