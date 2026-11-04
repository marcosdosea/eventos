using System;
using System.Linq;
using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
    [Route("subeventos/{idSubEvento:int}/participacoes")]
    public class ParticipacaoPessoaSubEventoController : Controller
    {
        private readonly IParticipacaoPessoaSubEventoService _participacaoService;
        private readonly IPessoaService _pessoaService;
        private readonly ISubeventoService _subeventoService;
        private readonly IMapper _mapper;
        private readonly IInscricaoService _inscricaoService;

        public ParticipacaoPessoaSubEventoController(
            IParticipacaoPessoaSubEventoService participacaoService,
            IPessoaService pessoaService,
            ISubeventoService subeventoService,
            IMapper mapper,
            IInscricaoService inscricaoService)
        {
            _participacaoService = participacaoService;
            _pessoaService = pessoaService;
            _subeventoService = subeventoService;
            _mapper = mapper;
            _inscricaoService = inscricaoService;
        }

        private IActionResult? ValidarAcesso(int idSubEvento)
        {
            if (idSubEvento <= 0) return NotFound();
            var subevento = _subeventoService.Get((uint)idSubEvento);
            if (subevento == null) return NotFound();
            if (User.IsInRole("ADMINISTRADOR")) return null;

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Forbid();
            if ((User.IsInRole("GESTOR") && _inscricaoService.GetGestorInEvent(username, subevento.IdEvento) != null) ||
                (User.IsInRole("COLABORADOR") && _inscricaoService.GetColaboradorInEvent(username, subevento.IdEvento) != null))
                return null;

            return Forbid();
        }

        [HttpGet]
        public IActionResult Index(int idSubEvento)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;

            var participacoes = _participacaoService.GetBySubEvento((uint)idSubEvento);
            var model = participacoes
                .Select(p => _mapper.Map<ParticipacaoPessoaSubEventoModel>(p))
                .ToList();

            ViewBag.SubEventoNome = _subeventoService.Get((uint)idSubEvento)?.Nome ?? string.Empty;
            return View(model);
        }

        [HttpGet("{id:int}")]
        public IActionResult Details(int id, int idSubEvento)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;

            var entity = _participacaoService
                .GetBySubEvento((uint)idSubEvento)
                .FirstOrDefault(p => p.Id == (uint)id);
            if (entity == null) return NotFound();

            var model = _mapper.Map<ParticipacaoPessoaSubEventoModel>(entity);
            return View(model);
        }

        [HttpGet("create")]
        public IActionResult Create(int idSubEvento)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;

            var pessoas = _pessoaService.GetAll();
            var model = new ParticipacaoPessoaSubEventoModel
            {
                IdSubEvento = (uint)idSubEvento,
                Entrada = DateTime.Now,
                Pessoas = new SelectList(pessoas, "Id", "Nome")
            };
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create([FromRoute] int idSubEvento, ParticipacaoPessoaSubEventoModel model)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;
            if (model.IdSubEvento != (uint)idSubEvento) return BadRequest();

            if (!ModelState.IsValid)
            {
                model.Pessoas = new SelectList(
                    _pessoaService.GetAll(), "Id", "Nome", model.IdPessoa);
                return View(model);
            }

            var entity = _mapper.Map<Participacaopessoasubevento>(model);
            _participacaoService.Create(entity);
            return RedirectToAction(nameof(Index), new { idSubEvento = model.IdSubEvento });
        }

        [HttpGet("edit/{id:int}")]
        public IActionResult Edit(int id, int idSubEvento)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;

            var entity = _participacaoService
                .GetBySubEvento((uint)idSubEvento)
                .FirstOrDefault(p => p.Id == (uint)id);
            if (entity == null) return NotFound();

            var model = _mapper.Map<ParticipacaoPessoaSubEventoModel>(entity);
            model.Pessoas = new SelectList(
                _pessoaService.GetAll(), "Id", "Nome", model.IdPessoa);
            return View(model);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromRoute] int idSubEvento, [FromRoute] int id, ParticipacaoPessoaSubEventoModel model)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;
            if (id <= 0 || model.Id != (uint)id || model.IdSubEvento != (uint)idSubEvento)
                return BadRequest();
            var existente = _participacaoService.GetBySubEvento((uint)idSubEvento)
                .FirstOrDefault(p => p.Id == (uint)id);
            if (existente == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Pessoas = new SelectList(
                    _pessoaService.GetAll(), "Id", "Nome", model.IdPessoa);
                return View(model);
            }

            var entity = _mapper.Map<Participacaopessoasubevento>(model);
            _participacaoService.Update(entity);
            return RedirectToAction(nameof(Index), new { idSubEvento = model.IdSubEvento });
        }

        [HttpGet("delete/{id:int}")]
        public IActionResult Delete(int id, int idSubEvento)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;

            var entity = _participacaoService
                .GetBySubEvento((uint)idSubEvento)
                .FirstOrDefault(p => p.Id == (uint)id);
            if (entity == null) return NotFound();

            var model = _mapper.Map<ParticipacaoPessoaSubEventoModel>(entity);
            model.NomePessoa = _pessoaService.Get(entity.IdPessoa)?.Nome ?? string.Empty;
            model.NomeSubEvento = _subeventoService.Get((uint)idSubEvento)?.Nome ?? string.Empty;
            return View(model);
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id, int idSubEvento)
        {
            var acesso = ValidarAcesso(idSubEvento);
            if (acesso != null) return acesso;

            var entity = _participacaoService
                .GetBySubEvento((uint)idSubEvento)
                .FirstOrDefault(p => p.Id == (uint)id);
            if (entity == null) return NotFound();

            _participacaoService.Delete((uint)id);
            TempData["Message"] = "Participante removido do sub-evento com sucesso.";
            return RedirectToAction(nameof(Index), new { idSubEvento });
        }
    }
}
