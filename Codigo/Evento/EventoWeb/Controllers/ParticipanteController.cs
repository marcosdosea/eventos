using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers;

public class ParticipanteController : Controller
{
    private readonly IParticipanteService _participanteService;
    private readonly IMapper _mapper;

    public ParticipanteController(IParticipanteService participanteService, IMapper mapper)
    {
        _participanteService = participanteService;
        _mapper = mapper;
    }
    // GET: AdministradorController/Create
    public async Task<ActionResult> Create()
    {
        var participantes = await _participanteService.GetParticipantesAsync();
        var participanteModel = new ParticipanteModel
        {
            Participantes = participantes
        };
        return View(participanteModel);
    }

    // POST: AdministradorController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(ParticipanteModel participanteModel)
    {
        if (ModelState.IsValid)
        {
            var participante = participanteModel.Participante;
            var pessoa = _mapper.Map<Pessoa>(participante);
            await _participanteService.CreateAsync(pessoa);
            participanteModel.Participantes = await _participanteService.GetParticipantesAsync();
            return View(participanteModel);
        }
        participanteModel.Participantes = await _participanteService.GetParticipantesAsync();
        return View(participanteModel);
    }

    // POST: AdministradorController/Delete/5
    public async Task<ActionResult> Delete(string cpf)
    {
        try
        {
            await _participanteService.DeleteAsync(cpf);
        }
        catch (Exception ex)
        {

            TempData["ErrorMessage"] = ex.Message;
        }
        var participanteModel = new ParticipanteModel();
        participanteModel.Participantes = await _participanteService.GetParticipantesAsync();
        return RedirectToAction("Create", new { participanteModel });
    }


}