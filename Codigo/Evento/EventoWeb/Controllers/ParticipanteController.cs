using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    public class ParticipanteController : Controller
    {
        private readonly IParticipanteService _participanteService;
        private readonly IMapper _mapper;

        public ParticipanteController(IParticipanteService participanteService, IMapper mapper)
        {
            _participanteService = participanteService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<ActionResult> Index()
        {
            var participantes = await _participanteService.GetParticipantesAsync();
            var participanteModel = new ParticipanteModel
            {
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View(participanteModel);
        }
        
        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("Create")]
        public async Task<ActionResult> Create()
        {
            var participantes = await _participanteService.GetParticipantesAsync();
            var participanteModel = new ParticipanteModel
            {
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View(participanteModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ParticipanteModel participanteModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = _mapper.Map<Pessoa>(participanteModel.Participante);
                await _participanteService.CreateAsync(pessoa);
                participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
                return RedirectToAction("Index");
            }
            participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
            return View(participanteModel);
        }

        [HttpGet]
        [Route("Edit/{cpf}")]
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
        [Route("Edit/{cpf}")]
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

        [HttpGet]
        [Route("Details/{cpf}")]
        public async Task<ActionResult> Details(string cpf)
        {
         
            var participante = await _participanteService.GetParticipanteByCpfAsync(cpf);
            if (participante == null)
            {
                return NotFound();
            }
            var participantes = await _participanteService.GetParticipantesAsync();
            var participanteModel = new ParticipanteModel
            {
                Participante = _mapper.Map<PessoaModel>(participante),
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View(participanteModel);
        }

        // No ParticipanteController.cs

        [HttpGet]
        [Route("ConfirmDelete/{cpf}")] // Nova rota para a página de confirmação
        public async Task<ActionResult> ConfirmDelete(string cpf) // Novo nome para o método GET
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
                // Pode remover a linha abaixo se não for usar 'Participantes' na view de confirmação
                Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(participantes)
            };
            return View("Delete", participanteModel); // Isso renderizaria ConfirmDelete.cshtml (ou Delete.cshtml, se preferir manter o nome do arquivo da view)
        }

        [HttpPost]
        [Route("Delete/{id}")] // Esta rota e método ficam como estão para o POST de exclusão
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _participanteService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}