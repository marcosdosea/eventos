using AutoMapper;
using Core;
using Core.Service;
using Microsoft.AspNetCore.Mvc;
using EventoWeb.Models;

namespace EventoWeb.Controllers;

public class PessoaController : Controller
{
    private readonly IPessoaService _pessoaService;
    private readonly IMapper _mapper;

    public PessoaController(IPessoaService pessoaService, IMapper mapper)
    {
        _pessoaService = pessoaService;
        _mapper = mapper;
    }

    // GET: PessoaController
    public ActionResult Index()
    {
        var listaPessoa= _pessoaService.GetAll();
        var listaPessoaModel = _mapper.Map<List<PessoaModel>>(listaPessoa);
        return View(listaPessoaModel);
    }

    // GET: PessoaController/Details/5
    public ActionResult Details(uint id)
    {
        Pessoa pessoa = _pessoaService.Get(id);
        PessoaModel pessoaModel = _mapper.Map<PessoaModel>(pessoa);
        return View(pessoaModel);
    }

    // GET: PessoaController/Create
    public ActionResult Create()
    {
        var pessoaModel = new PessoaModel();
        return View(pessoaModel);
    }


    // POST: PessoaController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(PessoaModel pessoaModel)
    {
        if (ModelState.IsValid)
        {
            var pessoa = _mapper.Map<Pessoa>(pessoaModel);
            _pessoaService.Create(pessoa);
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: PessoaController/Edit/5
    public ActionResult Edit(uint id)
    {
        return Details(id);
    }

    // POST: PessoaController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(uint id, PessoaModel pessoaModel)
    {
        if (ModelState.IsValid)
        {
            var pessoa = _mapper.Map<Pessoa>(pessoaModel);
            _pessoaService.Edit(pessoa);
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: PessoaController/Delete/5
    public ActionResult Delete(uint id)
    {
        Pessoa pessoa = _pessoaService.Get(id);
        PessoaModel pessoaModel = _mapper.Map<PessoaModel>(pessoa);
        return View(pessoaModel);
    }

    // POST: PessoaController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(uint id, PessoaModel pessoaModel)
    {
        _pessoaService.Delete(id);
        return RedirectToAction(nameof(Index));
    }
}