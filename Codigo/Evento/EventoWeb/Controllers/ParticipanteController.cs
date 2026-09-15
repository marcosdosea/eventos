using AutoMapper;
using Core;
using Core.DTO;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    public class ParticipanteController : Controller
    {
        private readonly IParticipanteService _participanteService;
        private readonly IPessoaService _pessoaService;
        private readonly IEstadosbrasilService _estadosbrasilService;
        private readonly IMapper _mapper;

        public ParticipanteController(IParticipanteService participanteService, IPessoaService pessoaService, IEstadosbrasilService estadosbrasilService, IMapper mapper)
        {
            _participanteService = participanteService;
            _pessoaService = pessoaService;
            _estadosbrasilService = estadosbrasilService;
            _mapper = mapper;
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
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

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("Edit/{cpf}")]
        public async Task<ActionResult> Edit(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return NotFound();
            }
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
            participanteModel.Participante.Estados = new SelectList(
                _estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome");
            return View(participanteModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpPost]
        [Route("Edit/{cpf}")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string cpf, ParticipanteModel participanteModel)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return NotFound();
            }
            var existente = await _participanteService.GetParticipanteByCpfAsync(cpf);
            if (existente == null)
            {
                return NotFound();
            }
            if (participanteModel?.Participante == null)
            {
                participanteModel ??= new ParticipanteModel();
                participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
                return View(participanteModel);
            }
            if (!string.Equals(cpf, participanteModel.Participante.Cpf, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Participante.Cpf", "CPF da rota não confere com o CPF do formulário.");
            }
            if (ModelState.IsValid)
            {
                var pessoa = _mapper.Map<Pessoa>(participanteModel.Participante);
                pessoa.Id = existente.Id;
                pessoa.Cpf = existente.Cpf;
                if (pessoa.Foto == null)
                {
                    pessoa.Foto = existente.Foto;
                }
                await _pessoaService.Edit(pessoa);
                return RedirectToAction(nameof(Index));
            }
            participanteModel.Participantes = _mapper.Map<IEnumerable<ParticipanteDTO>>(await _participanteService.GetParticipantesAsync());
            participanteModel.Participante.Estados = new SelectList(
                _estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome");
            return View(participanteModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("Details/{cpf}")]
        public async Task<ActionResult> Details(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return NotFound();
            }
            var participante = await _participanteService.GetParticipanteByCpfAsync(cpf);
            if (participante == null)
            {
                return NotFound();
            }
            var participanteModel = new ParticipanteModel
            {
                Participante = _mapper.Map<PessoaModel>(participante)
            };
            return View(participanteModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpGet]
        [Route("ConfirmDelete/{cpf}")]
        public async Task<ActionResult> ConfirmDelete(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return BadRequest();
            }
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
            return View("Delete", participanteModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }
            var participantes = await _participanteService.GetParticipantesAsync();
            var participante = participantes.FirstOrDefault(c => c.Cpf == id);
            if (participante == null)
            {
                return NotFound();
            }
            try
            {
                await _participanteService.DeleteAsync(id);
            }
            catch
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Participante removido com sucesso.";
            return RedirectToAction(nameof(Index));
        }
    }
}