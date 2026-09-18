using Microsoft.AspNetCore.Mvc;
using Core.Service;
using Core.DTO;
using AutoMapper;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventoWeb.Controllers
{
    public class InscricaopessoaeventoController : Controller
    {
        private readonly IInscricaopessoaeventoService _service;
        private readonly IPessoaService _pessoaService;
        private readonly IEventoService _eventoService;
        private readonly ITipoInscricaoService _tipoInscricaoService;
        private readonly IMapper _mapper;

        public InscricaopessoaeventoController(
            IInscricaopessoaeventoService service,
            IPessoaService pessoaService,
            IEventoService eventoService,
            ITipoInscricaoService tipoInscricaoService,
            IMapper mapper)
        {
            _service = service;
            _pessoaService = pessoaService;
            _eventoService = eventoService;
            _tipoInscricaoService = tipoInscricaoService;
            _mapper = mapper;
        }

        // ------------------ CRUD ADMINISTRATIVO ------------------

        // GET: /Inscricaopessoaevento/
        public IActionResult Index()
        {
            var inscricoes = _service.GetAll();
            var dtos = inscricoes.Select(i => _mapper.Map<InscricaopessoaeventoDTO>(i));
            return View(dtos);
        }

        // GET: /Inscricaopessoaevento/Details/5
        public IActionResult Details(uint id)
        {
            var inscricao = _service.GetById(id);
            if (inscricao == null)
                return NotFound();

            var dto = _mapper.Map<InscricaopessoaeventoDTO>(inscricao);
            return View(dto);
        }

        // GET: /Inscricaopessoaevento/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Inscricaopessoaevento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(InscricaopessoaeventoDTO dto)
        {
            if (ModelState.IsValid)
            {
                var inscricao = _mapper.Map<Inscricaopessoaevento>(dto);
                try
                {
                    _service.Create(inscricao);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }
            return View(dto);
        }

        // GET: /Inscricaopessoaevento/Edit/5
        public IActionResult Edit(uint id)
        {
            var inscricao = _service.GetById(id);
            if (inscricao == null)
                return NotFound();

            var dto = _mapper.Map<InscricaopessoaeventoDTO>(inscricao);
            return View(dto);
        }

        // POST: /Inscricaopessoaevento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(uint id, InscricaopessoaeventoDTO dto)
        {
            if (id != dto.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var inscricao = _mapper.Map<Inscricaopessoaevento>(dto);
                _service.Update(inscricao);
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        // GET: /Inscricaopessoaevento/Delete/5
        public IActionResult Delete(uint id)
        {
            var inscricao = _service.GetById(id);
            if (inscricao == null)
                return NotFound();

            var dto = _mapper.Map<InscricaopessoaeventoDTO>(inscricao);
            ViewBag.NomePessoa = _pessoaService.Get(inscricao.IdPessoa)?.Nome ?? string.Empty;
            ViewBag.NomeEvento = _eventoService.GetNomeById(inscricao.IdEvento);
            return View(dto);
        }

        // POST: /Inscricaopessoaevento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(uint id)
        {
            var inscricao = _service.GetById(id);
            if (inscricao == null)
                return NotFound();

            _service.Delete(id);
            TempData["Message"] = "Inscrição do participante removida com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------ TELA PÚBLICA DE INSCRIÇÃO ------------------

        // GET: /Inscricaopessoaevento/Inscrever/5
        [Authorize]
        public IActionResult Inscrever(uint idEvento)
        {
            var evento = _eventoService.Get(idEvento);
            if (evento == null)
                return NotFound();

            var lotes = _tipoInscricaoService.GetByEvento(idEvento)
                .Select(t => new LoteInscricaoModel
                {
                    Id = t.Id.ToString(),
                    NomeLote = t.Nome,
                    Descricao = t.Descricao,
                    Preco = t.Valor,
                    Quantidade = 0
                }).ToList();

            var model = new InscricaopessoaeventoModel
            {
                IdEvento = evento.Id,
                NomeEvento = evento.Nome,
                // Evento não possui campo de banner; view exibe bloco neutro quando vazio.
                BannerUrl = string.Empty,
                DataEvento = evento.DataInicio ?? DateTime.Today,
                DataFimEvento = evento.DataFim ?? DateTime.Today,
                LocalEvento = string.Join(", ", new[]
                {
                    string.Join(" ", new[] { evento.Rua, evento.Numero }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    evento.Bairro,
                    string.Join(" - ", new[] { evento.Cidade, evento.Estado }.Where(s => !string.IsNullOrWhiteSpace(s)))
                }.Where(s => !string.IsNullOrWhiteSpace(s))),
                DescricaoEvento = evento.Descricao ?? string.Empty,
                Lotes = lotes,
                TotalSelecionado = 0
            };

            return View(model);
        }

        // POST: /Inscricaopessoaevento/Inscrever/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inscrever(InscricaopessoaeventoModel model)
        {
            // Processa as inscrições selecionadas
            foreach (var lote in model.Lotes)
            {
                if (lote.Quantidade > 0)
                {
                    // Aqui você pode montar o DTO/Entidade e salvar no banco
                    var inscricao = new Inscricaopessoaevento
                    {
                        IdEvento = model.IdEvento,
                        IdPessoa = 1, // <-- Troque para o id do participante real!
                        IdPapel = 1, // <-- Ajuste conforme necessário
                        IdTipoInscricao = uint.TryParse(lote.Id, out uint tipo) ? tipo : (uint?)null,
                        DataInscricao = DateTime.Now,
                        ValorTotal = lote.Preco * lote.Quantidade,
                        Status = "A",
                        FrequenciaFinal = 0,
                        NomeCracha = ""
                    };
                    // _service.Create(inscricao);
                }
            }
            return RedirectToAction("Sucesso");
        }

        // GET: /Inscricaopessoaevento/Sucesso
        public IActionResult Sucesso()
        {
            return View();
        }
    }
}