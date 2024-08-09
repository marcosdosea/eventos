using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace EventoWeb.Controllers
{
    public class PessoaController : Controller
    {
        private readonly IPessoaService _pessoaService;
        private readonly IEstadosbrasilService _estadosbrasilService;
        private readonly IMapper _mapper;

        public PessoaController(IPessoaService pessoaService, IEstadosbrasilService estadosbrasilService, IMapper mapper)
        {
            _pessoaService = pessoaService;
            _estadosbrasilService = estadosbrasilService;
            _mapper = mapper;
        }

        // GET: PessoaController
        public ActionResult Index()
        {
            var listaPessoa = _pessoaService.GetAll();
            var listaPessoaModel = _mapper.Map<List<PessoaModel>>(listaPessoa);
            return View(listaPessoaModel);
        }

        // GET: PessoaController/Details/5
        public ActionResult Details(uint id)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return NotFound();
            }

            PessoaModel pessoaModel = _mapper.Map<PessoaModel>(pessoa);
            return View(pessoaModel);
        }

        // GET: PessoaController/Create
        public ActionResult Create()
        {
            var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new PessoaCreateModel()
            {
                Estados = new SelectList(estados, "Estado", "Nome"),
            };

            return View(viewModel);
        }

        // POST: PessoaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PessoaCreateModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (_pessoaService.CPFIsValid(viewModel.Pessoa.Cpf))
                {
                    viewModel.Pessoa.Cpf = _pessoaService.FormataCPF(viewModel.Pessoa.Cpf);
                    viewModel.Pessoa.Cep = _pessoaService.FormataCep(viewModel.Pessoa.Cep);

                    var pessoa = _mapper.Map<Pessoa>(viewModel.Pessoa);
                    _pessoaService.Create(pessoa);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("Pessoa.Cpf", "CPF inválido.");
                }
            }

            return View(viewModel);
        }

        // GET: PessoaController/Edit/5
        public ActionResult Edit(uint id)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return NotFound();
            }

            var pessoaModel = _mapper.Map<PessoaModel>(pessoa);
            var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new PessoaCreateModel
            {
                Pessoa = pessoaModel,
                Estados = new SelectList(estados, "Estado", "Nome")
            };

            return View(viewModel);
        }

        // POST: PessoaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, PessoaCreateModel viewModel)
        {
            if (id != viewModel.Pessoa.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                if (_pessoaService.CPFIsValid(viewModel.Pessoa.Cpf))
                {
                    viewModel.Pessoa.Cpf = _pessoaService.FormataCPF(viewModel.Pessoa.Cpf);
                    viewModel.Pessoa.Cep = _pessoaService.FormataCep(viewModel.Pessoa.Cep);

                    var pessoa = _mapper.Map<Pessoa>(viewModel.Pessoa);
                    _pessoaService.Edit(pessoa);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("Pessoa.Cpf", "CPF inválido.");
                }
            }

            return View(viewModel);
        }

        // GET: PessoaController/Delete/5
        public ActionResult Delete(uint id)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return NotFound();
            }

            PessoaModel pessoaModel = _mapper.Map<PessoaModel>(pessoa);
            return View(pessoaModel);
        }

        // POST: PessoaController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(uint id)
        {
            _pessoaService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
