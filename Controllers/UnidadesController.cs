using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaizerNordesteWeb.API.Data;
using RaizesNordesteWeb.API.DTOs;
using RaizesNordesteWeb.API.Models;

namespace RaizesNordesteWeb.API.Controllers
{
    [ApiController]
    [Route("unidades")]
    [Produces("application/json")]
    [Authorize]
    public class UnidadesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UnidadesController(AppDbContext db) => _db = db;

        /// <summary>Lista todas as unidades ativas da rede.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<Unidade>), 200)]
        public async Task<IActionResult> Listar([FromQuery] bool? ativa, [FromQuery] string? cidade)
        {
            var query = _db.Unidades.AsQueryable();

            if (ativa.HasValue)
                query = query.Where(u => u.Ativa == ativa.Value);

            if (!string.IsNullOrWhiteSpace(cidade))
                query = query.Where(u => u.Cidade.ToLower().Contains(cidade.ToLower()));

            var unidades = await query
                .Select(u => new
                {
                    u.Id,
                    u.Nome,
                    u.Cidade,
                    u.UF,
                    u.TipoCozinha,
                    u.Ativa
                })
                .ToListAsync();

            return Ok(unidades);
        }

        /// <summary>Retorna uma unidade específica pelo ID.</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var unidade = await _db.Unidades.FindAsync(id);
            if (unidade == null)
                return NotFound(ErroPadrao.Criar("UNIDADE_NAO_ENCONTRADA",
                    "Unidade não encontrada.", $"/unidades/{id}"));

            return Ok(new
            {
                unidade.Id, unidade.Nome, unidade.Cidade,
                unidade.UF, unidade.TipoCozinha, unidade.Ativa
            });
        }

        /// <summary>Cria uma nova unidade. Restrito a Admin e Gerente.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Gerente")]
        [ProducesResponseType(201)]
        [ProducesResponseType(typeof(ErroPadrao), 400)]
        [ProducesResponseType(typeof(ErroPadrao), 401)]
        [ProducesResponseType(typeof(ErroPadrao), 403)]
        public async Task<IActionResult> Criar([FromBody] Unidade dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var unidade = new Unidade
            {
                Nome = dto.Nome,
                Cidade = dto.Cidade,
                UF = dto.UF,
                TipoCozinha = dto.TipoCozinha,
                Ativa = dto.Ativa
            };

            _db.Unidades.Add(unidade);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(ObterPorId), new { id = unidade.Id }, new
            {
                unidade.Id, unidade.Nome, unidade.Cidade, unidade.UF, unidade.Ativa
            });
        }

        /// <summary>Ativa ou desativa uma unidade. Restrito a Admin.</summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErroPadrao), 404)]
        public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] bool ativa)
        {
            var unidade = await _db.Unidades.FindAsync(id);
            if (unidade == null)
                return NotFound(ErroPadrao.Criar("UNIDADE_NAO_ENCONTRADA",
                    "Unidade não encontrada.", $"/unidades/{id}/status"));

            unidade.Ativa = ativa;
            await _db.SaveChangesAsync();

            return Ok(new { unidade.Id, unidade.Nome, unidade.Ativa });
        }
    }
}
