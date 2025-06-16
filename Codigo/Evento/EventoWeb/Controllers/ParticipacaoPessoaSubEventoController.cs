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

        public ParticipacaoPessoaSubEventoController(
            IParticipacaoPessoaSubEventoService participacaoService,
            IPessoaService pessoaService,
            ISubeventoService subeventoService,
            IMapper mapper)
        {
            _participacaoService = participacaoService;
            _pessoaService = pessoaService;
            _subeventoService = subeventoService;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult Index(int idSubEvento)
        {
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
            var pessoas = _pessoaService.GetAll();
            var model = new ParticipacaoPessoaSubEventoModel
            {
                IdSubEvento = (uint)idSubEvento,
                Entrada = DateTime.Now,
                Pessoas = new SelectList(pessoas, "Id", "NomePessoa")
            };
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ParticipacaoPessoaSubEventoModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Pessoas = new SelectList(
                    _pessoaService.GetAll(), "Id", "NomePessoa", model.IdPessoa);
                return View(model);
            }

            var entity = _mapper.Map<Participacaopessoasubevento>(model);
            _participacaoService.Create(entity);
            return RedirectToAction(nameof(Index), new { idSubEvento = model.IdSubEvento });
        }

        [HttpGet("edit/{id:int}")]
        public IActionResult Edit(int id, int idSubEvento)
        {
            var entity = _participacaoService
                .GetBySubEvento((uint)idSubEvento)
                .FirstOrDefault(p => p.Id == (uint)id);
            if (entity == null) return NotFound();

            var model = _mapper.Map<ParticipacaoPessoaSubEventoModel>(entity);
            model.Pessoas = new SelectList(
                _pessoaService.GetAll(), "Id", "NomePessoa", model.IdPessoa);
            return View(model);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ParticipacaoPessoaSubEventoModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Pessoas = new SelectList(
                    _pessoaService.GetAll(), "Id", "NomePessoa", model.IdPessoa);
                return View(model);
            }

            var entity = _mapper.Map<Participacaopessoasubevento>(model);
            _participacaoService.Update(entity);
            return RedirectToAction(nameof(Index), new { idSubEvento = model.IdSubEvento });
        }

        [HttpGet("delete/{id:int}")]
        public IActionResult Delete(int id, int idSubEvento)
        {
            var entity = _participacaoService
                .GetBySubEvento((uint)idSubEvento)
                .FirstOrDefault(p => p.Id == (uint)id);
            if (entity == null) return NotFound();

            var model = _mapper.Map<ParticipacaoPessoaSubEventoModel>(entity);
            return View(model);
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id, int idSubEvento)
        {
            _participacaoService.Delete((uint)id);
            return RedirectToAction(nameof(Index), new { idSubEvento });
        }
    }
}
