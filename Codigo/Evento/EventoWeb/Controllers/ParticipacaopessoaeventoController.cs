using AutoMapper;
using Core;               // namespace onde está a entidade Participacaopessoaevento
using Core.DTO;           // namespace onde estarão seus DTOs (crie ParticipacaoPessoaEventoDTO aqui)
using Core.Service;       // namespace onde estarão suas interfaces IParticipacaoPessoaEventoService e a implementação
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventoWeb.Models;
using Microsoft.AspNetCore.Identity;

namespace EventoWeb.Controllers
{
    [Authorize]
    public class ParticipacaoPessoaEventoController : Controller
    {
        private readonly IParticipacaoPessoaEventoService _participacaoService;
        private readonly IEventoService _eventoService;
        private readonly ISubeventoService _subeventoService;
        private readonly IPessoaService _pessoaService;
        private readonly IInscricaoService _inscricaoService;
        private readonly IMapper _mapper;
        private readonly UserManager<UsuarioIdentity> _userManager;

        public ParticipacaoPessoaEventoController(
            IParticipacaoPessoaEventoService participacaoService,
            IEventoService eventoService,
            ISubeventoService subeventoService,
            IPessoaService pessoaService,
            IInscricaoService inscricaoService,
            IMapper mapper,
            UserManager<UsuarioIdentity> userManager)
        {
            _participacaoService = participacaoService;
            _eventoService = eventoService;
            _subeventoService = subeventoService;
            _pessoaService = pessoaService;
            _inscricaoService = inscricaoService;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        public async Task<IActionResult> Index(uint idEvento, uint? idSubEvento)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var gestor = _inscricaoService.GetGestorInEvent(user.UserName, idEvento);
            var colaborador = _inscricaoService.GetColaboradorInEvent(user.UserName, idEvento);

            if (gestor == null && colaborador == null && !User.IsInRole("ADMINISTRADOR"))
            {
                TempData.Clear();
                TempData["Message"] = "Você não tem permissão para registrar participação!";
                return RedirectToAction("GerenciarEvento", "Evento", new { idEvento });
            }

            var viewModel = new FrequenciaViewModel
            {
                Evento = _eventoService.GetEventoSimpleDto(idEvento),
                SubEvento = idSubEvento.HasValue ? _subeventoService.Get(idSubEvento.Value) : null,
                Frequencias = await _participacaoService.GetAllAsync()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarParticipacao(uint idEvento, uint? idSubEvento, string cpf)
        {
            var gestor = _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento);
            var colaborador = _inscricaoService.GetColaboradorInEvent(User.Identity.Name, idEvento);

            if (gestor == null && colaborador == null && !User.IsInRole("ADMINISTRADOR"))
            {
                TempData.Clear();
                TempData["Message"] = "Você não tem permissão para registrar participação!";
                return RedirectToAction("GerenciarEvento", "Evento");
            }

            // Formata o CPF removendo pontos e traços
            cpf = cpf.Replace(".", "").Replace("-", "");
            
            // Busca a pessoa pelo CPF
            var pessoa = _pessoaService.GetByCpf(cpf);
            if (pessoa == null)
            {
                TempData["Message"] = "Pessoa não encontrada com o CPF informado.";
                return RedirectToAction(nameof(Index), new { idEvento, idSubEvento });
            }

            // Verifica se a pessoa está inscrita no evento
            var inscricao = _inscricaoService.GetByEventoAndPapel(idEvento, 4)
                .FirstOrDefault(i => i.IdPessoa == pessoa.Id);
            
            if (inscricao == null)
            {
                TempData["Message"] = "Pessoa não está inscrita neste evento.";
                return RedirectToAction(nameof(Index), new { idEvento, idSubEvento });
            }

            // Busca a última participação da pessoa no evento
            var ultimaParticipacao = (await _participacaoService.GetAllAsync())
                .Where(f => f.IdPessoa == pessoa.Id && f.IdEvento == idEvento)
                .OrderByDescending(f => f.Id)
                .FirstOrDefault();

            var novaParticipacao = new Participacaopessoaevento
            {
                IdPessoa = pessoa.Id,
                IdEvento = idEvento,
                Entrada = DateTime.Now
            };

            // Se já existe uma entrada sem saída, registra a saída
            if (ultimaParticipacao != null && !ultimaParticipacao.Saida.HasValue)
            {
                ultimaParticipacao.Saida = DateTime.Now;
                await _participacaoService.UpdateAsync(ultimaParticipacao);
            }
            else
            {
                // Caso contrário, registra uma nova entrada
                await _participacaoService.AddAsync(novaParticipacao);
            }

            return RedirectToAction(nameof(Index), new { idEvento, idSubEvento });
        }

        [Authorize(Roles = "ADMINISTRADOR,GESTOR,COLABORADOR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirParticipacao(uint id, uint idEvento)
        {
            var participacao = await _participacaoService.GetByIdAsync(id);
            if (participacao == null)
            {
                return NotFound();
            }

            var gestor = _inscricaoService.GetGestorInEvent(User.Identity.Name, idEvento);
            var colaborador = _inscricaoService.GetColaboradorInEvent(User.Identity.Name, idEvento);

            if (gestor == null && colaborador == null)
            {
                TempData.Clear();
                TempData["Message"] = "Você não tem permissão para excluir participação!";
                return RedirectToAction("GerenciarEvento", "Evento");
            }

            await _participacaoService.DeleteAsync(id);
            return RedirectToAction(nameof(Index), new { idEvento });
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ParticipacaoPessoaEventoDTO>>> GetAll()
        {
            // Obtém todas as entidades do banco
            var entidades = await _participacaoService.GetAllAsync();
            // Mapeia para DTO
            var dtos = _mapper.Map<List<ParticipacaoPessoaEventoDTO>>(entidades);
            return Ok(dtos);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet("{id}")]
        public async Task<ActionResult<ParticipacaoPessoaEventoDTO>> GetById(uint id)
        {
            var entidade = await _participacaoService.GetByIdAsync(id);
            if (entidade == null)
                return NotFound();

            var dto = _mapper.Map<ParticipacaoPessoaEventoDTO>(entidade);
            return Ok(dto);
        }

        // POST: api/ParticipacaoPessoaEvento
        [HttpPost]
        public async Task<ActionResult<ParticipacaoPessoaEventoDTO>> Create([FromBody] ParticipacaoPessoaEventoDTO dto)
        {
            // Converte DTO em entidade de domínio
            var entidade = _mapper.Map<Participacaopessoaevento>(dto);
            // Persiste no banco
            var criada = await _participacaoService.AddAsync(entidade);
            // Mapeia de volta para DTO
            var resultDto = _mapper.Map<ParticipacaoPessoaEventoDTO>(criada);

            return CreatedAtAction(nameof(GetById), new { id = resultDto.Id }, resultDto);
        }

        // PUT: api/ParticipacaoPessoaEvento/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(uint id, [FromBody] ParticipacaoPessoaEventoDTO dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var entidade = _mapper.Map<Participacaopessoaevento>(dto);
            var sucesso = await _participacaoService.UpdateAsync(entidade);
            if (!sucesso)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/ParticipacaoPessoaEvento/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(uint id)
        {
            var sucesso = await _participacaoService.DeleteAsync(id);
            if (!sucesso)
                return NotFound();

            return NoContent();
        }
    }
}
