using AutoMapper;
using Core;               // namespace onde está a entidade Participacaopessoaevento
using Core.DTO;           // namespace onde estarão seus DTOs (crie ParticipacaoPessoaEventoDTO aqui)
using Core.Service;       // namespace onde estarão suas interfaces IParticipacaoPessoaEventoService e a implementação
using Microsoft.AspNetCore.Mvc;
using Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventoWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParticipacaoPessoaEventoController : ControllerBase
    {
        private readonly IParticipacaoPessoaEventoService _participacaoService;
        private readonly IMapper _mapper;

        public ParticipacaoPessoaEventoController(
            IParticipacaoPessoaEventoService participacaoService,
            IMapper mapper)
        {
            _participacaoService = participacaoService;
            _mapper = mapper;
        }

        // GET: api/ParticipacaoPessoaEvento
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ParticipacaoPessoaEventoDTO>>> GetAll()
        {
            // Obtém todas as entidades do banco
            var entidades = await _participacaoService.GetAllAsync();
            // Mapeia para DTO
            var dtos = _mapper.Map<List<ParticipacaoPessoaEventoDTO>>(entidades);
            return Ok(dtos);
        }

        // GET: api/ParticipacaoPessoaEvento/5
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
