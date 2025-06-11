using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    public class ParticipanteController : Controller
    {
        private readonly IParticipanteService _participanteService;
        private readonly IMapper _mapper;

        public ParticipanteController(IParticipanteService participanteService, IMapper mapper)
        {
            _participanteService = participanteService;
            _mapper = mapper;
        }

        public async Task<ActionResult> Index()
        {
            var participantes = await _participanteService.GetParticipantesAsync();
            var participanteModel = new ParticipanteModel
            {
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View(participanteModel);
        }

        public async Task<ActionResult> Create()
        {
            var participantes = await _participanteService.GetParticipantesAsync();
            var participanteModel = new ParticipanteModel
            {
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View(participanteModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ParticipanteModel participanteModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = _mapper.Map<Pessoa>(participanteModel.Participante);
                await _participanteService.CreateAsync(pessoa);
                participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
                return View(participanteModel);
            }
            participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
            return View(participanteModel);
        }

        public async Task<ActionResult> Edit(string cpf)
        {
            var participantes = await _participanteService.GetParticipantesAsync();
            var participante = participantes.FirstOrDefault(c => c.Cpf == cpf);
            if (participante == null)
            {
                return NotFound();
            }
            var participanteModel = new ParticipanteModel
            {
                Participante = _mapper.Map<PessoaModel>(participante),
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View(participanteModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string cpf, ParticipanteModel participanteModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = _mapper.Map<Pessoa>(participanteModel.Participante);
                await _participanteService.CreateAsync(pessoa);
                participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
                return View(participanteModel);
            }
            participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
            return View(participanteModel);
        }

        public async Task<ActionResult> Details(string cpf)
        {
            var participantes = await _participanteService.GetParticipantesAsync();
            var participante = participantes.FirstOrDefault(c => c.Cpf == cpf);
            if (participante == null)
            {
                return NotFound();
            }
            var participanteModel = new ParticipanteModel
            {
                Participante = _mapper.Map<PessoaModel>(participante),
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View(participanteModel);
        }

        public async Task<ActionResult> Delete(string cpf)
        {
            try
            {
                TempData["ErrorMessage"] = "Exclusão não permitida para participantes.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            var participanteModel = new ParticipanteModel
            {
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync())
            };
            return RedirectToAction("Create", participanteModel);
        }
    }
}