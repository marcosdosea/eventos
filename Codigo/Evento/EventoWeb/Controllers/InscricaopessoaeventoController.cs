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
        public IActionResult Inscrever(uint idEvento)
        {
            // Monte aqui os dados do evento e dos lotes
            var lotes = new List<LoteInscricaoModel>
            {
                new LoteInscricaoModel { Id = "1", NomeLote = "2,5km - 1º Lote", Descricao = "Inscrições até 06/09/2025", Preco = 167, Quantidade = 0 },
                new LoteInscricaoModel { Id = "2", NomeLote = "5km - 2º Lote", Descricao = "Inscrições até 06/09/2025", Preco = 187, Quantidade = 0 }
            };

            var model = new InscricaopessoaeventoModel
            {
                IdEvento = idEvento,
                NomeEvento = "WE CAN RUN Rede Primavera 2025",
                BannerUrl = "/images/banner-wecanrun.png",
                DataEvento = new DateTime(2025, 9, 6, 16, 0, 0),
                DataFimEvento = new DateTime(2025, 9, 6, 23, 0, 0),
                LocalEvento = "Central Garden, Aracaju - SE",
                DescricaoEvento = "Evento de corrida com várias modalidades. Escolha seu lote e inscreva-se!",
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
        public IActionResult Sucesso()
        {
            return View();
        }
    }
}