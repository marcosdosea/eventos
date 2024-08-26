using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers;

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
    public async Task<ActionResult> Create()
    {
        var administradores = await _administradorService.GetAdministradoresAsync();
        var administradorModel = new AdministradorModel
        {
            administradores = administradores
        };
        return View(administradorModel);
    }

// POST: AdministradorController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AdministradorModel administradorModel)
    {
        if (ModelState.IsValid)
        {
            var administrador = administradorModel.administrador;
            var pessoa = _mapper.Map<Pessoa>(administrador);
            await _administradorService.CreateAsync(pessoa);
        }

        return View(administradorModel);
    }

}