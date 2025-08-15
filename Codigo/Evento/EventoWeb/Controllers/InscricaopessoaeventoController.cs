using Microsoft.AspNetCore.Mvc;
using Core.Service;
using Core.DTO;
using AutoMapper;
using Core;
using EventoWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventoWeb.Controllers
{
    public class InscricaopessoaeventoController : Controller
    {
        private readonly IInscricaopessoaeventoService _service;
        private readonly IMapper _mapper;

        public InscricaopessoaeventoController(IInscricaopessoaeventoService service, IMapper mapper)
        {
            _service = service;
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
            return View(dto);
        }

        // POST: /Inscricaopessoaevento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(uint id)
        {
            _service.Delete(id);
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