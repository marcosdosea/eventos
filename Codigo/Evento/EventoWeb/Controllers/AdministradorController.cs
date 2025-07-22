using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers;

[Route("[controller]")]
[Authorize(Roles = "ADMINISTRADOR")]
public class AdministradorController : Controller
{
    private readonly IAdministradorService _administradorService;
    private readonly IMapper _mapper;

    public AdministradorController(IAdministradorService administradorService, IMapper mapper)
    {
        _administradorService = administradorService;
        _mapper = mapper;
    }
    // GET: AdministradorController/Create
    [HttpGet]
    [Route("")]
    [Route("Index")]
    [Route("Create")]
    public async Task<ActionResult> Create()
    {
        var administradores = await _administradorService.GetAdministradoresAsync();
        var administradorModel = new AdministradorModel
        {
            Administradores = administradores
        };
        return View(administradorModel);
    }

    // POST: AdministradorController/Create
    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AdministradorModel administradorModel)
    {
        if (ModelState.IsValid)
        {
            var administrador = administradorModel.Administrador;
            var pessoa = _mapper.Map<Pessoa>(administrador);
            await _administradorService.CreateAsync(pessoa);
            administradorModel.Administradores = await _administradorService.GetAdministradoresAsync();
            return View(administradorModel);
        }
        administradorModel.Administradores = await _administradorService.GetAdministradoresAsync();
        return View(administradorModel);
    }

    // POST: AdministradorController/Delete/5
    [HttpGet]
    [Route("Delete/{cpf}")]
    public async Task<ActionResult> Delete(string cpf)
    {
        try
        {
            await _administradorService.DeleteAsync(cpf);
        }
        catch (Exception ex)
        {
           
            TempData["ErrorMessage"] = ex.Message;
        }
        var administradorModel = new AdministradorModel();
        administradorModel.Administradores = await _administradorService.GetAdministradoresAsync();
        return RedirectToAction("Create", new { administradorModel});
    }


}