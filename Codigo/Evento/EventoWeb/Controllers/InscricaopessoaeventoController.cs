using Microsoft.AspNetCore.Mvc;
using Core.Service;
using Core.DTO;
using AutoMapper;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<InscricaopessoaeventoController> _logger;
        private readonly IMapper _mapper;

        public InscricaopessoaeventoController(
            IInscricaopessoaeventoService service,
            IPessoaService pessoaService,
            IEventoService eventoService,
            ITipoInscricaoService tipoInscricaoService,
            ILogger<InscricaopessoaeventoController> logger,
            IMapper mapper)
        {
            _service = service;
            _pessoaService = pessoaService;
            _eventoService = eventoService;
            _tipoInscricaoService = tipoInscricaoService;
            _logger = logger;
            _mapper = mapper;
        }

        // ------------------ CRUD ADMINISTRATIVO ------------------

        // GET: /Inscricaopessoaevento/
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        public IActionResult Index()
        {
            var inscricoes = _service.GetAll();
            var dtos = inscricoes.Select(i => _mapper.Map<InscricaopessoaeventoDTO>(i));
            return View(dtos);
        }

        // GET: /Inscricaopessoaevento/Details/5
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        public IActionResult Details(uint id)
        {
            var inscricao = _service.GetById(id);
            if (inscricao == null)
                return NotFound();

            var dto = _mapper.Map<InscricaopessoaeventoDTO>(inscricao);
            return View(dto);
        }

        // GET: /Inscricaopessoaevento/Create
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Inscricaopessoaevento/Create
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
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
                    _logger.LogError(ex, "Erro ao salvar inscrição");
                    ModelState.AddModelError(string.Empty, "Não foi possível salvar");
                }
            }
            return View(dto);
        }

        // GET: /Inscricaopessoaevento/Edit/5
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        public IActionResult Edit(uint id)
        {
            var inscricao = _service.GetById(id);
            if (inscricao == null)
                return NotFound();

            var dto = _mapper.Map<InscricaopessoaeventoDTO>(inscricao);
            return View(dto);
        }

        // POST: /Inscricaopessoaevento/Edit/5
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(uint id, InscricaopessoaeventoDTO dto)
        {
            if (id != dto.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var inscricao = _mapper.Map<Inscricaopessoaevento>(dto);
                    _service.Update(inscricao);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao salvar inscrição {Id}", id);
                    ModelState.AddModelError(string.Empty, "Não foi possível salvar");
                }
            }
            return View(dto);
        }

        // GET: /Inscricaopessoaevento/Delete/5
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
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
        [Authorize(Roles = "ADMINISTRADOR,GESTOR")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(uint id)
        {
            var inscricao = _service.GetById(id);
            if (inscricao == null)
                return NotFound();

            try
            {
                _service.Delete(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar inscrição {Id}", id);
                ModelState.AddModelError(string.Empty, "Não foi possível salvar");
                var dto = _mapper.Map<InscricaopessoaeventoDTO>(inscricao);
                return View("Delete", dto);
            }
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Inscrever(InscricaopessoaeventoModel model)
        {
            if (!ModelState.IsValid)
            {
                RecarregarDadosTela(model);
                return View(model);
            }

            if (model.Lotes == null || !model.Lotes.Any(l => l.Quantidade > 0))
            {
                ModelState.AddModelError(string.Empty, "Selecione ao menos um lote.");
                RecarregarDadosTela(model);
                return View(model);
            }

            if (model.Lotes.Any(l => l.Quantidade < 0))
            {
                ModelState.AddModelError(string.Empty, "Quantidade inválida.");
                RecarregarDadosTela(model);
                return View(model);
            }

            var cpf = User.Identity?.Name;
            if (string.IsNullOrEmpty(cpf))
                return Challenge();

            var pessoa = _pessoaService.GetByCpf(cpf);
            if (pessoa == null)
                return Forbid();

            var evento = _eventoService.Get(model.IdEvento);
            if (evento == null)
                return NotFound();

            try
            {
                foreach (var lote in model.Lotes.Where(l => l.Quantidade > 0))
                {
                    if (!uint.TryParse(lote.Id, out uint idTipoInscricao))
                    {
                        ModelState.AddModelError(string.Empty, "Lote inválido.");
                        RecarregarDadosTela(model);
                        return View(model);
                    }

                    var tipo = _tipoInscricaoService.Get(idTipoInscricao);
                    if (tipo == null || tipo.IdEvento != model.IdEvento)
                    {
                        ModelState.AddModelError(string.Empty, "Lote inválido para este evento.");
                        RecarregarDadosTela(model);
                        return View(model);
                    }

                    var inscricao = new Inscricaopessoaevento
                    {
                        IdEvento = model.IdEvento,
                        IdPessoa = pessoa.Id,
                        IdPapel = 4,
                        IdTipoInscricao = tipo.Id,
                        DataInscricao = DateTime.Now,
                        ValorTotal = tipo.Valor * lote.Quantidade,
                        Status = "S",
                        FrequenciaFinal = 0,
                        NomeCracha = pessoa.NomeCracha
                    };
                    _service.Create(inscricao);
                }
                return RedirectToAction("Sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inscrever falhou evento {Evento} user {User}", model.IdEvento, User.Identity?.Name);
                ModelState.AddModelError(string.Empty, "Não foi possível concluir a inscrição. Tente novamente.");
                RecarregarDadosTela(model);
                return View(model);
            }
        }

        private void RecarregarDadosTela(InscricaopessoaeventoModel model)
        {
            var quantidades = (model.Lotes ?? new List<LoteInscricaoModel>())
                .Where(l => l != null && l.Id != null)
                .GroupBy(l => l.Id)
                .ToDictionary(g => g.Key, g => g.Max(l => l.Quantidade));

            var evento = _eventoService.Get(model.IdEvento);
            if (evento != null)
            {
                model.NomeEvento = evento.Nome;
                if (evento.DataInicio.HasValue)
                    model.DataEvento = evento.DataInicio.Value;
                if (evento.DataFim.HasValue)
                    model.DataFimEvento = evento.DataFim.Value;
                model.LocalEvento = string.Join(", ", new[]
                {
                    string.Join(" ", new[] { evento.Rua, evento.Numero }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    evento.Bairro,
                    string.Join(" - ", new[] { evento.Cidade, evento.Estado }.Where(s => !string.IsNullOrWhiteSpace(s)))
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
                model.DescricaoEvento = evento.Descricao ?? string.Empty;
            }

            var tipos = _tipoInscricaoService.GetByEvento(model.IdEvento);
            if (tipos != null)
            {
                model.Lotes = tipos.Select(t => new LoteInscricaoModel
                {
                    Id = t.Id.ToString(),
                    NomeLote = t.Nome,
                    Descricao = t.Descricao,
                    Preco = t.Valor,
                    Quantidade = quantidades.TryGetValue(t.Id.ToString(), out int qtd) && qtd > 0 ? qtd : 0
                }).ToList();
            }
        }

        // GET: /Inscricaopessoaevento/Sucesso
        [Authorize]
        public IActionResult Sucesso()
        {
            return View();
        }
    }
}