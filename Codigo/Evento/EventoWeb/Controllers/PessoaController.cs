using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
            var viewModel = new PessoaModel();
            viewModel.Estados = new SelectList(estados, "Estado", "Nome");
            return View(viewModel);
        }

        // POST: PessoaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PessoaModel viewModel)
        {
            if (ModelState.IsValid)
            {
                byte[] fotoSource = null;
                if (viewModel.Foto != null && viewModel.Foto.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        viewModel.Foto.CopyTo(memoryStream);

                        if (memoryStream.Length <= 65535)
                        {
                            fotoSource = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("Foto", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            return View(viewModel);
                        }
                    }
                }
                var pessoa = _mapper.Map<Pessoa>(viewModel);
                pessoa.Foto = fotoSource;
                _pessoaService.Create(pessoa);
                return RedirectToAction(nameof(Index));
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
            
            var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var viewModel =  _mapper.Map<PessoaModel>(pessoa);
            viewModel.Estados = new SelectList(estados, "Estado", "Nome");

            return View(viewModel);
        }

        // POST: PessoaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, PessoaModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                byte[] fotoSource = null;
                if (viewModel.Foto != null && viewModel.Foto.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        viewModel.Foto.CopyTo(memoryStream);

                        if (memoryStream.Length <= 65535)
                        {
                            fotoSource = memoryStream.ToArray();
                        }
                        else
                        {
                            ModelState.AddModelError("Foto", "O arquivo é muito grande. Deve ser menor que 64 KB.");
                            return View(viewModel);
                        }
                    }
                }
                var pessoa = _mapper.Map<Pessoa>(viewModel);
                pessoa.Foto = fotoSource;
                _pessoaService.Edit(pessoa);
                return RedirectToAction(nameof(Index));
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
