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

        public PessoaController(
            IPessoaService pessoaService,
            IEstadosbrasilService estadosbrasilService,
            IMapper mapper)
        {
            _pessoaService = pessoaService;
            _estadosbrasilService = estadosbrasilService;
            _mapper = mapper;
        }

        // =====================================================================
        // INDEX (apenas ADM vê todos)
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            var lista = _pessoaService.GetAll();
            var model = _mapper.Map<List<PessoaModel>>(lista);
            return View(model);
        }

        // =====================================================================
        // DETAILS
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("Details/{id}")]
        public ActionResult Details(uint id)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null) return RedirectToAction("Index", "Home");

            return View(_mapper.Map<PessoaModel>(pessoa));
        }
        
        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("GestoresSistema")]
        public async Task<ActionResult> GestoresSistema()
        {
            var gestores = await _pessoaService.GetAllGestorAsync();
            var viewModel = new GestorModel
            {
                Gestores = _mapper.Map<List<PessoaModel>>(gestores.OrderBy(p => p.Nome))
            };
            return View(viewModel);
        }

        [Authorize(Roles = "GESTOR")]
        [HttpGet]
        [Route("Create")]
        public ActionResult CreateUsuario()
        {
            var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new PessoaModel
            {
                Estados = new SelectList(estados, "Estado", "Nome")
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateUsuario(PessoaModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = new Pessoa
                {
                    Cpf = viewModel.Cpf,
                    Nome = viewModel.Nome,
                    Telefone1 = viewModel.Telefone1,
                    Email = viewModel.Email
                };

                await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 0, 5);

            }
            return View(viewModel);
        }

        // =====================================================================
        // CREATE (auto-cadastro — sem autenticação)
        // Cria a Pessoa + usuário Identity com role USUARIO
        // =====================================================================

        [AllowAnonymous]
        [HttpGet]
        [Route("Create")]
        public ActionResult Create(string? returnUrl)
        {
            var estados = _estadosbrasilService.GetAll().OrderBy(e => e.Nome);
            var viewModel = new PessoaModel
            {
                Estados = new SelectList(estados, "Estado", "Nome")
            };
             ViewBag.ReturnUrl = returnUrl;
            return View(viewModel);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(PessoaModel viewModel,string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                byte[]? fotoSource = ProcessarFoto(viewModel, out string? fotoErro);
                if (fotoErro != null)
                {
                    ModelState.AddModelError("Foto", fotoErro);
                    viewModel.Estados = new SelectList(
                        _estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome");
                    return View(viewModel);
                }

                var pessoa = _mapper.Map<Pessoa>(viewModel);
                pessoa.Foto = fotoSource;

                try
                {
                    await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa, 0, 5);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                      return Redirect(returnUrl);
                    } 

                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        [Route("Edit/{id}")]
        public ActionResult Edit(uint id,string? returnUrl)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null) return NotFound();

            if (pessoa.Cpf != User.Identity!.Name && !User.IsInRole("ADMINISTRADOR"))
                return Forbid();

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
        public async Task<ActionResult> Edit(uint id, PessoaModel viewModel, string? returnUrl)
        {
            var pessoa = _pessoaService.Get(id);
            if (pessoa == null) return NotFound();

            if (pessoa.Cpf != User.Identity!.Name && !User.IsInRole("ADMINISTRADOR"))
                return Forbid();

            if (id != viewModel.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                byte[]? fotoSource = ProcessarFoto(viewModel, out string? fotoErro);
                if (fotoErro != null)
                {
                    ModelState.AddModelError("Foto", fotoErro);
                    viewModel.Estados = new SelectList(
                        _estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome");
                    return View(viewModel);
                }

                var pessoaEditada = _mapper.Map<Pessoa>(viewModel);
                pessoaEditada.Foto = fotoSource;
                await _pessoaService.Edit(pessoaEditada);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) { 
                    return Redirect(returnUrl);
                }

                              
                
            }

            viewModel.Estados = new SelectList(
                _estadosbrasilService.GetAll().OrderBy(e => e.Nome), "Estado", "Nome");
            return View(viewModel);
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("Delete/{id}")]
        [Route("Delete")]
        public ActionResult Delete(PessoaModel viewModel)
        {
            var pessoa = _pessoaService.Get(viewModel.Id);
            if (pessoa == null) return NotFound();
            PessoaModel pessoaModel = _mapper.Map<PessoaModel>(pessoa);
            
            return View(pessoaModel);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost, ActionName("Delete")]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(PessoaModel viewModel)
        {
                       
                var sucesso = _pessoaService.Delete(viewModel.Id);

                if (sucesso)
                {
                    TempData["SuccessMessage"] = "Exclusão realizada com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao excluir pessoa";
                }

            if (User.IsInRole("ADMINISTRADOR"))
            {
                return RedirectToAction("DefinirAdministrador", "Pessoa");
            }


            return RedirectToAction("Index", "Pessoa");
        }

        // =====================================================================
        // DEFINIR ADMINISTRADOR
        // =====================================================================

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("DefinirAdministrador")]
        public async Task<ActionResult> DefinirAdministrador()
        {
            var admins = await _pessoaService.GetAllAdmAsync();
            var viewModel = new GestaoAdministradorModel
            {
                Administradores = _mapper.Map<List<PessoaModel>>(admins.OrderBy(p => p.Nome).ToList())
            };
            return View(viewModel);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        [Route("DefinirAdministrador")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DefinirAdministrador(GestaoAdministradorModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var pessoa = new Pessoa
                {
                    Cpf = viewModel.Cpf,
                    Nome = viewModel.Nome,
                    NomeCracha = viewModel.Nome,
                    Telefone1 = viewModel.Telefone1,
                    Email = viewModel.Email
                };

                
                await _pessoaService.CreatePessoaIdentityComPapelAsync(pessoa,0 ,1);
                TempData["SuccessMessage"] = "Administrador definido com sucesso.";
                return RedirectToAction(nameof(DefinirAdministrador));
               
            }

            var adminsAtuais = await _pessoaService.GetAllAdmAsync();
            viewModel.Administradores = _mapper.Map<List<PessoaModel>>(adminsAtuais.OrderBy(p => p.Nome).ToList());
            return View(viewModel);
        }

        // =====================================================================
        // HELPER PRIVADO
        // =====================================================================

        private static byte[]? ProcessarFoto(PessoaModel viewModel, out string? erro)
        {
            erro = null;
            if (viewModel.Foto == null || viewModel.Foto.Length == 0) return null;

            using var ms = new MemoryStream();
            viewModel.Foto.CopyTo(ms);
            if (ms.Length > 65535)
            {
                erro = "O arquivo é muito grande. Deve ser menor que 64 KB.";
                return null;
            }
            return ms.ToArray();
        }
    }
}
