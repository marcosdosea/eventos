using Microsoft.AspNetCore.Mvc;
using Core.Service;
using Core.DTO;
using AutoMapper;
using Core;

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
    }
}
