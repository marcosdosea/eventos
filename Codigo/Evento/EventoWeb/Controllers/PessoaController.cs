using AutoMapper;
using Core;
using Core.Service;
using EventoWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Controllers
{
    [Route("[controller]")]
    [Authorize]
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

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            var listaPessoa = _pessoaService.GetAll();
            var listaPessoaModel = _mapper.Map<List<PessoaModel>>(listaPessoa);
            return View(listaPessoaModel);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(uint id)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return RedirectToAction("Index", "Home");
            }

            PessoaModel pessoaModel = _mapper.Map<PessoaModel>(pessoa);
            return View(pessoaModel);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("DefinirAdministrador")]
        //GET: PessoaController/DefinirAdministrador
        public async Task<ActionResult> DefinirAdministrador()
        {
            var admins = await _pessoaService.GetAllAdmAsync();
            var viewModel = new GestaoAdministradorModel
            {
                Administradores = _mapper.Map<List<PessoaModel>>(admins.OrderBy(p => p.Nome))


            };

            return View(viewModel);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        [Route("DefinirAdministrador")]
        [ValidateAntiForgeryToken]
        //POST: PessoaController/DefinirAdministrador
        public async Task<ActionResult> DefinirAdministrador(GestaoAdministradorModel pessoaModel)
        {
            if (ModelState.IsValid)
            {


                var pessoa = new Pessoa
                {
                    Cpf = pessoaModel.Cpf,
                    Nome = pessoaModel.Nome,
                    NomeCracha = pessoaModel.Nome,
                    Telefone1 = pessoaModel.Telefone1,
                    Email = pessoaModel.Email
                };

                if (pessoa == null)
                {

                    return View(pessoaModel);
                }
                ModelState.AddModelError("", "O usuário já existe.");
                try
                {
                    await _pessoaService.CreatePessoaPapelAsync(pessoa, 0, 1);
                    TempData["Success"] = "Administrador definido com sucesso.";
                    return RedirectToAction(nameof(DefinirAdministrador));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Erro ao definir administrador: " + ex.Message);
                    return View(pessoaModel);
                }
            }


            return View(pessoaModel);

        }

        [AllowAnonymous]
        [HttpGet]
        [Route("Create")]
        public ActionResult Create()
        {
            var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new PessoaModel();
            viewModel.Estados = new SelectList(estados, "Estado", "Nome");
            return View(viewModel);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PessoaModel viewModel)
        {
            if (ModelState.IsValid)
            {
                byte[]? fotoSource = null;
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

        [Authorize]
        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(uint id, string? returnUrl)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return NotFound();
            }

            if (pessoa.Cpf != User.Identity!.Name && !User.IsInRole("ADMINISTRADOR"))
            {
                return Forbid();
            }

            var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var viewModel = _mapper.Map<PessoaModel>(pessoa);
            viewModel.Estados = new SelectList(estados, "Estado", "Nome");

            ViewBag.ReturnUrl = returnUrl;
            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(uint id, PessoaModel viewModel, string? returnUrl)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return NotFound();
            }

            if (pessoa.Cpf != User.Identity!.Name && !User.IsInRole("ADMINISTRADOR"))
            {
                return Forbid();
            }

            if (id != viewModel.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                byte[]? fotoSource = null;
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

                _mapper.Map(viewModel, pessoa);
                 pessoa.Foto = fotoSource;
                _pessoaService.Edit(pessoa);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);


                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: PessoaController/Delete/5
        [HttpGet]
        [Authorize]
        [Route("Delete/{id}")]
        public ActionResult Delete(uint id, string? returnUrl)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return NotFound();
            }

            if (pessoa.Cpf != User.Identity!.Name && !User.IsInRole("ADMINISTRADOR"))
            {
                return Forbid();
            }

            PessoaModel pessoaModel = _mapper.Map<PessoaModel>(pessoa);
            ViewBag.ReturnUrl = returnUrl;
            return View(pessoaModel);
        }

        // POST: PessoaController/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(uint id, string? returnUrl)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null)
            {
                return NotFound();
            }
            if (pessoa.Cpf != User.Identity!.Name && !User.IsInRole("ADMINISTRADOR"))
            {
                return Forbid();
            }
            _pessoaService.Delete(id);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
